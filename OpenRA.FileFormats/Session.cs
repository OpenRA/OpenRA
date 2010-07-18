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
	// todo: ship most of this back to the Game assembly;
	// it was only in FileFormats due to the original server model,
	// in a sep. process.

	public class Session
	{
		public List<Client> Clients = new List<Client>();
		public Global GlobalSettings = new Global();

		public enum ClientState
		{
			NotReady,
			Ready
		}

		public class Client
		{
			public int Index;
			public System.Drawing.Color Color1;
			public System.Drawing.Color Color2;
			public string Country;
			public int SpawnPoint;
			public string Name;
			public ClientState State;
			public int Team;
		}

		public class Global
		{
			public string Map;
			public string[] Mods = { "ra" };	// mod names
			public int OrderLatency = 3;
			public int RandomSeed = 0;
			public bool LockTeams = false;	// don't allow team changes after game start.
		}
	}

	public class Manifest
	{
		public readonly string[] 
			Folders, Packages, Rules, 
			Sequences, Chrome, Assemblies, ChromeLayout, 
			Weapons, Voices, Music, TileSets;
		
		public readonly string ShellmapUid;

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
			TileSets = YamlList(yaml, "TileSets");
						
			ShellmapUid = yaml["ShellmapUid"].Value;
		}

		static string[] YamlList(Dictionary<string, MiniYaml> ys, string key)
		{
			return ys.ContainsKey(key) ? ys[key].Nodes.Keys.ToArray() : new string[] { };
		}
	}
}
