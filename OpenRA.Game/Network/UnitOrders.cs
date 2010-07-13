#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
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
					Game.AddChatLine(Game.world.PlayerColors()[client.PaletteIndex].Color, client.Name, order.TargetString);
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
							Game.AddChatLine(Game.world.PlayerColors()[client.PaletteIndex].Color, client.Name + " (Team)", order.TargetString);
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
