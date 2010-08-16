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
	public class CreateMapPlayersInfo : TraitInfo<CreateMapPlayers> { }

	public class CreateMapPlayers : ICreatePlayers
	{
		public Dictionary<string, Player> Players = new Dictionary<string, Player>();

		public void CreatePlayers(World w)
		{
			int mapPlayerIndex = -1;
			
			foreach (var kv in w.Map.Players)
			{
				var player = new Player(w, kv.Value, mapPlayerIndex--);
				w.AddPlayer(player);
				Players.Add(kv.Key,player);
				if (kv.Value.OwnsWorld)
					w.WorldActor.Owner = player;
			}
			
			foreach(var p in Players)
			{
				foreach(var q in w.Map.Players[p.Key].Allies)
					p.Value.Stances[Players[q]] = Stance.Ally;
				
				foreach(var q in w.Map.Players[p.Key].Enemies)
					p.Value.Stances[Players[q]] = Stance.Enemy;
			}
		}
	}
}
