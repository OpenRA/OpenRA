#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.UpdateRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.UtilityCommands
{
	using YamlFileSet = List<(IReadWritePackage Package, string File, List<MiniYamlNodeBuilder> Nodes)>;

	sealed class ExtractYamlStringsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--extract-yaml-strings"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Extract fluent strings that are not yet localized.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var traitInfos = modData.ObjectCreator.GetTypes()
				.Where(t => t.Name.EndsWith("Info", StringComparison.InvariantCulture) && t.IsSubclassOf(typeof(TraitInfo)))
				.ToDictionary(
					t => t.Name[..^4],
					t => Utility.GetFields(t).Where(Utility.HasAttribute<FluentReferenceAttribute>).Select(f => f.Name).ToArray())
				.Where(t => t.Value.Length > 0)
				.ToDictionary(t => t.Key, t => t.Value);

			var modRules = UpdateUtils.LoadModYaml(modData, UpdateUtils.FilterExternalFiles(modData, modData.Manifest.Rules, []));

			// Include files referenced in maps.
			foreach (var package in modData.MapCache.EnumerateMapPackagesWithoutCaching())
			{
				using (var mapStream = package.GetStream("map.yaml"))
				{
					if (mapStream == null)
						continue;

					var yaml = new MiniYamlBuilder(null, MiniYaml.FromStream(mapStream, $"{package.Name}:map.yaml", false).ToList());
					var mapRulesNode = yaml.NodeWithKeyOrDefault("Rules");
					if (mapRulesNode != null)
						modRules.AddRange(UpdateUtils.LoadExternalMapYaml(modData, mapRulesNode.Value, []));
				}
			}

			var fluentPackage = modData.ModFiles.OpenPackage(modData.Manifest.Id + "|fluent");
			ExtractFromFile(Path.Combine(fluentPackage.Name, "rules.ftl"), modRules, traitInfos);
			modRules.Save();

			// Extract from maps.
			foreach (var package in modData.MapCache.EnumerateMapPackagesWithoutCaching())
			{
				using (var mapStream = package.GetStream("map.yaml"))
				{
					if (mapStream == null)
						continue;

					var yaml = new MiniYamlBuilder(null, MiniYaml.FromStream(mapStream, $"{package.Name}:map.yaml", false).ToList());
					var mapRules = new YamlFileSet() { (package, "map.yaml", yaml.Nodes) };

					var mapRulesNode = yaml.NodeWithKeyOrDefault("Rules");
					if (mapRulesNode != null)
						mapRules.AddRange(UpdateUtils.LoadInternalMapYaml(modData, package, mapRulesNode.Value, []));

					const string Mapftl = "map.ftl";
					ExtractFromFile(Path.Combine(package.Name, Mapftl), mapRules, traitInfos, () =>
					{
						var node = yaml.NodeWithKeyOrDefault("FluentMessages");
						if (node != null)
						{
							var value = node.NodeValue<string[]>();
							if (!value.Contains(Mapftl))
								node.Value.Value = string.Join(", ", value.Concat([Mapftl]).ToArray());
						}
						else
							yaml.Nodes.Add(new MiniYamlNodeBuilder("FluentMessages", Mapftl));
					});

					mapRules.Save();
				}
			}
		}

		static void ExtractFromFile(string fluentPath, YamlFileSet yamlSet, Dictionary<string, string[]> traitInfos, Action addAction = null)
		{
			var unsortedCandidates = new List<ExtractionCandidate>();
			var groupedCandidates = new Dictionary<HashSet<string>, List<ExtractionCandidate>>();

			// Get all string candidates.
			foreach (var (_, file, actors) in yamlSet)
			{
				var candidates = new List<ExtractionCandidate>();
				foreach (var actor in actors)
					if (actor.Key != null)
						ExtractFromActor(actor, traitInfos, ref candidates);

				if (candidates.Count > 0)
				{
					var ruleFilename = file.Split('/').Last();
					groupedCandidates[[ruleFilename]] = [];
					for (var i = 0; i < candidates.Count; i++)
					{
						var candidate = candidates[i];
						candidate.Filename = ruleFilename;
						unsortedCandidates.Add(candidate);
					}
				}
			}

			if (unsortedCandidates.Count == 0)
				return;

			// Join matching candidates.
			foreach (var candidate in unsortedCandidates)
			{
				HashSet<string> foundHash = null;
				ExtractionCandidate found = default;
				foreach (var (hash, candidates) in groupedCandidates)
				{
					foreach (var c in candidates)
					{
						if (c.Actor == candidate.Actor && c.Key == candidate.Key && c.Value == candidate.Value)
						{
							foundHash = hash;
							found = c;
							break;
						}
					}

					if (foundHash != null)
						break;
				}

				if (foundHash == null)
				{
					var hash = groupedCandidates.Keys.First(t => t.First() == candidate.Filename);
					groupedCandidates[hash].Add(candidate);
					continue;
				}

				var newHash = foundHash.Append(candidate.Filename).ToHashSet();
				candidate.Nodes.AddRange(found.Nodes);
				groupedCandidates[foundHash].Remove(found);

				var nHash = groupedCandidates.FirstOrDefault(t => t.Key.SetEquals(newHash));
				if (nHash.Key != null)
					groupedCandidates[nHash.Key].Add(candidate);
				else
					groupedCandidates[newHash] = [candidate];
			}

			addAction?.Invoke();

			// StreamWriter can't create new directories.
			var startWithNewline = File.Exists(fluentPath);
			if (!startWithNewline)
				Directory.CreateDirectory(Path.GetDirectoryName(fluentPath));

			// Write output .ftl files.
			using (var fluentWriter = new StreamWriter(fluentPath, true))
			{
				foreach (var (filename, candidates) in groupedCandidates.OrderBy(t => string.Join(',', t.Key)))
				{
					if (candidates.Count == 0)
						continue;

					if (startWithNewline)
						fluentWriter.WriteLine();
					else
						startWithNewline = true;

					fluentWriter.WriteLine("## " + string.Join(", ", filename));

					// Pushing blocks to string first allows for fancier formatting.
					var build = "";
					foreach (var grouping in candidates.GroupBy(t => t.Actor))
					{
						if (grouping.Count() == 1)
						{
							var candidate = grouping.First();
							var key = $"{candidate.Actor}-{candidate.Key}";
							build += $"{key} = {candidate.Value}\n";
							foreach (var node in candidate.Nodes)
								node.Value.Value = key;
						}
						else
						{
							if (build.Length > 1 && build.Substring(build.Length - 2, 2) != "\n\n")
								build += "\n";

							var key = grouping.Key;
							build += $"{key} =\n";
							foreach (var candidate in grouping)
							{
								var type = candidate.Key;
								build += $"    .{type} = {candidate.Value}\n";

								foreach (var node in candidate.Nodes)
									node.Value.Value = $"{key}.{type}";
							}

							build += "\n";
						}
					}

					fluentWriter.WriteLine(build.Trim('\n'));
				}
			}
		}

		struct ExtractionCandidate(string actor, string key, string value, MiniYamlNodeBuilder node)
		{
			public string Filename = null;
			public readonly string Actor = actor;
			public readonly string Key = key;
			public readonly string Value = value;
			public readonly List<MiniYamlNodeBuilder> Nodes = [node];
		}

		static string ToLowerActor(string actor)
		{
			var s = actor.Replace('.', '-').Replace('_', '-').ToLowerInvariant();
			if (actor[0] == '^')
				return $"meta-{s[1..]}";
			else
				return $"actor-{s}";
		}

		static string ToLower(string value)
		{
			if (string.IsNullOrEmpty(value))
				return "";

			var s = new StringBuilder();
			for (var i = 0; i < value.Length; i++)
			{
				var c = value[i];
				if (char.IsUpper(c))
				{
					if (i > 0)
						s.Append('-');

					s.Append(char.ToLowerInvariant(c));
				}
				else
					s.Append(c);
			}

			return s.ToString();
		}

		static void ExtractFromActor(MiniYamlNodeBuilder actor, Dictionary<string, string[]> traitInfos, ref List<ExtractionCandidate> candidates)
		{
			if (actor.Value?.Nodes == null)
				return;

			foreach (var trait in actor.Value.Nodes)
			{
				if (trait.Key == null)
					continue;

				var traitSplit = trait.Key.Split('@');
				var traitInfo = traitSplit[0];
				if (!traitInfos.TryGetValue(traitInfo, out var type) || trait.Value?.Nodes == null)
					continue;

				foreach (var property in trait.Value.Nodes)
				{
					if (property.Key == null)
						continue;

					var propertyType = property.Key.Split('@')[0];
					if (!type.Contains(propertyType))
						continue;

					var propertyValue = property.Value.Value;
					if (string.IsNullOrEmpty(propertyValue) || UpdateUtils.IsAlreadyExtracted(propertyValue) || !propertyValue.Any(char.IsLetterOrDigit))
						continue;

					var replaced = false;
					var value = propertyValue;
					if (value.Contains("\\n"))
					{
						value = value.Replace("\\n", "\n    ");
						replaced = true;
					}

					// Preserve indentation
					value = (replaced ? "\n    " : "") + value.Trim().Trim('\n');

					var actorName = ToLowerActor(actor.Key);
					var key = traitInfo;
					if (traitInfo == nameof(Buildable))
					{
						candidates.Add(new ExtractionCandidate(actorName, ToLower(propertyType), value, property));
						continue;
					}
					else if (traitInfo == nameof(Encyclopedia))
					{
						candidates.Add(new ExtractionCandidate(actorName, ToLower(traitInfo), value, property));
						continue;
					}
					else if (traitInfo == nameof(Tooltip) || traitInfo == nameof(EditorOnlyTooltipInfo)[..^4])
					{
						if (traitSplit.Length > 1)
							key = $"{traitSplit[1].ToLowerInvariant()}-{propertyType}";
						else
							key = propertyType;

						candidates.Add(new ExtractionCandidate(actorName, ToLower(key), value, property));
						continue;
					}

					if (traitSplit.Length > 1)
						key += $"-{traitSplit[1]}";

					key += $"-{ToLower(propertyType)}";

					candidates.Add(new ExtractionCandidate(actorName, key.ToLowerInvariant(), value, property));
				}
			}
		}
	}
}
