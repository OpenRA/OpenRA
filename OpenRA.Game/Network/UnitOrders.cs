#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Traits;
using System;
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	static class UnitOrders
	{
		static Player FindPlayerByClient(this World world, Session.Client c)
		{
			/* TODO: this is still a hack.
			 * the cases we're trying to avoid are the extra players on the host's client -- Neutral, other MapPlayers,..*/
			return world.Players.FirstOrDefault(
				p => (p.ClientIndex == c.Index && p.PlayerReference.Playable));
		}

		public static void ProcessOrder(OrderManager orderManager, World world, int clientId, Order order)
		{
			if (world != null)
			{
				if (!world.WorldActor.TraitsImplementing<IValidateOrder>().All(vo =>
					vo.OrderValidation(orderManager, world, clientId, order)))
					return;
			}

			switch (order.OrderString)
			{
				case "Chat":
					{
						var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
						if (client != null)
						{
							var player = world != null ? world.FindPlayerByClient(client) : null;
							var suffix = (player != null && player.WinState == WinState.Lost) ? " (Dead)" : "";
							suffix = client.IsObserver ? " (Spectator)" : suffix;
							Game.AddChatLine(client.ColorRamp.GetColor(0), client.Name + suffix, order.TargetString);
						}
						else
							Game.AddChatLine(Color.White, "(player {0})".F(clientId), order.TargetString);
						break;
					}

				case "Disconnected": /* reports that the target player disconnected */
					{
						var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
						if (client != null)
						{
							client.State = Session.ClientState.Disconnected;
						}
						break;
					}

				case "TeamChat":
					{
						var client = orderManager.LobbyInfo.ClientWithIndex(clientId);

						if (client != null)
						{
							if (world == null)
							{
								if (orderManager.LocalClient != null && client.Team == orderManager.LocalClient.Team)
									Game.AddChatLine(client.ColorRamp.GetColor(0), client.Name + " (Team)",
										order.TargetString);
							}
							else
							{
								var player = world.FindPlayerByClient(client);
								if (player == null) return;

								if (world.LocalPlayer != null && player.Stances[world.LocalPlayer] == Stance.Ally || player.WinState == WinState.Lost)
								{
									var suffix = player.WinState == WinState.Lost ? " (Dead)" : " (Team)";
									Game.AddChatLine(client.ColorRamp.GetColor(0), client.Name + suffix, order.TargetString);
								}
							}
						}
						break;
					}

				case "StartGame":
					{
						Game.AddChatLine(Color.White, "Server", "The game has started.");
						Game.StartGame(orderManager.LobbyInfo.GlobalSettings.Map, false);
						break;
					}

				case "PauseGame":
					{
						var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
						if (client != null)
						{
							var pause = order.TargetString == "Pause";
							if (orderManager.world.Paused != pause && !world.LobbyInfo.IsSinglePlayer)
							{
								var pausetext = "The game is {0} by {1}".F(pause ? "paused" : "un-paused", client.Name);
								Game.AddChatLine(Color.White, "", pausetext);
							}

							orderManager.world.Paused = pause;
						}
						break;
					}

				case "HandshakeRequest":
					{
						var request = HandshakeRequest.Deserialize(order.TargetString);
						var localMods = orderManager.LobbyInfo.GlobalSettings.Mods.Select(m => "{0}@{1}".F(m,Mod.AllMods[m].Version)).ToArray();

						// Check if mods match
						if (localMods.FirstOrDefault().ToString().Split('@')[0] != request.Mods.FirstOrDefault().ToString().Split('@')[0])
							throw new InvalidOperationException("Server's mod ({0}) and yours ({1}) don't match".F(localMods.FirstOrDefault().ToString().Split('@')[0], request.Mods.FirstOrDefault().ToString().Split('@')[0]));
						// Check that the map exists on the client
						if (!Game.modData.AvailableMaps.ContainsKey(request.Map))
						{
							if (Game.Settings.Game.AllowDownloading)
								Game.DownloadMap(request.Map);
							else
								throw new InvalidOperationException("Missing map {0}".F(request.Map));
						}

						var info = new Session.Client()
						{
							Name = Game.Settings.Player.Name,
							PreferredColorRamp = Game.Settings.Player.ColorRamp,
							ColorRamp = Game.Settings.Player.ColorRamp,
							Country = "random",
							SpawnPoint = 0,
							Team = 0,
							State = Session.ClientState.NotReady
						};

						var response = new HandshakeResponse()
						{
							Client = info,
							Mods = localMods,
							Password = "Foo"
						};

						orderManager.IssueOrder(Order.HandshakeResponse(response.Serialize()));
						break;
					}

				case "ServerError":
					orderManager.ServerError = order.TargetString;
					break;

				case "SyncInfo":
					{
						orderManager.LobbyInfo = Session.Deserialize(order.TargetString);

						if (orderManager.FramesAhead != orderManager.LobbyInfo.GlobalSettings.OrderLatency
							&& !orderManager.GameStarted)
						{
							orderManager.FramesAhead = orderManager.LobbyInfo.GlobalSettings.OrderLatency;
							Game.Debug("Order lag is now {0} frames.".F(orderManager.LobbyInfo.GlobalSettings.OrderLatency));
						}
						Game.SyncLobbyInfo();
						break;
					}

				case "SetStance":
					{
						if (!Game.orderManager.LobbyInfo.GlobalSettings.FragileAlliances)
							return;

						var targetPlayer = order.Player.World.Players.FirstOrDefault(p => p.InternalName == order.TargetString);
						var newStance = (Stance)order.TargetLocation.X;

						SetPlayerStance(world, order.Player, targetPlayer, newStance);

						Game.Debug("{0} has set diplomatic stance vs {1} to {2}".F(
							order.Player.PlayerName, targetPlayer.PlayerName, newStance));

						// automatically declare war reciprocally
						if (newStance == Stance.Enemy && targetPlayer.Stances[order.Player] == Stance.Ally)
						{
							SetPlayerStance(world, targetPlayer, order.Player, newStance);
							Game.Debug("{0} has reciprocated",targetPlayer.PlayerName);
						}

						break;
					}
				default:
					{
						if( !order.IsImmediate )
						{
							var self = order.Subject;
							var health = self.TraitOrDefault<Health>();
							if( health == null || !health.IsDead )
								foreach( var t in self.TraitsImplementing<IResolveOrder>() )
									t.ResolveOrder( self, order );
						}
						break;
					}
			}
		}

		static void SetPlayerStance(World w, Player p, Player target, Stance s)
		{
			var oldStance = p.Stances[target];
			p.Stances[target] = s;
			target.Shroud.UpdatePlayerStance(w, p, oldStance, s);
			p.Shroud.UpdatePlayerStance(w, target, oldStance, s);

			foreach (var nsc in w.ActorsWithTrait<INotifyStanceChanged>())
				nsc.Trait.StanceChanged(nsc.Actor, p, target, oldStance, s);
		}
	}
}
