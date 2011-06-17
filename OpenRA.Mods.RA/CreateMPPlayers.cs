#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CreateMPPlayersInfo : TraitInfo<CreateMPPlayers> { }

	public class CreateMPPlayers : ICreatePlayers
	{
		public void CreatePlayers(World w)
		{
			// create the unplayable map players -- neutral, shellmap, scripted, etc.
			foreach (var kv in w.Map.Players.Where(p => !p.Value.Playable))
			{
				var player = new Player(w, null, null, kv.Value);
				w.AddPlayer(player);
				if (kv.Value.OwnsWorld)
					w.WorldActor.Owner = player;
			}

			// create the players which are bound through slots.
			foreach (var kv in w.LobbyInfo.Slots)
			{
				var client = w.LobbyInfo.ClientInSlot(kv.Key);
				if (client == null && kv.Value.Bot == null)
					continue;

				var player = new Player(w, client, kv.Value, w.Map.Players[kv.Value.PlayerReference]);
				w.AddPlayer(player);

				if (client != null && client.Index == Game.LocalClientId)
					w.SetLocalPlayer(player.InternalName);
			}
			
			foreach (var p in w.Players)
				foreach (var q in w.Players)
				{
					if (!p.Stances.ContainsKey(q))
						p.Stances[q] = ChooseInitialStance(p, q);
				}
		}

		static Stance ChooseInitialStance(Player p, Player q)
		{
			if (p == q) return Stance.Ally;
			var pc = GetClientForPlayer(p);
			var qc = GetClientForPlayer(q);

			if (p.World.LobbyInfo.Slots.Count > 0)
			{
				if (p.PlayerRef == null) return Stance.Ally;
				if (q.PlayerRef == null) return Stance.Ally;
			}

			// Stances set via the player reference
			if (p.PlayerRef.Allies.Contains(q.InternalName))
				return Stance.Ally;
			if (p.PlayerRef.Enemies.Contains(q.InternalName))
				return Stance.Enemy;
			
			// Otherwise, default to neutral for map-players
			if (!p.PlayerRef.Playable || !q.PlayerRef.Playable) return Stance.Neutral;
			// or enemy for bot vs human
			if (p.IsBot ^ q.IsBot) return Stance.Enemy;

			return pc.Team != 0 && pc.Team == qc.Team
				? Stance.Ally : Stance.Enemy;
		}

		static Session.Client GetClientForPlayer(Player p)
		{
			return p.World.LobbyInfo.ClientWithIndex(p.ClientIndex);
		}
	}
}
