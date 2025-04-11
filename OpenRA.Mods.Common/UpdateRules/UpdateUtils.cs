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
using System.Linq;
using System.Text;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UpdateRules
{
	using YamlFileSet = List<(IReadWritePackage Package, string File, List<MiniYamlNodeBuilder> Nodes)>;

	public static class UpdateUtils
	{
		/// <summary>
		/// Loads a YamlFileSet from a list of mod files.
		/// </summary>
		public static YamlFileSet LoadModYaml(ModData modData, IEnumerable<string> files)
		{
			var yaml = new YamlFileSet();
			foreach (var filename in files)
			{
				if (!modData.ModFiles.TryGetPackageContaining(filename, out var package, out var name) || package is not IReadWritePackage)
				{
					Console.WriteLine("Failed to load file `{0}` for writing. It will not be updated.", filename);
					continue;
				}

				yaml.Add(
					((IReadWritePackage)package,
					name,
					MiniYaml
						.FromStream(package.GetStream(name), $"{package.Name}:{name}", false)
						.Select(n => new MiniYamlNodeBuilder(n))
						.ToList()));
			}

			return yaml;
		}

		/// <summary>
		/// Loads a YamlFileSet containing any external yaml definitions referenced by a map yaml block.
		/// </summary>
		public static YamlFileSet LoadExternalMapYaml(ModData modData, MiniYamlBuilder yaml, HashSet<string> externalFilenames)
		{
			return FieldLoader.GetValue<string[]>("value", yaml.Value)
				.Where(f => f.Contains('|'))
				.SelectMany(f => LoadModYaml(modData, FilterExternalFiles(modData, [f], externalFilenames)))
				.ToList();
		}

		/// <summary>
		/// Loads a YamlFileSet containing any internal definitions yaml referenced by a map yaml block.
		/// External references or internal references to missing files are ignored.
		/// </summary>
		public static YamlFileSet LoadInternalMapYaml(ModData modData, IReadWritePackage mapPackage, MiniYamlBuilder yaml, HashSet<string> externalFilenames)
		{
			var fileSet = new YamlFileSet()
			{
				(null, "map.yaml", yaml.Nodes)
			};

			var files = FieldLoader.GetValue<string[]>("value", yaml.Value);
			foreach (var filename in files)
			{
				// Ignore any files that aren't in the map bundle
				if (!filename.Contains('|') && mapPackage.Contains(filename))
					fileSet.Add((
						mapPackage,
						filename,
						MiniYaml
							.FromStream(mapPackage.GetStream(filename), $"{mapPackage.Name}:{filename}", false)
							.Select(n => new MiniYamlNodeBuilder(n))
							.ToList()));
				else if (modData.ModFiles.Exists(filename))
					externalFilenames.Add(filename);
			}

			return fileSet;
		}

		/// <summary>
		/// Run a given update rule on a map.
		/// The rule is only applied to internal files - external includes are assumed to be handled separately
		/// but are noted in the externalFilenames list for informational purposes.
		/// </summary>
		public static List<string> UpdateMap(ModData modData, IReadWritePackage mapPackage, UpdateRule rule, out YamlFileSet files, HashSet<string> externalFilenames)
		{
			var manualSteps = new List<string>();

			using (var mapStream = mapPackage.GetStream("map.yaml"))
			{
				if (mapStream == null)
				{
					// Not a valid map
					files = [];
					return manualSteps;
				}

				var yaml = new MiniYamlBuilder(null, MiniYaml.FromStream(mapStream, $"{mapPackage.Name}:map.yaml", false).ToList());
				files = [(mapPackage, "map.yaml", yaml.Nodes)];

				manualSteps.AddRange(rule.BeforeUpdate(modData));

				var mapActorsNode = yaml.NodeWithKeyOrDefault("Actors");
				if (mapActorsNode != null)
				{
					var mapActors = new YamlFileSet()
					{
						(null, "map.yaml", mapActorsNode.Value.Nodes)
					};

					manualSteps.AddRange(ApplyTopLevelTransform(modData, mapActors, rule.UpdateMapActorNode));
					files.AddRange(mapActors);
				}

				var mapRulesNode = yaml.NodeWithKeyOrDefault("Rules");
				if (mapRulesNode != null)
				{
					if (rule is IBeforeUpdateActors before)
					{
						var resolvedActors = LoadMapYaml(modData.DefaultFileSystem, mapPackage, modData.Manifest.Rules, mapRulesNode.Value);
						manualSteps.AddRange(before.BeforeUpdateActors(modData, resolvedActors));
					}

					var mapRules = LoadInternalMapYaml(modData, mapPackage, mapRulesNode.Value, externalFilenames);
					manualSteps.AddRange(ApplyTopLevelTransform(modData, mapRules, rule.UpdateActorNode));
					files.AddRange(mapRules);
				}

				var mapWeaponsNode = yaml.NodeWithKeyOrDefault("Weapons");
				if (mapWeaponsNode != null)
				{
					if (rule is IBeforeUpdateWeapons before)
					{
						var resolvedWeapons = LoadMapYaml(modData.DefaultFileSystem, mapPackage, modData.Manifest.Weapons, mapWeaponsNode.Value);
						manualSteps.AddRange(before.BeforeUpdateWeapons(modData, resolvedWeapons));
					}

					var mapWeapons = LoadInternalMapYaml(modData, mapPackage, mapWeaponsNode.Value, externalFilenames);
					manualSteps.AddRange(ApplyTopLevelTransform(modData, mapWeapons, rule.UpdateWeaponNode));
					files.AddRange(mapWeapons);
				}

				var mapSequencesNode = yaml.NodeWithKeyOrDefault("Sequences");
				if (mapSequencesNode != null)
				{
					if (rule is IBeforeUpdateSequences before)
					{
						var resolvedImages = LoadMapYaml(modData.DefaultFileSystem, mapPackage, modData.Manifest.Sequences, mapSequencesNode.Value);
						manualSteps.AddRange(before.BeforeUpdateSequences(modData, resolvedImages));
					}

					var mapSequences = LoadInternalMapYaml(modData, mapPackage, mapSequencesNode.Value, externalFilenames);
					manualSteps.AddRange(ApplyTopLevelTransform(modData, mapSequences, rule.UpdateSequenceNode));
					files.AddRange(mapSequences);
				}

				manualSteps.AddRange(rule.AfterUpdate(modData));
			}

			return manualSteps;
		}

		public static List<MiniYamlNodeBuilder> LoadMapYaml(
			IReadOnlyFileSystem fileSystem, IReadOnlyPackage mapPackage, IEnumerable<string> files, MiniYamlBuilder mapNode)
		{
			var yaml = files.Select(s => MiniYaml.FromStream(fileSystem.Open(s), s)).ToList();

			if (mapNode != null && mapNode.Value != null)
			{
				var mapFiles = FieldLoader.GetValue<string[]>("value", mapNode.Value);
				yaml.AddRange(mapFiles.Select(filename =>
				{
					// Explicit package paths never refer to a map
					if (!filename.Contains('|') && mapPackage.Contains(filename))
						return MiniYaml.FromStream(mapPackage.GetStream(filename), $"{mapPackage.Name}:{filename}");

					return MiniYaml.FromStream(fileSystem.Open(filename), filename);
				}));
			}

			if (mapNode != null && mapNode.Nodes.Count > 0)
				yaml.Add(mapNode.Nodes.ConvertAll(n => n.Build()));

			return MiniYaml.Merge(yaml).ConvertAll(n => new MiniYamlNodeBuilder(n));
		}

		public static IEnumerable<string> FilterExternalFiles(ModData modData, IEnumerable<string> files, HashSet<string> externalFilenames)
		{
			foreach (var f in files)
			{
				if (f.Contains('|') && modData.DefaultFileSystem.IsExternalFile(f))
				{
					externalFilenames.Add(f);
					continue;
				}

				yield return f;
			}
		}

		public static List<string> UpdateMod(ModData modData, UpdateRule rule, out YamlFileSet files, HashSet<string> externalFilenames)
		{
			var manualSteps = new List<string>();

			var modRules = LoadModYaml(modData, FilterExternalFiles(modData, modData.Manifest.Rules, externalFilenames));
			var modWeapons = LoadModYaml(modData, FilterExternalFiles(modData, modData.Manifest.Weapons, externalFilenames));
			var modSequences = LoadModYaml(modData, FilterExternalFiles(modData, modData.Manifest.Sequences, externalFilenames));
			var modTilesets = LoadModYaml(modData, FilterExternalFiles(modData, modData.Manifest.TileSets, externalFilenames));
			var modChromeLayout = LoadModYaml(modData, FilterExternalFiles(modData, modData.Manifest.ChromeLayout, externalFilenames));
			var modChromeProvider = LoadModYaml(modData, FilterExternalFiles(modData, modData.Manifest.Chrome, externalFilenames));

			// Find and add shared map includes
			foreach (var package in modData.MapCache.EnumerateMapPackagesWithoutCaching())
			{
				using (var mapStream = package.GetStream("map.yaml"))
				{
					if (mapStream == null)
						continue;

					var yaml = new MiniYamlBuilder(new MiniYaml(null, MiniYaml.FromStream(mapStream, $"{package.Name}:map.yaml", false)));
					var mapRulesNode = yaml.NodeWithKeyOrDefault("Rules");
					if (mapRulesNode != null)
						foreach (var f in LoadExternalMapYaml(modData, mapRulesNode.Value, externalFilenames))
							if (!modRules.Any(m => m.Package == f.Package && m.File == f.File))
								modRules.Add(f);

					var mapWeaponsNode = yaml.NodeWithKeyOrDefault("Weapons");
					if (mapWeaponsNode != null)
						foreach (var f in LoadExternalMapYaml(modData, mapWeaponsNode.Value, externalFilenames))
							if (!modWeapons.Any(m => m.Package == f.Package && m.File == f.File))
								modWeapons.Add(f);

					var mapSequencesNode = yaml.NodeWithKeyOrDefault("Sequences");
					if (mapSequencesNode != null)
						foreach (var f in LoadExternalMapYaml(modData, mapSequencesNode.Value, externalFilenames))
							if (!modSequences.Any(m => m.Package == f.Package && m.File == f.File))
								modSequences.Add(f);
				}
			}

			manualSteps.AddRange(rule.BeforeUpdate(modData));

			if (rule is IBeforeUpdateActors beforeActors)
			{
				var resolvedActors = MiniYaml.Load(modData.DefaultFileSystem, modData.Manifest.Rules, null)
					.ConvertAll(n => new MiniYamlNodeBuilder(n));
				manualSteps.AddRange(beforeActors.BeforeUpdateActors(modData, resolvedActors));
			}

			manualSteps.AddRange(ApplyTopLevelTransform(modData, modRules, rule.UpdateActorNode));

			if (rule is IBeforeUpdateWeapons beforeWeapons)
			{
				var resolvedWeapons = MiniYaml.Load(modData.DefaultFileSystem, modData.Manifest.Weapons, null)
					.ConvertAll(n => new MiniYamlNodeBuilder(n));
				manualSteps.AddRange(beforeWeapons.BeforeUpdateWeapons(modData, resolvedWeapons));
			}

			manualSteps.AddRange(ApplyTopLevelTransform(modData, modWeapons, rule.UpdateWeaponNode));

			if (rule is IBeforeUpdateSequences beforeSequences)
			{
				var resolvedImages = MiniYaml.Load(modData.DefaultFileSystem, modData.Manifest.Sequences, null)
					.ConvertAll(n => new MiniYamlNodeBuilder(n));
				manualSteps.AddRange(beforeSequences.BeforeUpdateSequences(modData, resolvedImages));
			}

			manualSteps.AddRange(ApplyTopLevelTransform(modData, modSequences, rule.UpdateSequenceNode));

			manualSteps.AddRange(ApplyTopLevelTransform(modData, modTilesets, rule.UpdateTilesetNode));
			manualSteps.AddRange(ApplyChromeTransform(modData, modChromeLayout, rule.UpdateChromeNode));
			manualSteps.AddRange(ApplyTopLevelTransform(modData, modChromeProvider, rule.UpdateChromeProviderNode));
			manualSteps.AddRange(rule.AfterUpdate(modData));

			files = modRules.ToList();
			files.AddRange(modWeapons);
			files.AddRange(modSequences);
			files.AddRange(modTilesets);
			files.AddRange(modChromeLayout);
			files.AddRange(modChromeProvider);

			return manualSteps;
		}

		static IEnumerable<string> ApplyChromeTransformInner(ModData modData, MiniYamlNodeBuilder current, UpdateRule.ChromeNodeTransform transform)
		{
			foreach (var manualStep in transform(modData, current))
				yield return manualStep;

			var childrenNode = current.Value.NodeWithKeyOrDefault("Children");
			if (childrenNode != null)
				foreach (var node in childrenNode.Value.Nodes)
					if (node.Key != null)
						foreach (var manualStep in ApplyChromeTransformInner(modData, node, transform))
							yield return manualStep;
		}

		static IEnumerable<string> ApplyChromeTransform(ModData modData, YamlFileSet files, UpdateRule.ChromeNodeTransform transform)
		{
			if (transform == null)
				yield break;

			foreach (var (_, _, nodes) in files)
				foreach (var node in nodes)
					if (node.Key != null)
						foreach (var manualStep in ApplyChromeTransformInner(modData, node, transform))
							yield return manualStep;
		}

		static IEnumerable<string> ApplyTopLevelTransform(ModData modData, YamlFileSet files, UpdateRule.TopLevelNodeTransform transform)
		{
			if (transform == null)
				yield break;

			foreach (var (_, _, nodes) in files)
				foreach (var node in nodes)
					if (node.Key != null)
						foreach (var manualStep in transform(modData, node))
							yield return manualStep;
		}

		public static string FormatMessageList(IEnumerable<string> messages, int indent = 0, string separator = "*")
		{
			var prefix = string.Concat(Enumerable.Repeat("   ", indent));
			return string.Join("\n", messages.Select(m => prefix + $" {separator} {m.Replace("\n", "\n   " + prefix)}"));
		}

		public static bool IsAlreadyExtracted(string key)
		{
#pragma warning disable CA1862 // Use the 'StringComparison' method overloads to perform case-insensitive string comparisons
			if (key == key.ToLowerInvariant() && key.Any(c => c == '-') && key.All(c => c != ' '))
#pragma warning restore CA1862
			{
				Console.WriteLine($"Skipping {key} because it is already extracted.");
				return true;
			}

			return false;
		}
	}

	public static class UpdateExtensions
	{
		public static void Save(this YamlFileSet files)
		{
			foreach (var (package, file, nodes) in files)
			{
				if (package == null)
					continue;

				var textData = Encoding.UTF8.GetBytes(nodes.WriteToString());
				if (!Enumerable.SequenceEqual(textData, package.GetStream(file).ReadAllBytes()))
					package.Update(file, textData);
			}
		}

		/// <summary>Checks if node is a removal (has '-' prefix).</summary>
		public static bool IsRemoval(this MiniYamlNodeBuilder node)
		{
			return node.Key[0].ToString() == "-";
		}

		/// <summary>Renames a yaml key preserving any @suffix.</summary>
		public static void RenameKey(
			this MiniYamlNodeBuilder node, string newKey, bool preserveSuffix = true, bool includeRemovals = true)
		{
			var prefix = includeRemovals && node.IsRemoval() ? "-" : "";
			var split = node.Key.IndexOf('@');
			if (preserveSuffix && split > -1)
				node.Key = prefix + newKey + node.Key[split..];
			else
				node.Key = prefix + newKey;
		}

		public static T NodeValue<T>(this MiniYamlNodeBuilder node)
		{
			return FieldLoader.GetValue<T>(node.Key, node.Value.Value);
		}

		public static void ReplaceValue(this MiniYamlNodeBuilder node, string value)
		{
			node.Value.Value = value;
		}

		public static void AddNode(this MiniYamlNodeBuilder node, string key, object value)
		{
			node.Value.Nodes.Add(new MiniYamlNodeBuilder(key, FieldSaver.FormatValue(value)));
		}

		public static void AddNode(this MiniYamlNodeBuilder node, MiniYamlNodeBuilder toAdd)
		{
			node.Value.Nodes.Add(toAdd);
		}

		public static void RemoveNode(this MiniYamlNodeBuilder node, MiniYamlNodeBuilder toRemove)
		{
			node.Value.Nodes.Remove(toRemove);
		}

		public static void MoveNode(this MiniYamlNodeBuilder node, MiniYamlNodeBuilder fromNode, MiniYamlNodeBuilder toNode)
		{
			toNode.Value.Nodes.Add(node);
			fromNode.Value.Nodes.Remove(node);
		}

		public static void MoveAndRenameNode(
			this MiniYamlNodeBuilder node,
			MiniYamlNodeBuilder fromNode, MiniYamlNodeBuilder toNode, string newKey, bool preserveSuffix = true, bool includeRemovals = true)
		{
			node.RenameKey(newKey, preserveSuffix, includeRemovals);
			node.MoveNode(fromNode, toNode);
		}

		/// <summary>Removes children with keys equal to [match] or [match]@[arbitrary suffix].</summary>
		public static int RemoveNodes(
			this MiniYamlNodeBuilder node, string match, bool ignoreSuffix = true, bool includeRemovals = true)
		{
			return node.Value.Nodes.RemoveAll(n => n.KeyMatches(match, ignoreSuffix, includeRemovals));
		}

		/// <summary>Returns true if the node is of the form [match] or [match]@[arbitrary suffix].</summary>
		public static bool KeyMatches(
			this MiniYamlNodeBuilder node, string match, bool ignoreSuffix = true, bool includeRemovals = true)
		{
			if (node.Key == null)
				return false;

			var prefix = includeRemovals && node.IsRemoval() ? "-" : "";
			if (node.Key == prefix + match)
				return true;

			// If the previous check didn't return true and we wanted the suffix to match, return false unconditionally here
			if (!ignoreSuffix)
				return false;

			var atPosition = node.Key.IndexOf('@');
			return atPosition > 0 && node.Key[..atPosition] == prefix + match;
		}

		/// <summary>Returns true if the node is of the form [match], [match]@[arbitrary suffix] or [arbitrary suffix]@[match].</summary>
		public static bool KeyContains(
			this MiniYamlNodeBuilder node, string match, bool ignoreSuffix = true, bool includeRemovals = true)
		{
			if (node.Key == null)
				return false;

			var atPosition = node.Key.IndexOf('@');
			var relevantPart = ignoreSuffix && atPosition > 0 ? node.Key[..atPosition] : node.Key;

			if (relevantPart.Contains(match) && (includeRemovals || !node.IsRemoval()))
				return true;

			return false;
		}

		/// <summary>Returns children with keys equal to [match] or [match]@[arbitrary suffix].</summary>
		public static IEnumerable<MiniYamlNodeBuilder> ChildrenMatching(
			this MiniYamlNodeBuilder node, string match, bool ignoreSuffix = true, bool includeRemovals = true)
		{
			return node.Value.Nodes.Where(n => n.KeyMatches(match, ignoreSuffix, includeRemovals));
		}

		/// <summary>Returns children whose keys contain 'match' (optionally in the suffix).</summary>
		public static IEnumerable<MiniYamlNodeBuilder> ChildrenContaining(
			this MiniYamlNodeBuilder node, string match, bool ignoreSuffix = true, bool includeRemovals = true)
		{
			return node.Value.Nodes.Where(n => n.KeyContains(match, ignoreSuffix, includeRemovals));
		}

		public static MiniYamlNodeBuilder LastChildMatching(
			this MiniYamlNodeBuilder node, string match, bool includeRemovals = true)
		{
			return node.ChildrenMatching(match, includeRemovals: includeRemovals).LastOrDefault();
		}

		public static void RenameChildrenMatching(
			this MiniYamlNodeBuilder node, string match, string newKey, bool preserveSuffix = true, bool includeRemovals = true)
		{
			var matching = node.ChildrenMatching(match);
			foreach (var m in matching)
				m.RenameKey(newKey, preserveSuffix, includeRemovals);
		}
	}
}
