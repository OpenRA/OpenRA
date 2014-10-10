#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
			ChromeMetrics, LuaScripts, MapCompatibility, Missions;

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
			var path = new[] { "mods", mod, "mod.yaml" }.Aggregate(Path.Combine);
			var yaml = new MiniYaml(null, MiniYaml.FromFile(path)).ToDictionary();

			Mod = FieldLoader.Load<ModMetadata>(yaml["Metadata"]);
			Mod.Id = mod;

			// TODO: Use fieldloader
			Folders = YamlList(yaml, "Folders");
			MapFolders = YamlDictionary(yaml, "MapFolders");
			Packages = YamlDictionary(yaml, "Packages");
			Rules = YamlList(yaml, "Rules");
			ServerTraits = YamlList(yaml, "ServerTraits");
			Sequences = YamlList(yaml, "Sequences");
			VoxelSequences = YamlList(yaml, "VoxelSequences");
			Cursors = YamlList(yaml, "Cursors");
			Chrome = YamlList(yaml, "Chrome");
			Assemblies = YamlList(yaml, "Assemblies");
			ChromeLayout = YamlList(yaml, "ChromeLayout");
			Weapons = YamlList(yaml, "Weapons");
			Voices = YamlList(yaml, "Voices");
			Notifications = YamlList(yaml, "Notifications");
			Music = YamlList(yaml, "Music");
			Movies = YamlList(yaml, "Movies");
			Translations = YamlList(yaml, "Translations");
			TileSets = YamlList(yaml, "TileSets");
			ChromeMetrics = YamlList(yaml, "ChromeMetrics");
			LuaScripts = YamlList(yaml, "LuaScripts");
			Missions = YamlList(yaml, "Missions");

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

		static string[] YamlList(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.ContainsKey(key))
				return new string[] { };

			return yaml[key].ToDictionary().Keys.ToArray();
		}

		static IReadOnlyDictionary<string, string> YamlDictionary(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.ContainsKey(key))
				return new ReadOnlyDictionary<string, string>();

			var inner = yaml[key].ToDictionary(my => my.Value);
			return new ReadOnlyDictionary<string, string>(inner);
		}
	}
}
