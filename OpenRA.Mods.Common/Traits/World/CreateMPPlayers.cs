#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

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
				if (client == null)
					continue;

				var player = new Player(w, client, kv.Value, w.Map.Players[kv.Value.PlayerReference]);
				w.AddPlayer(player);

				if (client.Index == Game.LocalClientId)
					w.SetLocalPlayer(player.InternalName);
			}

			// create a player that is allied with everyone for shared observer shroud
			w.AddPlayer(new Player(w, null, null, new PlayerReference
			{
				Name = "Everyone",
				NonCombatant = true,
				Spectating = true,
				Allies = w.Players.Where(p => !p.NonCombatant && p.Playable).Select(p => p.InternalName).ToArray()
			}));

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
