#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA
{
	public enum TileShape { Rectangle, Diamond }

	// Describes what is to be loaded in order to run a mod
	public class Manifest
	{
		public readonly ModMetadata Mod;
		public readonly string[]
			Folders, Rules, ServerTraits,
			Sequences, VoxelSequences, Cursors, Chrome, Assemblies, ChromeLayout,
			Weapons, Voices, Notifications, Music, Movies, Translations, TileSets,
			ChromeMetrics, MapCompatibility, Missions;

		public readonly IReadOnlyDictionary<string, string> Packages;
		public readonly IReadOnlyDictionary<string, string> MapFolders;
		public readonly MiniYaml LoadScreen;
		public readonly MiniYaml LobbyDefaults;
		public readonly InstallData ContentInstaller;
		public readonly Dictionary<string, Pair<string, int>> Fonts;
		public readonly Size TileSize = new Size(24, 24);
		public readonly TileShape TileShape = TileShape.Rectangle;

		public readonly string[] SpriteFormats = { };

		[Desc("(x,y,z) offset of the full cell and each sub-cell", "X & Y should be between -512 ... 512 and Z >= 0")]
		public readonly WVec[] SubCellOffsets =
		{
			new WVec(0, 0, 0),       // full cell - index 0
			new WVec(-299, -256, 0), // top left - index 1
			new WVec(256, -256, 0),  // top right - index 2
			new WVec(0, 0, 0),       // center - index 3
			new WVec(-299, 256, 0),  // bottom left - index 4
			new WVec(256, 256, 0),   // bottom right - index 5
		};

		[Desc("Default subcell index used if SubCellInit is absent", "0 - full cell, 1 - first sub-cell")]
		public readonly int SubCellDefaultIndex = 3;

		public Manifest(string mod)
		{
			var path = Platform.ResolvePath(".", "mods", mod, "mod.yaml");
			var yaml = new MiniYaml(null, MiniYaml.FromFile(path)).ToDictionary();

			Mod = FieldLoader.Load<ModMetadata>(yaml["Metadata"]);
			Mod.Id = mod;

			// TODO: Use fieldloader
			Folders = YamlList(yaml, "Folders", true);
			MapFolders = YamlDictionary(yaml, "MapFolders", true);
			Packages = YamlDictionary(yaml, "Packages", true);
			Rules = YamlList(yaml, "Rules", true);
			Sequences = YamlList(yaml, "Sequences", true);
			VoxelSequences = YamlList(yaml, "VoxelSequences", true);
			Cursors = YamlList(yaml, "Cursors", true);
			Chrome = YamlList(yaml, "Chrome", true);
			Assemblies = YamlList(yaml, "Assemblies", true);
			ChromeLayout = YamlList(yaml, "ChromeLayout", true);
			Weapons = YamlList(yaml, "Weapons", true);
			Voices = YamlList(yaml, "Voices", true);
			Notifications = YamlList(yaml, "Notifications", true);
			Music = YamlList(yaml, "Music", true);
			Movies = YamlList(yaml, "Movies", true);
			Translations = YamlList(yaml, "Translations", true);
			TileSets = YamlList(yaml, "TileSets", true);
			ChromeMetrics = YamlList(yaml, "ChromeMetrics", true);
			Missions = YamlList(yaml, "Missions", true);

			ServerTraits = YamlList(yaml, "ServerTraits");
			LoadScreen = yaml["LoadScreen"];
			LobbyDefaults = yaml["LobbyDefaults"];

			if (yaml.ContainsKey("ContentInstaller"))
				ContentInstaller = FieldLoader.Load<InstallData>(yaml["ContentInstaller"]);

			Fonts = yaml["Fonts"].ToDictionary(my =>
				{
					var nd = my.ToDictionary();
					return Pair.New(nd["Font"].Value, Exts.ParseIntegerInvariant(nd["Size"].Value));
				});

			if (yaml.ContainsKey("TileSize"))
				TileSize = FieldLoader.GetValue<Size>("TileSize", yaml["TileSize"].Value);

			if (yaml.ContainsKey("TileShape"))
				TileShape = FieldLoader.GetValue<TileShape>("TileShape", yaml["TileShape"].Value);

			if (yaml.ContainsKey("SubCells"))
			{
				var subcells = yaml["SubCells"].ToDictionary();

				// Read (x,y,z) offset (relative to cell center) pairs for positioning subcells
				if (subcells.ContainsKey("Offsets"))
					SubCellOffsets = FieldLoader.GetValue<WVec[]>("Offsets", subcells["Offsets"].Value);

				if (subcells.ContainsKey("DefaultIndex"))
					SubCellDefaultIndex = FieldLoader.GetValue<int>("DefaultIndex", subcells["DefaultIndex"].Value);
				else	// Otherwise set the default subcell index to the middle subcell entry
					SubCellDefaultIndex = SubCellOffsets.Length / 2;
			}

			// Validate default index - 0 for no subcells, otherwise > 1 & <= subcell count (offset triples count - 1)
			if (SubCellDefaultIndex < (SubCellOffsets.Length > 1 ? 1 : 0) || SubCellDefaultIndex >= SubCellOffsets.Length)
				throw new InvalidDataException("Subcell default index must be a valid index into the offset triples and must be greater than 0 for mods with subcells");

			// Allow inherited mods to import parent maps.
			var compat = new List<string>();
			compat.Add(mod);

			if (yaml.ContainsKey("SupportsMapsFrom"))
				foreach (var c in yaml["SupportsMapsFrom"].Value.Split(','))
					compat.Add(c.Trim());

			MapCompatibility = compat.ToArray();

			if (yaml.ContainsKey("SpriteFormats"))
				SpriteFormats = FieldLoader.GetValue<string[]>("SpriteFormats", yaml["SpriteFormats"].Value);
		}

		static string[] YamlList(Dictionary<string, MiniYaml> yaml, string key, bool parsePaths = false)
		{
			if (!yaml.ContainsKey(key))
				return new string[] { };

			var list = yaml[key].ToDictionary().Keys.ToArray();
			return parsePaths ? list.Select(Platform.ResolvePath).ToArray() : list;
		}

		static IReadOnlyDictionary<string, string> YamlDictionary(Dictionary<string, MiniYaml> yaml, string key, bool parsePaths = false)
		{
			if (!yaml.ContainsKey(key))
				return new ReadOnlyDictionary<string, string>();

			Func<string, string> keySelector = parsePaths ? (Func<string, string>)Platform.ResolvePath : k => k;
			var inner = yaml[key].ToDictionary(keySelector, my => my.Value);

			return new ReadOnlyDictionary<string, string>(inner);
		}
	}
}
