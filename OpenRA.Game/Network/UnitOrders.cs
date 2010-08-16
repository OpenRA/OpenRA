#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Network
{
	static class UnitOrders
	{
		public static void ProcessOrder( World world, int clientId, Order order )
		{
			// Drop exploiting orders
			if (order.Subject != null && order.Subject.Owner != world.players[clientId])
				return;
			
			switch( order.OrderString )
			{
			case "Chat":
				{
					var client = Game.LobbyInfo.Clients.FirstOrDefault(c => c.Index == clientId);
					if (client != null)
					{
						var player = world.players.Values.FirstOrDefault(p => p.Index == client.Index);
						if (player != null && player.WinState == WinState.Lost)
							Game.AddChatLine(client.Color1, client.Name + " (Dead)", order.TargetString);
						else
							Game.AddChatLine(client.Color1, client.Name, order.TargetString);
					}
					break;
				}
			case "TeamChat":
				{
					var client = Game.LobbyInfo.Clients.FirstOrDefault(c => c.Index == clientId);
					if (client != null)
					{
						var player = world.players.Values.FirstOrDefault(p => p.Index == client.Index);
						var display = (world.GameHasStarted) ? 
							player != null && (world.LocalPlayer != null && player.Stances[world.LocalPlayer] == Stance.Ally 
								|| player.WinState == WinState.Lost) :
							client == Game.LocalClient || (client.Team == Game.LocalClient.Team && client.Team != 0);
						
						if (display)
						{
							var suffix = (player != null && player.WinState == WinState.Lost) ? " (Dead)" : " (Team)";
							Game.AddChatLine(client.Color1, client.Name + suffix, order.TargetString);
						}
					}
					break;
				}
			case "StartGame":
				{
					Game.AddChatLine(Color.White, "Server", "The game has started.");
					Game.StartGame();
					break;
				}
			case "SyncInfo":
				{
					Game.SyncLobbyInfo(order.TargetString);
					break;
				}
			case "SetStance":
				{
					var targetPlayer = order.Player.World.players[order.TargetLocation.X];
					var oldStance = order.Player.Stances[targetPlayer];
					order.Player.Stances[targetPlayer] = (Stance)order.TargetLocation.Y;
					
					if (targetPlayer == world.LocalPlayer)
						world.WorldActor.Trait<Shroud>().UpdatePlayerStance(world, order.Player, oldStance, order.Player.Stances[targetPlayer]);
					
					Game.Debug("{0} has set diplomatic stance vs {1} to {2}".F(
						order.Player.PlayerName, targetPlayer.PlayerName, order.Player.Stances[targetPlayer]));
					break;
				}
			default:
				{
					if( !order.IsImmediate )
						foreach (var t in order.Subject.TraitsImplementing<IResolveOrder>())
							t.ResolveOrder(order.Subject, order);
					break;
				}
			}
		}
	}
}
