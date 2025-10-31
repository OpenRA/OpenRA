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

			var widgetInfos = modData.ObjectCreator.GetTypes()
				.Where(t => t.Name.EndsWith("Widget", StringComparison.InvariantCulture) && t.IsSubclassOf(typeof(Widget)))
				.ToDictionary(
					t => t.Name[..^6],
					t => Utility.GetFields(t).Where(Utility.HasAttribute<FluentReferenceAttribute>).Select(f => f.Name).ToArray())
				.Where(t => t.Value.Length > 0)
				.ToDictionary(t => t.Key, t => t.Value);

			var chromeLayouts = modData.Manifest.ChromeLayout.GroupBy(c => c.Split('/')[0].Split('|')[0], c => c);

			foreach (var layout in chromeLayouts)
			{
				var fluentFolder = layout.Key + "|fluent";
				var fluentPackage = modData.ModFiles.OpenPackage(fluentFolder);
				var fluentPath = Path.Combine(fluentPackage.Name, "chrome.ftl");

				var unsortedCandidates = new List<ExtractionCandidate>();
				var groupedCandidates = new Dictionary<HashSet<string>, List<ExtractionCandidate>>();

				var yamlSet = new YamlFileSet();

				// Get all string candidates.
				foreach (var chrome in layout)
				{
					modData.ModFiles.TryGetPackageContaining(chrome, out var chromePackage, out var chromeName);
					var chromePath = Path.Combine(chromePackage.Name, chromeName);

					var yaml = MiniYaml.FromFile(chromePath, false).Select(n => new MiniYamlNodeBuilder(n)).ToList();
					yamlSet.Add(((IReadWritePackage)chromePackage, chromeName, yaml));

					var extractionCandidates = new List<ExtractionCandidate>();
					foreach (var node in yaml)
					{
						if (node.Key != null)
						{
							var nodeSplit = node.Key.Split('@');
							var nodeId = nodeSplit.Length > 1 ? ClearContainersAndToLower(nodeSplit[1]) : null;
							FromChromeLayout(node, widgetInfos, nodeId, ref extractionCandidates);
						}
					}

					if (extractionCandidates.Count > 0)
					{
						var chromeFilename = chrome.Split('/').Last();
						groupedCandidates[[chromeFilename]] = [];
						for (var i = 0; i < extractionCandidates.Count; i++)
						{
							var candidate = extractionCandidates[i];
							candidate.Chrome = chromeFilename;
							unsortedCandidates.Add(candidate);
						}
					}
				}

				// Join matching candidates.
				foreach (var candidate in unsortedCandidates)
				{
					HashSet<string> foundHash = null;
					ExtractionCandidate found = default;
					foreach (var (hash, candidates) in groupedCandidates)
					{
						foreach (var c in candidates)
						{
							if (c.Key == candidate.Key && c.Type == candidate.Type && c.Value == candidate.Value)
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
						groupedCandidates[newHash] = [candidate];
				}

				var startWithNewline = File.Exists(fluentPath);

				// StreamWriter can't create new directories.
				if (!startWithNewline)
					Directory.CreateDirectory(Path.GetDirectoryName(fluentPath));

				// Write output .ftl files.
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

						// Pushing blocks to string first allows for fancier formatting.
						var build = "";
						foreach (var grouping in candidates.GroupBy(t => t.Key))
						{
							if (grouping.Count() == 1)
							{
								var candidate = grouping.First();
								var key = candidate.Key;
								if (candidate.Type != "text")
									key = $"{key}-" + candidate.Type.Replace("text", "");

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
									var type = candidate.Type;
									if (candidate.Type != "label")
									{
										if (candidate.Type == "text")
											type = "label";
										else
											type = type.Replace("text", "");
									}

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

				yamlSet.Save();
			}
		}

		struct ExtractionCandidate(string key, string type, string value, MiniYamlNodeBuilder node)
		{
			public string Chrome = null;
			public readonly string Key = key;
			public readonly string Type = type;
			public readonly string Value = value;
			public readonly List<MiniYamlNodeBuilder> Nodes = [node];
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

		static void FromChromeLayout(
			MiniYamlNodeBuilder node, Dictionary<string, string[]> widgetInfos, string container, ref List<ExtractionCandidate> candidates)
		{
			var nodeSplit = node.Key.Split('@');
			var widgetType = nodeSplit[0];
			var nodeId = nodeSplit.Length > 1 ? ClearContainersAndToLower(nodeSplit[1]) : null;

			if ((widgetType == "Background" || widgetType == "Container") && nodeId != null)
				container = nodeId;

			var validChildTypes = new List<(MiniYamlNodeBuilder Node, string Type, string Value)>();
			foreach (var childNode in node.Value.Nodes)
			{
				if (widgetInfos.TryGetValue(widgetType, out var fieldName))
				{
					var childType = childNode.Key.Split('@')[0];
					if (fieldName.Contains(childType)
						&& !string.IsNullOrEmpty(childNode.Value.Value)
						&& !UpdateUtils.IsAlreadyExtracted(childNode.Value.Value)
						&& childNode.Value.Value.Any(char.IsLetterOrDigit))
					{
						var replaced = false;
						var value = childNode.Value.Value;
						if (value.Contains("\\n"))
						{
							value = value.Replace("\\n", "\n    ");
							replaced = true;
						}

						value = value
							.Replace("{", "<")
							.Replace("}", ">")
							.Trim().Trim('\n');

						// Preserve indentation
						if (replaced)
							value = "\n    " + value;

						validChildTypes.Add((childNode, childType.ToLowerInvariant(), value));
					}
				}
			}

			// Generate string key.
			if (validChildTypes.Count > 0)
			{
				widgetType = ClearTypesAndToLower(widgetType);

				var key = widgetType;
				if (!string.IsNullOrEmpty(container))
				{
					var containerType = string.Join('-', container.Split('_').Exclude(widgetType).Where(s => !string.IsNullOrEmpty(s)));
					if (!string.IsNullOrEmpty(containerType))
						key = $"{key}-{containerType}";
				}

				if (!string.IsNullOrEmpty(nodeId))
				{
					nodeId = string.Join('-', nodeId.Split('_')
						.Except(string.IsNullOrEmpty(container) ? [widgetType] : container.Split('_').Append(widgetType))
						.Where(s => !string.IsNullOrEmpty(s)));

					if (!string.IsNullOrEmpty(nodeId))
						key = $"{key}-{nodeId}";
				}

				foreach (var (childNode, childType, childValue) in validChildTypes)
					candidates.Add(new ExtractionCandidate(key, childType, childValue.Trim().Trim('\n'), childNode));
			}

			// Recursive.
			foreach (var childNode in node.Value.Nodes)
				if (childNode.Key == "Children")
					foreach (var n in childNode.Value.Nodes)
						FromChromeLayout(n, widgetInfos, container, ref candidates);
		}
	}
}
