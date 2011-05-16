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
using System.Linq;

namespace OpenRA.FileFormats
{
	/* describes what is to be loaded in order to run a set of mods */

	public class Manifest
	{
		public readonly string[]
			Mods, Folders, Packages, Rules, ServerTraits,
			Sequences, Cursors, Chrome, Assemblies, ChromeLayout,
			Weapons, Voices, Music, Movies, TileSets, ChromeMetrics;
		public readonly MiniYaml LoadScreen;
		public readonly Dictionary<string, Pair<string,int>> Fonts;
		public readonly int TileSize = 24;

		public Manifest(string[] mods)
		{
			Mods = mods;
			var yaml = mods
				.Select(m => MiniYaml.FromFile("mods/" + m + "/mod.yaml"))
				.Aggregate(MiniYaml.MergeLiberal);
			
			// Todo: Use fieldloader
			Folders = YamlList(yaml, "Folders");
			Packages = YamlList(yaml, "Packages");
			Rules = YamlList(yaml, "Rules");
			ServerTraits = YamlList(yaml, "ServerTraits");
			Sequences = YamlList(yaml, "Sequences");
			Cursors = YamlList(yaml, "Cursors");
			Chrome = YamlList(yaml, "Chrome");
			Assemblies = YamlList(yaml, "Assemblies");
			ChromeLayout = YamlList(yaml, "ChromeLayout");
			Weapons = YamlList(yaml, "Weapons");
			Voices = YamlList(yaml, "Voices");
			Music = YamlList(yaml, "Music");
			Movies = YamlList(yaml, "Movies");
			TileSets = YamlList(yaml, "TileSets");
			ChromeMetrics = YamlList(yaml, "ChromeMetrics");
			LoadScreen = yaml.First( x => x.Key == "LoadScreen" ).Value;
			Fonts = yaml.First( x => x.Key == "Fonts" ).Value
				.NodesDict.ToDictionary(x => x.Key, x => Pair.New(x.Value.NodesDict["Font"].Value,
				                                                  int.Parse(x.Value.NodesDict["Size"].Value)));
			if (yaml.FirstOrDefault( x => x.Key == "TileSize" ) != null)
				TileSize = int.Parse(yaml.First( x => x.Key == "TileSize" ).Value.Value);
		}

		static string[] YamlList(List<MiniYamlNode> ys, string key)
		{
			var y = ys.FirstOrDefault( x => x.Key == key );
			if( y == null )
				return new string[ 0 ];

			return y.Value.NodesDict.Keys.ToArray();
		}
	}
}
