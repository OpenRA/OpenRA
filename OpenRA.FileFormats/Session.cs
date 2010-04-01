#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class Session
	{
		public List<Client> Clients = new List<Client>();
		public Global GlobalSettings = new Global();

		public enum ClientState
		{
			NotReady,
			Downloading,
			Ready
		}

		public class Client
		{
			public int Index;
			public int PaletteIndex;
			public string Country;
			public int SpawnPoint;
			public string Name;
			public ClientState State;
			public int Team;
		}

		public class Global
		{
			public string Map = "mods/ra/testmap.yaml";
			public string[] Packages = {};	// filename:sha1 pairs.
			public string[] Mods = { "ra" };	// mod names
			public int OrderLatency = 3;
			public int RandomSeed = 0;
		}
	}

	public class Manifest
	{
		public readonly string[] 
			Folders, Packages, Rules, 
			Sequences, Chrome, Assemblies, ChromeLayout, 
			Weapons, Voices, Terrain;

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
			Terrain = YamlList(yaml, "Terrain");
		}

		static string[] YamlList(Dictionary<string, MiniYaml> ys, string key) { return ys[key].Nodes.Keys.ToArray(); }
	}
}
