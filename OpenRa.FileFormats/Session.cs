using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.FileFormats
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
			public string Race;
			public int SpawnPoint;
			public string Name;
			public ClientState State;
		}

		public class Global
		{
			public string Map = "scm12ea.ini";
			public string[] Packages = {};	// filename:sha1 pairs.
			public string[] Mods = { "ra" };	// mod names
			public int OrderLatency = 3;
		}
	}

	public class Manifest
	{
		public readonly string[] Folders = { };
		public readonly string[] Packages = { };
		public readonly string[] LegacyRules = { };
		public readonly string[] Rules = { };
		public readonly string[] Sequences = { };
		public readonly string[] Chrome = { };
		public readonly string[] Assemblies = { };

		public Manifest(string[] mods)
		{
			var yaml = mods
				.Select(m => MiniYaml.FromFile("mods/" + m + "/mod.yaml"))
				.Aggregate(MiniYaml.Merge);
				
			Folders = YamlList(yaml, "Folders");
			Packages = YamlList(yaml, "Packages");
			LegacyRules = YamlList(yaml, "LegacyRules");
			Rules = YamlList(yaml, "Rules");
			Sequences = YamlList(yaml, "Sequences");
			Chrome = YamlList(yaml, "Chrome");
			Assemblies = YamlList(yaml, "Assemblies");
		}

		static string[] YamlList(Dictionary<string, MiniYaml> ys, string key) { return ys[key].Nodes.Keys.ToArray(); }
	}
}
