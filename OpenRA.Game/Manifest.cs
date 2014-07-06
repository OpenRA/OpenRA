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
			ChromeMetrics, PackageContents, LuaScripts, MapCompatibility, Missions;

		public readonly IReadOnlyDictionary<string, string> Packages;
		public readonly IReadOnlyDictionary<string, string> MapFolders;
		public readonly MiniYaml LoadScreen;
		public readonly MiniYaml LobbyDefaults;
		public readonly InstallData ContentInstaller;
		public readonly Dictionary<string, Pair<string, int>> Fonts;
		public readonly Size TileSize = new Size(24, 24);
		public readonly string NewsUrl;
		public readonly TileShape TileShape = TileShape.Rectangle;

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
			PackageContents = YamlList(yaml, "PackageContents");
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

			// Allow inherited mods to import parent maps.
			var compat = new List<string>();
			compat.Add(mod);

			if (yaml.ContainsKey("SupportsMapsFrom"))
				foreach (var c in yaml["SupportsMapsFrom"].Value.Split(','))
					compat.Add(c.Trim());

			MapCompatibility = compat.ToArray();

			if (yaml.ContainsKey("NewsUrl"))
				NewsUrl = yaml["NewsUrl"].Value;
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
