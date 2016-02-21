#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to the world actor.")]
	public class CreateMPPlayersInfo : TraitInfo<CreateMPPlayers> { }

	public class CreateMPPlayers : ICreatePlayers
	{
		public void CreatePlayers(World w)
		{
			var players = new MapPlayers(w.Map.PlayerDefinitions).Players;
			var worldPlayers = new List<Player>();

			// Create the unplayable map players -- neutral, shellmap, scripted, etc.
			foreach (var kv in players.Where(p => !p.Value.Playable))
			{
				var player = new Player(w, null, kv.Value);
				worldPlayers.Add(player);
				if (kv.Value.OwnsWorld)
					w.WorldActor.Owner = player;
			}

			Player localPlayer = null;

			// Create the regular playable players.
			foreach (var kv in w.LobbyInfo.Slots)
			{
				var client = w.LobbyInfo.ClientInSlot(kv.Key);
				if (client == null)
					continue;

				var player = new Player(w, client, players[kv.Value.PlayerReference]);
				worldPlayers.Add(player);

				if (client.Index == Game.LocalClientId)
					localPlayer = player;
			}

			// Create a player that is allied with everyone for shared observer shroud.
			worldPlayers.Add(new Player(w, null, new PlayerReference
			{
				Name = "Everyone",
				NonCombatant = true,
				Spectating = true,
				Faction = "Random",
				Allies = worldPlayers.Where(p => !p.NonCombatant && p.Playable).Select(p => p.InternalName).ToArray()
			}));

			w.SetPlayers(worldPlayers, localPlayer);

			foreach (var p in w.Players)
				foreach (var q in w.Players)
					if (!p.Stances.ContainsKey(q))
						p.Stances[q] = ChooseInitialStance(p, q);
		}

		static Stance ChooseInitialStance(Player p, Player q)
		{
			if (p == q)
				return Stance.Ally;

			if (q.Spectating && !p.NonCombatant && p.Playable)
				return Stance.Ally;

			// Stances set via PlayerReference
			if (p.PlayerReference.Allies.Contains(q.InternalName))
				return Stance.Ally;
			if (p.PlayerReference.Enemies.Contains(q.InternalName))
				return Stance.Enemy;

			// HACK: Map players share a ClientID with the host, so would
			// otherwise take the host's team stance instead of being neutral
			if (p.PlayerReference.Playable && q.PlayerReference.Playable)
			{
				// Stances set via lobby teams
				var pc = GetClientForPlayer(p);
				var qc = GetClientForPlayer(q);
				if (pc != null && qc != null)
					return pc.Team != 0 && pc.Team == qc.Team
						? Stance.Ally : Stance.Enemy;
			}

			// Otherwise, default to neutral
			return Stance.Neutral;
		}

		static Session.Client GetClientForPlayer(Player p)
		{
			return p.World.LobbyInfo.ClientWithIndex(p.ClientIndex);
		}
	}
}
