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
		static Player FindPlayerByClientId( this World world, int id)
		{
			/* todo: find the interactive player. */
			return world.players.Values.FirstOrDefault(p => p.ClientIndex == id);
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
					if( client != null )
						Game.AddChatLine( client.Color1, client.Name, order.TargetString );
					else
						Game.AddChatLine( Color.White, "(player {0})".F( clientId ), order.TargetString );
					break;
				}
			case "TeamChat":
				{
					var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
					if (client != null)
					{
						if (world != null)		// TODO: fix this whole thing properly. this is BS.
						{
							var player = world.FindPlayerByClientId(clientId);
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

					//if( !world.GameHasStarted )
					//    world.SharedRandom = new XRandom( LobbyInfo.GlobalSettings.RandomSeed );

					//if (orderManager.Connection.ConnectionState == ConnectionState.Connected)
					//    world.SetLocalPlayer(orderManager.Connection.LocalClientId);

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
