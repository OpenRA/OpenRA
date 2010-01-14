using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.FileFormats
{
	public class Session
	{
		public List<Client> Clients = new List<Client>();
		public string Map = "scm12ea.ini";
		// public string[] Mods = { };
	}

	public enum ClientState
	{
		NotReady,
		// Downloading,
		// Uploading,
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
}
