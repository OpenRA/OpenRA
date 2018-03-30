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
		static YamlFileSet LoadYaml(ModData modData, IEnumerable<string> files)
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

		static YamlFileSet LoadMapYaml(ModData modData, IReadWritePackage mapPackage, MiniYaml yaml)
		{
			var fileSet = new YamlFileSet()
			{
				Tuple.Create<IReadWritePackage, string, List<MiniYamlNode>>(null, "map.yaml", yaml.Nodes)
			};

			var files = FieldLoader.GetValue<string[]>("value", yaml.Value);
			foreach (var filename in files)
			{
				if (!filename.Contains("|") && mapPackage.Contains(filename))
					fileSet.Add(Tuple.Create(mapPackage, filename, MiniYaml.FromStream(mapPackage.GetStream(filename), filename)));
				else
					fileSet.AddRange(LoadYaml(modData, new[] { filename }));
			}

			return fileSet;
		}

		public static List<string> UpdateMap(ModData modData, IReadWritePackage mapPackage, UpdateRule rule, out YamlFileSet files)
		{
			var manualSteps = new List<string>();

			var mapStream = mapPackage.GetStream("map.yaml");
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
				var mapRules = LoadMapYaml(modData, mapPackage, mapRulesNode.Value);
				manualSteps.AddRange(ApplyTopLevelTransform(modData, mapRules, rule.UpdateActorNode));
				files.AddRange(mapRules);
			}

			var mapWeaponsNode = yaml.Nodes.FirstOrDefault(n => n.Key == "Weapons");
			if (mapWeaponsNode != null)
			{
				var mapWeapons = LoadMapYaml(modData, mapPackage, mapWeaponsNode.Value);
				manualSteps.AddRange(ApplyTopLevelTransform(modData, mapWeapons, rule.UpdateWeaponNode));
				files.AddRange(mapWeapons);
			}

			manualSteps.AddRange(rule.AfterUpdate(modData));

			return manualSteps;
		}

		public static List<string> UpdateMod(ModData modData, UpdateRule rule, out YamlFileSet files)
		{
			var manualSteps = new List<string>();
			var modRules = LoadYaml(modData, modData.Manifest.Rules);
			var modWeapons = LoadYaml(modData, modData.Manifest.Weapons);
			var modTilesets = LoadYaml(modData, modData.Manifest.TileSets);
			var modChromeLayout = LoadYaml(modData, modData.Manifest.ChromeLayout);

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
