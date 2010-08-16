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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CreateMPPlayersInfo : TraitInfo<CreateMPPlayers> { }

	public class CreateMPPlayers : ICreatePlayers
	{
		public Dictionary<string, Player> Players = new Dictionary<string, Player>();

		public void CreatePlayers(World w)
		{
			// Add real players
			w.SetLocalPlayer(Game.LocalClientId);

			foreach (var c in Game.LobbyInfo.Clients)
				w.AddPlayer(new Player(w, c));
			
			foreach (var p in w.players.Values)
				foreach (var q in w.players.Values)
				{
					if (!p.Stances.ContainsKey(q))
						p.Stances[q] = Game.ChooseInitialStance(p, q);
				}
		}
	}
}
