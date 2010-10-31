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
			var playerIndex = 0;
			var mapPlayerIndex = -1;	// todo: unhack this, but people still rely on it.

			// create the unplayable map players -- neutral, shellmap, scripted, etc.
			foreach (var kv in w.Map.Players.Where(p => !p.Value.Playable))
			{
				var player = new Player(w, kv.Value, mapPlayerIndex--);
				w.AddPlayer(player);
				if (kv.Value.OwnsWorld)
					w.WorldActor.Owner = player;
			}

			// create the players which are bound through slots.
			foreach (var slot in w.LobbyInfo.Slots)
			{
				var client = w.LobbyInfo.Clients.FirstOrDefault(c => c.Slot == slot.Index && slot.MapPlayer != null);
				if (client != null)
				{
					/* spawn a real player in this slot. */
					var player = new Player(w, client, w.Map.Players[slot.MapPlayer], playerIndex++);
					w.AddPlayer(player);
					if (client.Index == Game.LocalClientId)
						w.SetLocalPlayer(player.Index);		// bind this one to the local player.
				}
				else if (slot.Bot != null && slot.MapPlayer != null)
				{
					/* spawn a bot in this slot, "owned" by the host */

					/* pick a random color for the bot */
					var hue = (float)w.SharedRandom.NextDouble();
					w.Map.Players[slot.MapPlayer].Color = PlayerColorRemap.ColorFromHSL(hue, 1.0f, 0.7f);
					w.Map.Players[slot.MapPlayer].Color2 = PlayerColorRemap.ColorFromHSL(hue, 1.0f, 0.2f);

					/* todo: pick a random name from the pool */

					var player = new Player(w, w.Map.Players[slot.MapPlayer], playerIndex++);
					w.AddPlayer(player);
					
					/* todo: only activate the bot option that's selected! */
					if (Game.IsHost)
						foreach (var bot in player.PlayerActor.TraitsImplementing<IBot>())
							bot.Activate(player);

					/* a bit of a hack */
					player.IsBot = true;
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
			var pc = GetClientForPlayer(p);
			var qc = GetClientForPlayer(q);

			if (p.World.LobbyInfo.Slots.Count > 0)
			{
				if (p.World.LobbyInfo.Slots[pc.Slot].Spectator) return Stance.Ally;
				if (p.World.LobbyInfo.Slots[qc.Slot].Spectator) return Stance.Ally;
			}

			if (p.PlayerRef.Allies.Contains(q.InternalName))
				return Stance.Ally;
			if (p.PlayerRef.Enemies.Contains(q.InternalName))
				return Stance.Enemy;

			// Hack: All map players are neutral wrt everyone else
			if (p.Index < 0 || q.Index < 0) return Stance.Neutral;

			if (p.IsBot ^ q.IsBot)
				return Stance.Enemy;	// bots and humans hate each other


			return pc.Team != 0 && pc.Team == qc.Team
				? Stance.Ally : Stance.Enemy;
		}

		static Session.Client GetClientForPlayer(Player p)
		{
			return p.World.LobbyInfo.ClientWithIndex(p.ClientIndex);
		}
	}
}
