#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	using YamlFileSet = List<Tuple<IReadWritePackage, string, List<MiniYamlNode>>>;

	public static class UpdateUtils
	{
		/// <summary>
		/// Loads a YamlFileSet from a list of mod files.
		/// </summary>
		static YamlFileSet LoadModYaml(ModData modData, IEnumerable<string> files)
		{
			var yaml = new YamlFileSet();
			foreach (var filename in files)
			{
				string name;
				IReadOnlyPackage package;
				if (!modData.ModFiles.TryGetPackageContaining(filename, out package, out name) || !(package is IReadWritePackage))
				{
					Console.WriteLine("Failed to load file `{0}` for writing. It will not be updated.", filename);
					continue;
				}

				yaml.Add(Tuple.Create((IReadWritePackage)package, name, MiniYaml.FromStream(package.GetStream(name), name)));
			}

			return yaml;
		}

		/// <summary>
		/// Loads a YamlFileSet containing any external yaml definitions referenced by a map yaml block.
		/// </summary>
		static YamlFileSet LoadExternalMapYaml(ModData modData, MiniYaml yaml)
		{
			return FieldLoader.GetValue<string[]>("value", yaml.Value)
				.Where(f => f.Contains("|"))
				.SelectMany(f => LoadModYaml(modData, new[] { f }))
				.ToList();
		}

		/// <summary>
		/// Loads a YamlFileSet containing any internal definitions yaml referenced by a map yaml block.
		/// External references or internal references to missing files are ignored.
		/// </summary>
		static YamlFileSet LoadInternalMapYaml(ModData modData, IReadWritePackage mapPackage, MiniYaml yaml, HashSet<string> externalFilenames)
		{
			var fileSet = new YamlFileSet()
			{
				Tuple.Create<IReadWritePackage, string, List<MiniYamlNode>>(null, "map.yaml", yaml.Nodes)
			};

			var files = FieldLoader.GetValue<string[]>("value", yaml.Value);
			foreach (var filename in files)
			{
				// Ignore any files that aren't in the map bundle
				if (!filename.Contains("|") && mapPackage.Contains(filename))
					fileSet.Add(Tuple.Create(mapPackage, filename, MiniYaml.FromStream(mapPackage.GetStream(filename), filename)));
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
					files = new YamlFileSet();
					return manualSteps;
				}

				var yaml = new MiniYaml(null, MiniYaml.FromStream(mapStream, mapPackage.Name));
				files = new YamlFileSet() { Tuple.Create(mapPackage, "map.yaml", yaml.Nodes) };

				manualSteps.AddRange(rule.BeforeUpdate(modData));

				var mapRulesNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Rules");
				if (mapRulesNode != null)
				{
					var mapRules = LoadInternalMapYaml(modData, mapPackage, mapRulesNode.Value, externalFilenames);
					manualSteps.AddRange(ApplyTopLevelTransform(modData, mapRules, rule.UpdateActorNode));
					files.AddRange(mapRules);
				}

				var mapWeaponsNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Weapons");
				if (mapWeaponsNode != null)
				{
					var mapWeapons = LoadInternalMapYaml(modData, mapPackage, mapWeaponsNode.Value, externalFilenames);
					manualSteps.AddRange(ApplyTopLevelTransform(modData, mapWeapons, rule.UpdateWeaponNode));
					files.AddRange(mapWeapons);
				}

				manualSteps.AddRange(rule.AfterUpdate(modData));
			}

			return manualSteps;
		}

		public static List<string> UpdateMod(ModData modData, UpdateRule rule, out YamlFileSet files)
		{
			var manualSteps = new List<string>();
			var modRules = LoadModYaml(modData, modData.Manifest.Rules);
			var modWeapons = LoadModYaml(modData, modData.Manifest.Weapons);
			var modTilesets = LoadModYaml(modData, modData.Manifest.TileSets);
			var modChromeLayout = LoadModYaml(modData, modData.Manifest.ChromeLayout);

			// Find and add shared map includes
			foreach (var package in modData.MapCache.EnumerateMapPackagesWithoutCaching())
			{
				using (var mapStream = package.GetStream("map.yaml"))
				{
					if (mapStream == null)
						continue;

					var yaml = new MiniYaml(null, MiniYaml.FromStream(mapStream, package.Name));
					var mapRulesNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Rules");
					if (mapRulesNode != null)
						foreach (var f in LoadExternalMapYaml(modData, mapRulesNode.Value))
							if (!modRules.Any(m => m.Item1 == f.Item1 && m.Item2 == f.Item2))
								modRules.Add(f);

					var mapWeaponsNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Weapons");
					if (mapWeaponsNode != null)
						foreach (var f in LoadExternalMapYaml(modData, mapWeaponsNode.Value))
							if (!modWeapons.Any(m => m.Item1 == f.Item1 && m.Item2 == f.Item2))
								modWeapons.Add(f);
				}
			}

			manualSteps.AddRange(rule.BeforeUpdate(modData));
			manualSteps.AddRange(ApplyTopLevelTransform(modData, modRules, rule.UpdateActorNode));
			manualSteps.AddRange(ApplyTopLevelTransform(modData, modWeapons, rule.UpdateWeaponNode));
			manualSteps.AddRange(ApplyTopLevelTransform(modData, modTilesets, rule.UpdateTilesetNode));
			manualSteps.AddRange(ApplyChromeTransform(modData, modChromeLayout, rule.UpdateChromeNode));
			manualSteps.AddRange(rule.AfterUpdate(modData));

			files = modRules.ToList();
			files.AddRange(modWeapons);
			files.AddRange(modTilesets);
			files.AddRange(modChromeLayout);

			return manualSteps;
		}

		static IEnumerable<string> ApplyChromeTransformInner(ModData modData, MiniYamlNode current, UpdateRule.ChromeNodeTransform transform)
		{
			foreach (var manualStep in transform(modData, current))
				yield return manualStep;

			var childrenNode = current.Value.Nodes.FirstOrDefault(n => n.Key == "Children");
			if (childrenNode != null)
				foreach (var node in childrenNode.Value.Nodes)
					foreach (var manualStep in ApplyChromeTransformInner(modData, node, transform))
						yield return manualStep;
		}

		static IEnumerable<string> ApplyChromeTransform(ModData modData, YamlFileSet files, UpdateRule.ChromeNodeTransform transform)
		{
			if (transform == null)
				yield break;

			foreach (var file in files)
				foreach (var node in file.Item3)
					foreach (var manualStep in ApplyChromeTransformInner(modData, node, transform))
						yield return manualStep;
		}

		static IEnumerable<string> ApplyTopLevelTransform(ModData modData, YamlFileSet files, UpdateRule.TopLevelNodeTransform transform)
		{
			if (transform == null)
				yield break;

			foreach (var file in files)
				foreach (var node in file.Item3)
					foreach (var manualStep in transform(modData, node))
						yield return manualStep;
		}

		public static string FormatMessageList(IEnumerable<string> messages, int indent = 0)
		{
			var prefix = string.Concat(Enumerable.Repeat("   ", indent));
			return string.Join("\n", messages.Select(m => prefix + " * {0}".F(m.Replace("\n", "\n   " + prefix))));
		}
	}

	public static class UpdateExtensions
	{
		public static void Save(this YamlFileSet files)
		{
			foreach (var file in files)
				if (file.Item1 != null)
					file.Item1.Update(file.Item2, Encoding.ASCII.GetBytes(file.Item3.WriteToString()));
		}

		/// <summary>Renames a yaml key preserving any @suffix</summary>
		public static void RenameKeyPreservingSuffix(this MiniYamlNode node, string newKey)
		{
			var split = node.Key.IndexOf("@", StringComparison.Ordinal);
			if (split == -1)
				node.Key = newKey;
			else
				node.Key = newKey + node.Key.Substring(split);
		}

		public static T NodeValue<T>(this MiniYamlNode node)
		{
			return FieldLoader.GetValue<T>(node.Key, node.Value.Value);
		}

		public static void AddNode(this MiniYamlNode node, string key, object value)
		{
			node.Value.Nodes.Add(new MiniYamlNode(key, FieldSaver.FormatValue(value)));
		}

		/// <summary>Removes children with keys equal to [match] or [match]@[arbitrary suffix]</summary>
		public static int RemoveNodes(this MiniYamlNode node, string match)
		{
			return node.Value.Nodes.RemoveAll(n => n.KeyMatches(match));
		}

		/// <summary>Returns true if the node is of the form <match> or <match>@arbitrary</summary>
		public static bool KeyMatches(this MiniYamlNode node, string match)
		{
			if (node.Key == match)
				return true;

			var atPosition = node.Key.IndexOf('@');
			return atPosition > 0 && node.Key.Substring(0, atPosition) == match;
		}

		/// <summary>Returns children with keys equal to [match] or [match]@[arbitrary suffix]</summary>
		public static IEnumerable<MiniYamlNode> ChildrenMatching(this MiniYamlNode node, string match)
		{
			return node.Value.Nodes.Where(n => n.KeyMatches(match));
		}

		public static MiniYamlNode LastChildMatching(this MiniYamlNode node, string match)
		{
			return node.ChildrenMatching(match).LastOrDefault();
		}
	}
}
