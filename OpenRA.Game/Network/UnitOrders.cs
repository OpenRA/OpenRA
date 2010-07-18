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
			switch( order.OrderString )
			{
			case "Chat":
				{
					var client = Game.LobbyInfo.Clients.FirstOrDefault(c => c.Index == clientId);
					if (client != null)
						Game.AddChatLine(client.Color1, 
							client.Name, order.TargetString);
					break;
				}
			case "TeamChat":
				{
					var client = Game.LobbyInfo.Clients.FirstOrDefault(c => c.Index == clientId);
					if (client != null)
					{
						var player = Game.world.players.Values.FirstOrDefault(p => p.Index == client.Index);
						var isAlly = (world.GameHasStarted) ? 
							player != null && Game.world.LocalPlayer != null && player.Stances[Game.world.LocalPlayer] == Stance.Ally :
							client == Game.LocalClient || (client.Team == Game.LocalClient.Team && client.Team != 0);

						if (isAlly)
							Game.AddChatLine(client.Color1, client.Name + " (Team)", order.TargetString);
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
					order.Player.Stances[targetPlayer] = (Stance)order.TargetLocation.Y;
					Game.Debug("{0} has set diplomatic stance vs {1} to {2}".F(
						order.Player.PlayerName, targetPlayer.PlayerName, (Stance)order.TargetLocation.Y));
					break;
				}
			default:
				{
					if( !order.IsImmediate )
						foreach (var t in order.Subject.traits.WithInterface<IResolveOrder>())
							t.ResolveOrder(order.Subject, order);
					break;
				}
			}
		}
	}
}
