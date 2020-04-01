#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Network
{
	public static class UnitOrders
	{
		public const int ChatMessageMaxLength = 2500;

		static Player FindPlayerByClient(this World world, Session.Client c)
		{
			return world.Players.FirstOrDefault(p => (p.ClientIndex == c.Index && p.PlayerReference.Playable));
		}

		internal static void ProcessOrder(OrderManager orderManager, World world, int clientId, Order order)
		{
			if (world != null)
			{
				if (!world.WorldActor.TraitsImplementing<IValidateOrder>().All(vo =>
					vo.OrderValidation(orderManager, world, clientId, order)))
					return;
			}

			switch (order.OrderString)
			{
				// Server message
				case "Message":
					Game.AddSystemLine(order.TargetString);
					break;

				// Reports that the target player disconnected
				case "Disconnected":
					{
						var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
						if (client != null)
							client.State = Session.ClientState.Disconnected;
						break;
					}

				case "Chat":
					{
						var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
						if (client == null)
							break;

						// Cut chat messages to the hard limit to avoid exploits
						var message = order.TargetString;
						if (message.Length > ChatMessageMaxLength)
							message = order.TargetString.Substring(0, ChatMessageMaxLength);

						// ExtraData 0 means this is a normal chat order, everything else is team chat
						if (order.ExtraData == 0)
						{
							var p = world != null ? world.FindPlayerByClient(client) : null;
							var suffix = (p != null && p.WinState == WinState.Lost) ? " (Dead)" : "";
							suffix = client.IsObserver ? " (Spectator)" : suffix;

							if (orderManager.LocalClient != null && client != orderManager.LocalClient && client.Team > 0 && client.Team == orderManager.LocalClient.Team)
								suffix += " (Ally)";

							Game.AddChatLine(client.Name + suffix, client.Color, message);
							break;
						}

						// We are still in the lobby
						if (world == null)
						{
							var prefix = order.ExtraData == uint.MaxValue ? "[Spectators] " : "[Team] ";
							if (orderManager.LocalClient != null && client.Team == orderManager.LocalClient.Team)
								Game.AddChatLine(prefix + client.Name, client.Color, message);

							break;
						}

						var player = world.FindPlayerByClient(client);
						var localClientIsObserver = world.IsReplay || (orderManager.LocalClient != null && orderManager.LocalClient.IsObserver)
							|| (world.LocalPlayer != null && world.LocalPlayer.WinState != WinState.Undefined);

						// ExtraData gives us the team number, uint.MaxValue means Spectators
						if (order.ExtraData == uint.MaxValue && localClientIsObserver)
						{
							// Validate before adding the line
							if (client.IsObserver || (player != null && player.WinState != WinState.Undefined))
								Game.AddChatLine("[Spectators] " + client.Name, client.Color, message);

							break;
						}

						var valid = client.Team == order.ExtraData && player != null && player.WinState == WinState.Undefined;
						var isSameTeam = orderManager.LocalClient != null && order.ExtraData == orderManager.LocalClient.Team
							&& world.LocalPlayer != null && world.LocalPlayer.WinState == WinState.Undefined;

						if (valid && (isSameTeam || world.IsReplay))
							Game.AddChatLine("[Team" + (world.IsReplay ? " " + order.ExtraData : "") + "] " + client.Name, client.Color, message);

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

						if (!string.IsNullOrEmpty(order.TargetString))
						{
							var data = MiniYaml.FromString(order.TargetString);
							var saveLastOrdersFrame = data.FirstOrDefault(n => n.Key == "SaveLastOrdersFrame");
							if (saveLastOrdersFrame != null)
								orderManager.GameSaveLastFrame =
									FieldLoader.GetValue<int>("saveLastOrdersFrame", saveLastOrdersFrame.Value.Value);

							var saveSyncFrame = data.FirstOrDefault(n => n.Key == "SaveSyncFrame");
							if (saveSyncFrame != null)
								orderManager.GameSaveLastSyncFrame =
									FieldLoader.GetValue<int>("SaveSyncFrame", saveSyncFrame.Value.Value);
						}
						else
							Game.AddSystemLine("The game has started.");

						Game.StartGame(orderManager.LobbyInfo.GlobalSettings.Map, WorldType.Regular);
						break;
					}

				case "SaveTraitData":
					{
						var data = MiniYaml.FromString(order.TargetString)[0];
						var traitIndex = int.Parse(data.Key);

						if (world != null)
							world.AddGameSaveTraitData(traitIndex, data.Value);

						break;
					}

				case "GameSaved":
					if (!orderManager.World.IsReplay)
						Game.AddSystemLine("Game saved");

					foreach (var nsr in orderManager.World.WorldActor.TraitsImplementing<INotifyGameSaved>())
						nsr.GameSaved(orderManager.World);
					break;

				case "PauseGame":
					{
						var client = orderManager.LobbyInfo.ClientWithIndex(clientId);
						if (client != null)
						{
							var pause = order.TargetString == "Pause";

							// Prevent injected unpause orders from restarting a finished game
							if (orderManager.World.PauseStateLocked && !pause)
								break;

							if (orderManager.World.Paused != pause && world != null && world.LobbyInfo.NonBotClients.Count() > 1)
							{
								var pausetext = "The game is {0} by {1}".F(pause ? "paused" : "un-paused", client.Name);
								Game.AddSystemLine(pausetext);
							}

							orderManager.World.Paused = pause;
							orderManager.World.PredictedPaused = pause;
						}

						break;
					}

				case "HandshakeRequest":
					{
						// Switch to the server's mod if we need and are able to
						var mod = Game.ModData.Manifest;
						var request = HandshakeRequest.Deserialize(order.TargetString);

						var externalKey = ExternalMod.MakeKey(request.Mod, request.Version);
						ExternalMod external;
						if ((request.Mod != mod.Id || request.Version != mod.Metadata.Version)
							&& Game.ExternalMods.TryGetValue(externalKey, out external))
						{
							// The ConnectionFailedLogic will prompt the user to switch mods
							orderManager.ServerExternalMod = external;
							orderManager.Connection.Dispose();
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

						var localProfile = Game.LocalPlayerProfile;
						var response = new HandshakeResponse()
						{
							Client = info,
							Mod = mod.Id,
							Version = mod.Metadata.Version,
							Password = orderManager.Password,
							Fingerprint = localProfile.Fingerprint,
							OrdersProtocol = ProtocolVersion.Orders
						};

						if (request.AuthToken != null && response.Fingerprint != null)
							response.AuthSignature = localProfile.Sign(request.AuthToken);

						orderManager.IssueOrder(new Order("HandshakeResponse", null, false)
						{
							Type = OrderType.Handshake,
							IsImmediate = true,
							TargetString = response.Serialize()
						});

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
						// The ConnectionFailedLogic will prompt the user for the password
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
						orderManager.IssueOrder(Order.FromTargetString("Pong", order.TargetString, true));
						break;
					}

				default:
					{
						ResolveOrder(order);
						break;
					}
			}
		}

		static void ResolveOrder(Order order)
		{
			if (order.Subject != null && !order.Subject.IsDead)
				foreach (var t in order.Subject.TraitsImplementing<IResolveOrder>())
					t.ResolveOrder(order.Subject, order);
		}

		static void SetOrderLag(OrderManager o)
		{
			if (o.OrderLatency != o.LobbyInfo.GlobalSettings.OrderLatency && !o.GameStarted)
			{
				o.OrderLatency = o.LobbyInfo.GlobalSettings.OrderLatency;
				Log.Write("server", "Order lag is now {0} frames.", o.LobbyInfo.GlobalSettings.OrderLatency);
			}
		}
	}
}
