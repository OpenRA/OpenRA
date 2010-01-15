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
			public int Palette;
			public int Race;
			// public int SpawnPoint;
			public string Name;
			public ClientState State;
		}

		public class Global
		{
			public string Map = "scm12ea.ini";
			public string[] Mods = {};	// filename:sha1 pairs.
			public int OrderLatency = 3;
		}
	}
}
