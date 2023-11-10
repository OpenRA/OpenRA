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
using OpenRA.FileSystem;
using OpenRA.Mods.Common.UpdateRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.UtilityCommands
{
	using YamlFileSet = List<(IReadWritePackage Package, string File, List<MiniYamlNodeBuilder> Nodes)>;

	sealed class ExtractChromeStringsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--extract-chrome-strings"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Extract translatable strings that are not yet localized and update chrome layout.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			var translatableFields = modData.ObjectCreator.GetTypes()
				.Where(t => t.Name.EndsWith("Widget", StringComparison.InvariantCulture) && t.IsSubclassOf(typeof(Widget)))
				.ToDictionary(
					t => t.Name[..^6],
					t => t.GetFields().Where(f => f.HasAttribute<TranslationReferenceAttribute>()).Select(f => f.Name).ToArray())
				.Where(t => t.Value.Length > 0)
				.ToDictionary(t => t.Key, t => t.Value);

			var chromeLayouts = modData.Manifest.ChromeLayout.GroupBy(c => c.Split('/')[0].Split('|')[0], c => c);

			foreach (var layout in chromeLayouts)
			{
				var fluentFolder = layout.Key + "|languages";
				var fluentPackage = modData.ModFiles.OpenPackage(fluentFolder);
				var fluentPath = Path.Combine(fluentPackage.Name, "chrome/en.ftl");

				var unsortedCandidates = new List<TranslationCandidate>();
				var groupedCandidates = new Dictionary<HashSet<string>, List<TranslationCandidate>>();

				var yamlSet = new YamlFileSet();

				// Get all translations.
				foreach (var chrome in layout)
				{
					modData.ModFiles.TryGetPackageContaining(chrome, out var chromePackage, out var chromeName);
					var chromePath = Path.Combine(chromePackage.Name, chromeName);

					var yaml = MiniYaml.FromFile(chromePath, false).ConvertAll(n => new MiniYamlNodeBuilder(n));
					yamlSet.Add(((IReadWritePackage)chromePackage, chromeName, yaml));

					var translationCandidates = new List<TranslationCandidate>();
					foreach (var node in yaml)
					{
						if (node.Key != null)
						{
							var nodeSplit = node.Key.Split('@');
							var nodeId = nodeSplit.Length > 1 ? ClearContainersAndToLower(nodeSplit[1]) : null;
							FromChromeLayout(node, translatableFields, nodeId, ref translationCandidates);
						}
					}

					if (translationCandidates.Count > 0)
					{
						var chromeFilename = chrome.Split('/').Last();
						groupedCandidates[new HashSet<string>() { chromeFilename }] = new List<TranslationCandidate>();
						for (var i = 0; i < translationCandidates.Count; i++)
						{
							var candidate = translationCandidates[i];
							candidate.Chrome = chromeFilename;
							unsortedCandidates.Add(candidate);
						}
					}
				}

				// Join matching translations.
				foreach (var candidate in unsortedCandidates)
				{
					HashSet<string> foundHash = null;
					TranslationCandidate found = default;
					foreach (var (hash, translation) in groupedCandidates)
					{
						foreach (var c in translation)
						{
							if (c.Key == candidate.Key && c.Type == candidate.Type && c.Translation == candidate.Translation)
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
						var hash = groupedCandidates.Keys.First(t => t.First() == candidate.Chrome);
						groupedCandidates[hash].Add(candidate);
						continue;
					}

					var newHash = foundHash.Append(candidate.Chrome).ToHashSet();
					candidate.Nodes.AddRange(found.Nodes);
					groupedCandidates[foundHash].Remove(found);

					var nHash = groupedCandidates.FirstOrDefault(t => t.Key.SetEquals(newHash));
					if (nHash.Key != null)
						groupedCandidates[nHash.Key].Add(candidate);
					else
						groupedCandidates[newHash] = new List<TranslationCandidate>() { candidate };
				}

				var startWithNewline = File.Exists(fluentPath);

				// StreamWriter can't create new directories.
				if (!startWithNewline)
					Directory.CreateDirectory(Path.GetDirectoryName(fluentPath));

				// Write to translation files.
				using (var fluentWriter = new StreamWriter(fluentPath, append: true))
				{
					foreach (var (chromeFilename, candidates) in groupedCandidates.OrderBy(t => string.Join(',', t.Key)))
					{
						if (candidates.Count == 0)
							continue;

						if (startWithNewline)
							fluentWriter.WriteLine();
						else
							startWithNewline = true;

						fluentWriter.WriteLine("## " + string.Join(", ", chromeFilename));

						// Pushing blocks of translations to string first allows for fancier formatting.
						var build = "";
						foreach (var grouping in candidates.GroupBy(t => t.Key))
						{
							if (grouping.Count() == 1)
							{
								var candidate = grouping.First();
								var translationKey = candidate.Key;
								if (candidate.Type == "text")
									translationKey = $"{translationKey}";
								else
									translationKey = $"{translationKey}-" + candidate.Type.Replace("text", "");

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
									var type = candidate.Type;
									if (candidate.Type != "label")
									{
										if (candidate.Type == "text")
											type = "label";
										else
											type = type.Replace("text", "");
									}

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

				yamlSet.Save();
			}
		}

		struct TranslationCandidate
		{
			public string Chrome;
			public readonly string Key;
			public readonly string Type;
			public readonly string Translation;
			public readonly List<MiniYamlNodeBuilder> Nodes;

			public TranslationCandidate(string key, string type, string translation, MiniYamlNodeBuilder node)
			{
				Chrome = null;
				Key = key;
				Type = type;
				Translation = translation;
				Nodes = new List<MiniYamlNodeBuilder>() { node };
			}
		}

		static string ClearContainersAndToLower(string node)
		{
			return node
				.Replace("Background", "")
				.Replace("Container", "")
				.Replace("Panel", "")
				.ToLowerInvariant()
				.Replace("headers", "");
		}

		static string ClearTypesAndToLower(string node)
		{
			return node
				.Replace("LabelForInput", "Label")
				.Replace("LabelWithHighlight", "Label")
				.Replace("DropdownButton", "Dropdown")
				.Replace("CheckboxButton", "Checkbox")
				.Replace("MenuButton", "Button")
				.Replace("WorldButton", "Button")
				.Replace("ProductionTypeButton", "Button")
				.ToLowerInvariant();
		}

		static void FromChromeLayout(MiniYamlNodeBuilder node, Dictionary<string, string[]> translatables, string container, ref List<TranslationCandidate> translations)
		{
			var nodeSplit = node.Key.Split('@');
			var nodeType = nodeSplit[0];
			var nodeId = nodeSplit.Length > 1 ? ClearContainersAndToLower(nodeSplit[1]) : null;

			if ((nodeType == "Background" || nodeType == "Container") && nodeId != null)
				container = nodeId;

			// Get translatable types.
			var validChildTypes = new List<(MiniYamlNodeBuilder Node, string Type, string Value)>();
			foreach (var childNode in node.Value.Nodes)
			{
				if (translatables.TryGetValue(nodeType, out var fieldName))
				{
					var childType = childNode.Key.Split('@')[0];
					if (fieldName.Contains(childType)
						&& !string.IsNullOrEmpty(childNode.Value.Value)
						&& !UpdateUtils.IsAlreadyTranslated(childNode.Value.Value)
						&& childNode.Value.Value.Any(char.IsLetterOrDigit))
					{
						var translationValue = childNode.Value.Value
							.Replace("\\n", "\n    ")
							.Replace("{", "<")
							.Replace("}", ">")
							.Trim().Trim('\n');

						validChildTypes.Add((childNode, childType.ToLowerInvariant(), translationValue));
					}
				}
			}

			// Generate translation key.
			if (validChildTypes.Count > 0)
			{
				nodeType = ClearTypesAndToLower(nodeType);

				var translationKey = nodeType;
				if (!string.IsNullOrEmpty(container))
				{
					var containerType = string.Join('-', container.Split('_').Exclude(nodeType).Where(s => !string.IsNullOrEmpty(s)));
					if (!string.IsNullOrEmpty(containerType))
						translationKey = $"{translationKey}-{containerType}";
				}

				if (!string.IsNullOrEmpty(nodeId))
				{
					nodeId = string.Join('-', nodeId.Split('_')
						.Except(string.IsNullOrEmpty(container) ? new string[] { nodeType } : container.Split('_').Append(nodeType))
						.Where(s => !string.IsNullOrEmpty(s)));

					if (!string.IsNullOrEmpty(nodeId))
						translationKey = $"{translationKey}-{nodeId}";
				}

				foreach (var (childNode, childType, translationValue) in validChildTypes)
					translations.Add(new TranslationCandidate(translationKey, childType, translationValue.Trim().Trim('\n'), childNode));
			}

			// Recursive.
			foreach (var childNode in node.Value.Nodes)
				if (childNode.Key == "Children")
					foreach (var n in childNode.Value.Nodes)
						FromChromeLayout(n, translatables, container, ref translations);
		}

		/// <summary>This is a helper method to find untranslated strings in chrome layouts.</summary>
		public static void FindUntranslatedStringFields(ModData modData)
		{
			var types = modData.ObjectCreator.GetTypes();
			foreach (var (type, fields) in types.Where(t => t.Name.EndsWith("Widget", StringComparison.InvariantCulture) && t.IsSubclassOf(typeof(Widget))).ToDictionary(t => t.Name[..^6],
				t => t.GetFields().Where(f => f.Name != "Id" && f.IsPublic && f.FieldType == typeof(string) && !f.HasAttribute<TranslationReferenceAttribute>()).Distinct().Select(f => f.Name).ToList()))
				if (fields.Count > 0)
					Console.WriteLine($"{type}Widget:\n  {string.Join("\n  ", fields)}");
		}
	}
}
