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
		static Player FindPlayerByClient( this World world, Session.Client c)
		{
			/* todo: this is still a hack. 
			 * the cases we're trying to avoid are the extra players on the host's client -- Neutral, other MapPlayers,
			 * bots,.. */
			return world.players.Values.FirstOrDefault(
				p => p.ClientIndex == c.Index && p.PlayerName == c.Name);
		}

		public static void ProcessOrder( OrderManager orderManager, World world, int clientId, Order order )
		{
			// Drop exploiting orders
			if (order.Subject != null && order.Subject.Owner.ClientIndex != clientId)
			{
				Game.Debug("Detected exploit order from {0}: {1}".F(clientId, order.OrderString));
				return;
			}
			
			switch( order.OrderString )
			{
			case "Chat":
				{
					var client = orderManager.LobbyInfo.ClientWithIndex( clientId );
					if (client != null)
					{
						var player = world != null ? world.FindPlayerByClient(client) : null;
						var suffix = (player != null && player.WinState == WinState.Lost) ? " (Dead)" : "";
						Game.AddChatLine(client.Color1, client.Name+suffix, order.TargetString);
					}
					else
						Game.AddChatLine(Color.White, "(player {0})".F(clientId), order.TargetString);
					break;
				}
			case "TeamChat":
				{
					var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
					if (client != null)
					{
						if (world == null)
						{
							if (client.Team == orderManager.LocalClient.Team)
								Game.AddChatLine(client.Color1, client.Name + " (Team)",
									order.TargetString);
						}
						else
						{
							var player = world.FindPlayerByClient(client);
							var display = player != null
								&& (world.LocalPlayer != null && player.Stances[world.LocalPlayer] == Stance.Ally
									|| player.WinState == WinState.Lost);

							if (display)
							{
								var suffix = (player != null && player.WinState == WinState.Lost) ? " (Dead)" : " (Team)";
								Game.AddChatLine(client.Color1, client.Name + suffix, order.TargetString);
							}
						}
					}
					break;
				}
			case "StartGame":
				{
					Game.AddChatLine(Color.White, "Server", "The game has started.");
					Game.StartGame(orderManager.LobbyInfo.GlobalSettings.Map);
					break;
				}
			case "SyncInfo":
				{
					orderManager.LobbyInfo = Session.Deserialize( order.TargetString );

					if( orderManager.FramesAhead != orderManager.LobbyInfo.GlobalSettings.OrderLatency
						&& !orderManager.GameStarted )
					{
						orderManager.FramesAhead = orderManager.LobbyInfo.GlobalSettings.OrderLatency;
						Game.Debug( "Order lag is now {0} frames.".F( orderManager.LobbyInfo.GlobalSettings.OrderLatency ) );
					}

					Game.SyncLobbyInfo();
					break;
				}
			case "SetStance":
				{
					var targetPlayer = order.Player.World.players[order.TargetLocation.X];
					var newStance = (Stance)order.TargetLocation.Y;

					SetPlayerStance(world, order.Player, targetPlayer, newStance);

					// automatically declare war reciprocally
					if (newStance == Stance.Enemy)
						SetPlayerStance(world, targetPlayer, order.Player, newStance);

					Game.Debug("{0} has set diplomatic stance vs {1} to {2}".F(
						order.Player.PlayerName, targetPlayer.PlayerName, newStance));
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

		static void SetPlayerStance(World w, Player a, Player b, Stance s)
		{
			var oldStance = a.Stances[b];
			a.Stances[b] = s;
			if (b == w.LocalPlayer)
				w.WorldActor.Trait<Shroud>().UpdatePlayerStance(w, b, oldStance, s);
		}
	}
}
