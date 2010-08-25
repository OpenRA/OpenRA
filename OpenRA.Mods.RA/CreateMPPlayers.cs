#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CreateMPPlayersInfo : TraitInfo<CreateMPPlayers> { }

	public class CreateMPPlayers : ICreatePlayers
	{
		public void CreatePlayers(World w)
		{
			var playerIndex = 0;
			foreach (var slot in Game.LobbyInfo.Slots)
			{
				var client = Game.LobbyInfo.Clients.FirstOrDefault(c => c.Slot == slot.Index);
				if (client != null)
				{
					/* spawn a real player in this slot. */
					var player = new Player(w, client, w.Map.Players[slot.MapPlayer], playerIndex++);
					w.AddPlayer(player);
					if (client.Index == Game.LocalClientId)
						w.SetLocalPlayer(player.Index);		// bind this one to the local player.
				}
				else if (slot.Bot != null)
				{
					/* spawn a bot in this slot, "owned" by the host */
					var player = new Player(w, w.Map.Players[slot.MapPlayer], playerIndex++);
					w.AddPlayer(player);
					
					/* todo: only activate the bot option that's selected! */
					if (Game.IsHost)
						foreach (var bot in player.PlayerActor.TraitsImplementing<IBot>())
							bot.Activate(player);
				}
			}
			
			foreach (var p in w.players.Values)
				foreach (var q in w.players.Values)
				{
					if (!p.Stances.ContainsKey(q))
						p.Stances[q] = ChooseInitialStance(p, q);
				}
		}

		static Stance ChooseInitialStance(Player p, Player q)
		{
			if (p == q) return Stance.Ally;

			// Hack: All map players are neutral wrt everyone else
			if (p.Index < 0 || q.Index < 0) return Stance.Neutral;

			var pc = GetClientForPlayer(p);
			var qc = GetClientForPlayer(q);

			return pc.Team != 0 && pc.Team == qc.Team
				? Stance.Ally : Stance.Enemy;
		}

		static Session.Client GetClientForPlayer(Player p)
		{
			return Game.LobbyInfo.Clients.Single(c => c.Index == p.ClientIndex);
		}
	}
}
