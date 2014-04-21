﻿#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
	// Describes what is to be loaded in order to run a mod
	public class Manifest
	{
		public readonly Mod Mod;
		public readonly string[]
			Folders, MapFolders, Rules, ServerTraits,
			Sequences, VoxelSequences, Cursors, Chrome, Assemblies, ChromeLayout,
			Weapons, Voices, Notifications, Music, Movies, Translations, TileSets,
			ChromeMetrics, PackageContents, LuaScripts, MapCompatibility, Missions;

		public readonly Dictionary<string, string> Packages;
		public readonly MiniYaml LoadScreen;
		public readonly MiniYaml LobbyDefaults;
		public readonly Dictionary<string, Pair<string, int>> Fonts;
		public readonly Size TileSize = new Size(24, 24);

		public Manifest(string mod)
		{
			var path = new[] { "mods", mod, "mod.yaml" }.Aggregate(Path.Combine);
			var yaml = new MiniYaml(null, MiniYaml.FromFile(path)).NodesDict;

			Mod = FieldLoader.Load<Mod>(yaml["Metadata"]);
			Mod.Id = mod;

			// TODO: Use fieldloader
			Folders = YamlList(yaml, "Folders");
			MapFolders = YamlList(yaml, "MapFolders");
			Packages = yaml["Packages"].NodesDict.ToDictionary(x => x.Key, x => x.Value.Value);
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
			Fonts = yaml["Fonts"].NodesDict.ToDictionary(x => x.Key,
				x => Pair.New(x.Value.NodesDict["Font"].Value,
					int.Parse(x.Value.NodesDict["Size"].Value)));

			if (yaml.ContainsKey("TileSize"))
				TileSize = FieldLoader.GetValue<Size>("TileSize", yaml["TileSize"].Value);

			// Allow inherited mods to import parent maps.
			var compat = new List<string>();
			compat.Add(mod);

			if (yaml.ContainsKey("SupportsMapsFrom"))
				foreach (var c in yaml["SupportsMapsFrom"].Value.Split(','))
					compat.Add(c.Trim());

			MapCompatibility = compat.ToArray();
		}

		static string[] YamlList(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.ContainsKey(key))
				return new string[] { };

			return yaml[key].NodesDict.Keys.ToArray();
		}
	}
}
