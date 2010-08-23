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
			public bool AllowCheats = false;
		}
	}
}
