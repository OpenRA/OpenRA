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

		[Desc("Extract translatable strings that are not yet localized.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var translatables = modData.ObjectCreator.GetTypes()
				.Where(t => t.Name.EndsWith("Info", StringComparison.InvariantCulture) && t.IsSubclassOf(typeof(TraitInfo)))
				.ToDictionary(
					t => t.Name[..^4],
					t => t.GetFields().Where(f => f.HasAttribute<TranslationReferenceAttribute>()).Select(f => f.Name).ToArray())
				.Where(t => t.Value.Length > 0)
				.ToDictionary(t => t.Key, t => t.Value);

			var modRules = UpdateUtils.LoadModYaml(modData, UpdateUtils.FilterExternalModFiles(modData, modData.Manifest.Rules, new HashSet<string>()));

			// Include files referenced in maps.
			foreach (var package in modData.MapCache.EnumerateMapPackagesWithoutCaching())
			{
				using (var mapStream = package.GetStream("map.yaml"))
				{
					if (mapStream == null)
						continue;

					var yaml = new MiniYamlBuilder(null, MiniYaml.FromStream(mapStream, $"{package.Name}:map.yaml", false));
					var mapRulesNode = yaml.NodeWithKeyOrDefault("Rules");
					if (mapRulesNode != null)
						modRules.AddRange(UpdateUtils.LoadExternalMapYaml(modData, mapRulesNode.Value, new HashSet<string>()));
				}
			}

			var fluentPackage = modData.ModFiles.OpenPackage(modData.Manifest.Id + "|languages");
			ExtractFromFile(Path.Combine(fluentPackage.Name, "rules/en.ftl"), modRules, translatables);
			modRules.Save();

			// Extract from maps.
			foreach (var package in modData.MapCache.EnumerateMapPackagesWithoutCaching())
			{
				using (var mapStream = package.GetStream("map.yaml"))
				{
					if (mapStream == null)
						continue;

					var yaml = new MiniYamlBuilder(null, MiniYaml.FromStream(mapStream, $"{package.Name}:map.yaml", false));
					var mapRules = new YamlFileSet() { (package, "map.yaml", yaml.Nodes) };

					var mapRulesNode = yaml.NodeWithKeyOrDefault("Rules");
					if (mapRulesNode != null)
						mapRules.AddRange(UpdateUtils.LoadInternalMapYaml(modData, package, mapRulesNode.Value, new HashSet<string>()));

					const string Enftl = "en.ftl";
					ExtractFromFile(Path.Combine(package.Name, Enftl), mapRules, translatables, () =>
					{
						var node = yaml.NodeWithKeyOrDefault("Translations");
						if (node != null)
						{
							var value = node.NodeValue<string[]>();
							if (!value.Contains(Enftl))
								node.Value.Value = string.Join(", ", value.Concat(new string[] { Enftl }).ToArray());
						}
						else
							yaml.Nodes.Add(new MiniYamlNodeBuilder("Translations", Enftl));
					});

					mapRules.Save();
				}
			}
		}

		static void ExtractFromFile(string fluentPath, YamlFileSet yamlSet, Dictionary<string, string[]> translatables, Action addTranslation = null)
		{
			var unsortedCandidates = new List<TranslationCandidate>();
			var groupedCandidates = new Dictionary<HashSet<string>, List<TranslationCandidate>>();

			// Get all translations.
			foreach (var (_, file, nodes) in yamlSet)
			{
				var translationCandidates = new List<TranslationCandidate>();
				foreach (var actor in nodes)
					if (actor.Key != null)
						ExtractFromActor(actor, translatables, ref translationCandidates);

				if (translationCandidates.Count > 0)
				{
					var ruleFilename = file.Split('/').Last();
					groupedCandidates[new HashSet<string>() { ruleFilename }] = new List<TranslationCandidate>();
					for (var i = 0; i < translationCandidates.Count; i++)
					{
						var candidate = translationCandidates[i];
						candidate.Filename = ruleFilename;
						unsortedCandidates.Add(candidate);
					}
				}
			}

			if (unsortedCandidates.Count == 0)
				return;

			// Join matching translations.
			foreach (var candidate in unsortedCandidates)
			{
				HashSet<string> foundHash = null;
				TranslationCandidate found = default;
				foreach (var (hash, translation) in groupedCandidates)
				{
					foreach (var c in translation)
					{
						if (c.Actor == candidate.Actor && c.Key == candidate.Key && c.Translation == candidate.Translation)
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
					groupedCandidates[newHash] = new List<TranslationCandidate>() { candidate };
			}

			addTranslation?.Invoke();

			// StreamWriter can't create new directories.
			var startWithNewline = File.Exists(fluentPath);
			if (!startWithNewline)
				Directory.CreateDirectory(Path.GetDirectoryName(fluentPath));

			// Write to translation files.
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

					// Pushing blocks of translations to string first allows for fancier formatting.
					var build = "";
					foreach (var grouping in candidates.GroupBy(t => t.Actor))
					{
						if (grouping.Count() == 1)
						{
							var candidate = grouping.First();
							var translationKey = $"{candidate.Actor}-{candidate.Key}";
							build += $"{translationKey} = {candidate.Translation}\n";
							foreach (var node in candidate.Nodes)
								node.Value.Value = translationKey;
						}
						else
						{
							if (build.Length > 1 && build.Substring(build.Length - 2, 2) != "\n\n")
								build += "\n";

							var translationKey = grouping.Key;
							build += $"{translationKey} =\n";
							foreach (var candidate in grouping)
							{
								var type = candidate.Key;
								build += $"   .{type} = {candidate.Translation}\n";

								foreach (var node in candidate.Nodes)
									node.Value.Value = $"{translationKey}.{type}";
							}

							build += "\n";
						}
					}

					fluentWriter.WriteLine(build.Trim('\n'));
				}
			}
		}

		struct TranslationCandidate
		{
			public string Filename;
			public readonly string Actor;
			public readonly string Key;
			public readonly string Translation;
			public readonly List<MiniYamlNodeBuilder> Nodes;

			public TranslationCandidate(string actor, string key, string translation, MiniYamlNodeBuilder node)
			{
				Filename = null;
				Actor = actor;
				Key = key;
				Translation = translation;
				Nodes = new List<MiniYamlNodeBuilder>() { node };
			}
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

		static void ExtractFromActor(MiniYamlNodeBuilder actor, Dictionary<string, string[]> translatables, ref List<TranslationCandidate> translations)
		{
			if (actor.Value?.Nodes == null)
				return;

			foreach (var trait in actor.Value.Nodes)
			{
				if (trait.Key == null)
					continue;

				var traitSplit = trait.Key.Split('@');
				var traitInfo = traitSplit[0];
				if (!translatables.TryGetValue(traitInfo, out var translatableType) || trait.Value?.Nodes == null)
					continue;

				foreach (var property in trait.Value.Nodes)
				{
					if (property.Key == null)
						continue;

					var propertyType = property.Key.Split('@')[0];
					if (!translatableType.Contains(propertyType))
						continue;

					var propertyValue = property.Value.Value;
					if (string.IsNullOrEmpty(propertyValue) || UpdateUtils.IsAlreadyTranslated(propertyValue) || !propertyValue.Any(char.IsLetterOrDigit))
						continue;

					var translationValue = propertyValue
						.Replace("\\n", "\n    ")
						.Trim().Trim('\n');

					var actorName = ToLowerActor(actor.Key);
					var key = traitInfo;
					if (traitInfo == nameof(Buildable))
					{
						translations.Add(new TranslationCandidate(actorName, ToLower(propertyType), translationValue, property));
						continue;
					}
					else if (traitInfo == nameof(Encyclopedia))
					{
						translations.Add(new TranslationCandidate(actorName, ToLower(traitInfo), translationValue, property));
						continue;
					}
					else if (traitInfo == nameof(Tooltip) || traitInfo == nameof(EditorOnlyTooltipInfo)[..^4])
					{
						if (traitSplit.Length > 1)
							key = $"{traitSplit[1].ToLowerInvariant()}-{propertyType}";
						else
							key = propertyType;

						translations.Add(new TranslationCandidate(actorName, ToLower(key), translationValue, property));
						continue;
					}

					if (traitSplit.Length > 1)
						key += $"-{traitSplit[1]}";

					key += $"-{ToLower(propertyType)}";

					translations.Add(new TranslationCandidate(actorName, key.ToLowerInvariant(), translationValue, property));
				}
			}
		}
	}
}
