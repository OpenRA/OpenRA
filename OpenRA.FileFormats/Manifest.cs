#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
			Folders, Packages, Rules,
			Sequences, Chrome, Assemblies, ChromeLayout,
			Weapons, Voices, Music, Movies, TileSets;

		public readonly string ShellmapUid, LoadScreen;

		public Manifest(string[] mods)
		{
			var yaml = mods
				.Select(m => MiniYaml.FromFile("mods/" + m + "/mod.yaml"))
				.Aggregate(MiniYaml.Merge);

			Folders = YamlList(yaml, "Folders");
			Packages = YamlList(yaml, "Packages");
			Rules = YamlList(yaml, "Rules");
			Sequences = YamlList(yaml, "Sequences");
			Chrome = YamlList(yaml, "Chrome");
			Assemblies = YamlList(yaml, "Assemblies");
			ChromeLayout = YamlList(yaml, "ChromeLayout");
			Weapons = YamlList(yaml, "Weapons");
			Voices = YamlList(yaml, "Voices");
			Music = YamlList(yaml, "Music");
			Movies = YamlList(yaml, "Movies");
			TileSets = YamlList(yaml, "TileSets");

			ShellmapUid = yaml["ShellmapUid"].Value;
			LoadScreen = yaml["LoadScreen"].Value;
		}

		static string[] YamlList(Dictionary<string, MiniYaml> ys, string key)
		{
			return ys.ContainsKey(key) ? ys[key].Nodes.Keys.ToArray() : new string[] { };
		}
	}
}
