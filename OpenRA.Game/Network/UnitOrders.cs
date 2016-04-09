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
using System.Drawing;
using System.Linq;
using OpenRA.Traits;

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

							if (orderManager.LocalClient != null && client != orderManager.LocalClient && client.Team > 0 && client.Team == orderManager.LocalClient.Team)
								suffix += " (Ally)";

							Game.AddChatLine(client.Color.RGB, client.Name + suffix, order.TargetString);
						}
						else
							Game.AddChatLine(Color.White, "(player {0})".F(clientId), order.TargetString);
						break;
					}

				case "Message": // Server message
					Game.AddChatLine(Color.White, "Server", order.TargetString);
					break;

				case "Disconnected": /* reports that the target player disconnected */
					{
						var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
						if (client != null)
							client.State = Session.ClientState.Disconnected;
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
									Game.AddChatLine(client.Color.RGB, "[Team] " + client.Name, order.TargetString);
							}
							else
							{
								var player = world.FindPlayerByClient(client);
								if (player != null && player.WinState == WinState.Lost)
									Game.AddChatLine(client.Color.RGB, client.Name + " (Dead)", order.TargetString);
								else if (player != null && world.LocalPlayer != null && player.Stances[world.LocalPlayer] == Stance.Ally)
									Game.AddChatLine(client.Color.RGB, "[Team] " + client.Name, order.TargetString);
								else if (orderManager.LocalClient != null && orderManager.LocalClient.IsObserver && client.IsObserver)
									Game.AddChatLine(client.Color.RGB, "[Spectators] " + client.Name, order.TargetString);
							}
						}

						break;
					}

				case "StartGame":
					{
						if (Game.ModData.MapCache[orderManager.LobbyInfo.GlobalSettings.Map].Status != MapStatus.Available)
						{
							Game.Disconnect();
							Game.LoadShellMap();

							// TODO: After adding a startup error dialog, notify the replay load failure.
							break;
						}

						Game.AddChatLine(Color.White, "Server", "The game has started.");
						Game.StartGame(orderManager.LobbyInfo.GlobalSettings.Map, WorldType.Regular);
						break;
					}

				case "PauseGame":
					{
						var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
						if (client != null)
						{
							var pause = order.TargetString == "Pause";
							if (orderManager.World.Paused != pause && world != null && !world.LobbyInfo.IsSinglePlayer)
							{
								var pausetext = "The game is {0} by {1}".F(pause ? "paused" : "un-paused", client.Name);
								Game.AddChatLine(Color.White, "", pausetext);
							}

							orderManager.World.Paused = pause;
							orderManager.World.PredictedPaused = pause;
						}

						break;
					}

				case "HandshakeRequest":
					{
						// Switch to the server's mod if we need and are able to
						var mod = Game.ModData.Manifest.Mod;
						var request = HandshakeRequest.Deserialize(order.TargetString);

						ModMetadata serverMod;
						if (request.Mod != mod.Id &&
							ModMetadata.AllMods.TryGetValue(request.Mod, out serverMod) &&
							serverMod.Version == request.Version)
						{
							var replay = orderManager.Connection as ReplayConnection;
							var launchCommand = replay != null ?
								"Launch.Replay=" + replay.Filename :
								"Launch.Connect=" + orderManager.Host + ":" + orderManager.Port;

							Game.ModData.LoadScreen.Display();
							Game.InitializeMod(request.Mod, new Arguments(launchCommand));

							break;
						}

						Game.Settings.Player.Name = Settings.SanitizedPlayerName(Game.Settings.Player.Name);
						Game.Settings.Save();

						// Otherwise send the handshake with our current settings and let the server reject us
						var info = new Session.Client()
						{
							Name = Game.Settings.Player.Name,
							PreferredColor = Game.Settings.Player.Color,
							Color = Game.Settings.Player.Color,
							Faction = "Random",
							SpawnPoint = 0,
							Team = 0,
							State = Session.ClientState.Invalid
						};

						var response = new HandshakeResponse()
						{
							Client = info,
							Mod = mod.Id,
							Version = mod.Version,
							Password = orderManager.Password
						};

						orderManager.IssueOrder(Order.HandshakeResponse(response.Serialize()));
						break;
					}

				case "ServerError":
					{
						orderManager.ServerError = order.TargetString;
						orderManager.AuthenticationFailed = false;
						break;
					}

				case "AuthenticationError":
					{
						orderManager.ServerError = order.TargetString;
						orderManager.AuthenticationFailed = true;
						break;
					}

				case "SyncInfo":
					{
						orderManager.LobbyInfo = Session.Deserialize(order.TargetString);
						SetOrderLag(orderManager);
						Game.SyncLobbyInfo();
						break;
					}

				case "SyncLobbyClients":
					{
						var clients = new List<Session.Client>();
						var nodes = MiniYaml.FromString(order.TargetString);
						foreach (var node in nodes)
						{
							var strings = node.Key.Split('@');
							if (strings[0] == "Client")
								clients.Add(Session.Client.Deserialize(node.Value));
						}

						orderManager.LobbyInfo.Clients = clients;
						Game.SyncLobbyInfo();
						break;
					}

				case "SyncLobbySlots":
					{
						var slots = new Dictionary<string, Session.Slot>();
						var nodes = MiniYaml.FromString(order.TargetString);
						foreach (var node in nodes)
						{
							var strings = node.Key.Split('@');
							if (strings[0] == "Slot")
							{
								var slot = Session.Slot.Deserialize(node.Value);
								slots.Add(slot.PlayerReference, slot);
							}
						}

						orderManager.LobbyInfo.Slots = slots;
						Game.SyncLobbyInfo();
						break;
					}

				case "SyncLobbyGlobalSettings":
					{
						var nodes = MiniYaml.FromString(order.TargetString);
						foreach (var node in nodes)
						{
							var strings = node.Key.Split('@');
							if (strings[0] == "GlobalSettings")
								orderManager.LobbyInfo.GlobalSettings = Session.Global.Deserialize(node.Value);
						}

						SetOrderLag(orderManager);
						Game.SyncLobbyInfo();
						break;
					}

				case "SyncClientPings":
					{
						var pings = new List<Session.ClientPing>();
						var nodes = MiniYaml.FromString(order.TargetString);
						foreach (var node in nodes)
						{
							var strings = node.Key.Split('@');
							if (strings[0] == "ClientPing")
								pings.Add(Session.ClientPing.Deserialize(node.Value));
						}

						orderManager.LobbyInfo.ClientPings = pings;
						break;
					}

				case "Ping":
					{
						orderManager.IssueOrder(Order.Pong(order.TargetString));
						break;
					}

				default:
					{
						if (!order.IsImmediate)
						{
							var self = order.Subject;
							if (!self.IsDead)
								foreach (var t in self.TraitsImplementing<IResolveOrder>())
									t.ResolveOrder(self, order);
						}

						break;
					}
			}
		}

		static void SetOrderLag(OrderManager o)
		{
			if (o.FramesAhead != o.LobbyInfo.GlobalSettings.OrderLatency && !o.GameStarted)
			{
				o.FramesAhead = o.LobbyInfo.GlobalSettings.OrderLatency;
				Log.Write("server", "Order lag is now {0} frames.", o.LobbyInfo.GlobalSettings.OrderLatency);
			}
		}
	}
}
