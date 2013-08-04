#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	/* describes what is to be loaded in order to run a set of mods */

	public class Manifest
	{
		public readonly string[]
			Mods, Folders, Rules, ServerTraits,
			Sequences, VoxelSequences, Cursors, Chrome, Assemblies, ChromeLayout,
			Weapons, Voices, Notifications, Music, Movies, TileSets,
			ChromeMetrics, PackageContents;

		public readonly Dictionary<string, string> Packages;
		public readonly MiniYaml LoadScreen;
		public readonly MiniYaml LobbyDefaults;
		public readonly Dictionary<string, Pair<string,int>> Fonts;
		public readonly int TileSize = 24;

		public Manifest(string[] mods)
		{
			Mods = mods;
			var yaml = new MiniYaml(null, mods
				.Select(m => MiniYaml.FromFile("mods{0}{1}{0}mod.yaml".F(Path.DirectorySeparatorChar, m)))
				.Aggregate(MiniYaml.MergeLiberal)).NodesDict;

			// TODO: Use fieldloader
			Folders = YamlList(yaml, "Folders");
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
			TileSets = YamlList(yaml, "TileSets");
			ChromeMetrics = YamlList(yaml, "ChromeMetrics");
			PackageContents = YamlList(yaml, "PackageContents");

			LoadScreen = yaml["LoadScreen"];
			LobbyDefaults = yaml["LobbyDefaults"];
			Fonts = yaml["Fonts"].NodesDict.ToDictionary(x => x.Key,
				x => Pair.New(x.Value.NodesDict["Font"].Value,
					int.Parse(x.Value.NodesDict["Size"].Value)));

			if (yaml.ContainsKey("TileSize"))
				TileSize = int.Parse(yaml["TileSize"].Value);
		}

		static string[] YamlList(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.ContainsKey(key))
				return new string[] {};

			return yaml[key].NodesDict.Keys.ToArray();
		}
	}
}
