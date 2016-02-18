#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class LobbyCommands : ServerTrait, IInterpretCommand, INotifyServerStart, IClientJoined
	{
		static bool ValidateSlotCommand(S server, Connection conn, Session.Client client, string arg, bool requiresHost)
		{
			if (!server.LobbyInfo.Slots.ContainsKey(arg))
			{
				Log.Write("server", "Invalid slot: {0}", arg);
				return false;
			}

			if (requiresHost && !client.IsAdmin)
			{
				server.SendOrderTo(conn, "Message", "Only the host can do that.");
				return false;
			}

			return true;
		}

		public static bool ValidateCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (server.State == ServerState.GameStarted)
			{
				server.SendOrderTo(conn, "Message", "Cannot change state when game started. ({0})".F(cmd));
				return false;
			}
			else if (client.State == Session.ClientState.Ready && !(cmd.StartsWith("state") || cmd == "startgame"))
			{
				server.SendOrderTo(conn, "Message", "Cannot change state when marked as ready.");
				return false;
			}

			return true;
		}

		static void CheckAutoStart(S server)
		{
			var playerClients = server.LobbyInfo.Clients.Where(c => c.Bot == null && c.Slot != null);

			// Are all players ready?
			if (!playerClients.Any() || playerClients.Any(c => c.State != Session.ClientState.Ready))
				return;

			// Are the map conditions satisfied?
			if (server.LobbyInfo.Slots.Any(sl => sl.Value.Required && server.LobbyInfo.ClientInSlot(sl.Key) == null))
				return;

			// Does server have only one player?
			if (server.Settings.DisableSinglePlayer && playerClients.Count() == 1)
				return;

			server.StartGame();
		}

		public bool InterpretCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (server == null || conn == null || client == null || !ValidateCommand(server, conn, client, cmd))
				return false;

			var dict = new Dictionary<string, Func<string, bool>>
			{
				{ "state",
					s =>
					{
						var state = Session.ClientState.Invalid;
						if (!Enum<Session.ClientState>.TryParse(s, false, out state))
						{
							server.SendOrderTo(conn, "Message", "Malformed state command");
							return true;
						}

						client.State = state;

						Log.Write("server", "Player @{0} is {1}",
							conn.Socket.RemoteEndPoint, client.State);

						server.SyncLobbyClients();

						CheckAutoStart(server);

						return true;
					}
				},
				{ "startgame",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can start the game.");
							return true;
						}

						if (server.LobbyInfo.Slots.Any(sl => sl.Value.Required &&
							server.LobbyInfo.ClientInSlot(sl.Key) == null))
						{
							server.SendOrderTo(conn, "Message", "Unable to start the game until required slots are full.");
							return true;
						}

						if (server.Settings.DisableSinglePlayer &&
							server.LobbyInfo.Clients.Where(c => c.Bot == null && c.Slot != null).Count() == 1)
						{
							server.SendOrderTo(conn, "Message", "Unable to start the game until another player joins.");
							return true;
						}

						server.StartGame();
						return true;
					}
				},
				{ "slot",
					s =>
					{
						if (!server.LobbyInfo.Slots.ContainsKey(s))
						{
							Log.Write("server", "Invalid slot: {0}", s);
							return false;
						}

						var slot = server.LobbyInfo.Slots[s];

						if (slot.Closed || server.LobbyInfo.ClientInSlot(s) != null)
							return false;

						client.Slot = s;
						S.SyncClientToPlayerReference(client, server.MapPlayers.Players[s]);

						if (!slot.LockColor)
							client.PreferredColor = client.Color = SanitizePlayerColor(server, client.Color, client.Index, conn);

						server.SyncLobbyClients();
						CheckAutoStart(server);

						return true;
					}
				},
				{ "allow_spectators",
					s =>
					{
						if (bool.TryParse(s, out server.LobbyInfo.GlobalSettings.AllowSpectators))
						{
							server.SyncLobbyGlobalSettings();
							return true;
						}
						else
						{
							server.SendOrderTo(conn, "Message", "Malformed allow_spectate command");
							return true;
						}
					}
				},
				{ "spectate",
					s =>
					{
						if (server.LobbyInfo.GlobalSettings.AllowSpectators || client.IsAdmin)
						{
							client.Slot = null;
							client.SpawnPoint = 0;
							client.Color = HSLColor.FromRGB(255, 255, 255);
							server.SyncLobbyClients();
							return true;
						}
						else
							return false;
					}
				},
				{ "slot_close",
					s =>
					{
						if (!ValidateSlotCommand(server, conn, client, s, true))
							return false;

						// kick any player that's in the slot
						var occupant = server.LobbyInfo.ClientInSlot(s);
						if (occupant != null)
						{
							if (occupant.Bot != null)
							{
								server.LobbyInfo.Clients.Remove(occupant);
								server.SyncLobbyClients();
								var ping = server.LobbyInfo.PingFromClient(occupant);
								if (ping != null)
								{
									server.LobbyInfo.ClientPings.Remove(ping);
									server.SyncClientPing();
								}
							}
							else
							{
								var occupantConn = server.Conns.FirstOrDefault(c => c.PlayerIndex == occupant.Index);
								if (occupantConn != null)
								{
									server.SendOrderTo(occupantConn, "ServerError", "Your slot was closed by the host.");
									server.DropClient(occupantConn);
								}
							}
						}

						server.LobbyInfo.Slots[s].Closed = true;
						server.SyncLobbySlots();
						return true;
					}
				},
				{ "slot_open",
					s =>
					{
						if (!ValidateSlotCommand(server, conn, client, s, true))
							return false;

						var slot = server.LobbyInfo.Slots[s];
						slot.Closed = false;
						server.SyncLobbySlots();

						// Slot may have a bot in it
						var occupant = server.LobbyInfo.ClientInSlot(s);
						if (occupant != null && occupant.Bot != null)
						{
							server.LobbyInfo.Clients.Remove(occupant);
							var ping = server.LobbyInfo.PingFromClient(occupant);
							if (ping != null)
							{
								server.LobbyInfo.ClientPings.Remove(ping);
								server.SyncClientPing();
							}
						}

						server.SyncLobbyClients();
						return true;
					}
				},
				{ "slot_bot",
					s =>
					{
						var parts = s.Split(' ');

						if (parts.Length < 3)
						{
							server.SendOrderTo(conn, "Message", "Malformed slot_bot command");
							return true;
						}

						if (!ValidateSlotCommand(server, conn, client, parts[0], true))
							return false;

						var slot = server.LobbyInfo.Slots[parts[0]];
						var bot = server.LobbyInfo.ClientInSlot(parts[0]);
						int controllerClientIndex;
						if (!Exts.TryParseIntegerInvariant(parts[1], out controllerClientIndex))
						{
							Log.Write("server", "Invalid bot controller client index: {0}", parts[1]);
							return false;
						}

						var botType = parts.Skip(2).JoinWith(" ");

						// Invalid slot
						if (bot != null && bot.Bot == null)
						{
							server.SendOrderTo(conn, "Message", "Can't add bots to a slot with another client.");
							return true;
						}

						slot.Closed = false;
						if (bot == null)
						{
							// Create a new bot
							bot = new Session.Client()
							{
								Index = server.ChooseFreePlayerIndex(),
								Name = botType,
								Bot = botType,
								Slot = parts[0],
								Faction = "Random",
								SpawnPoint = 0,
								Team = 0,
								State = Session.ClientState.NotReady,
								BotControllerClientIndex = controllerClientIndex
							};

							// Pick a random color for the bot
							var validator = server.ModData.Manifest.Get<ColorValidator>();
							var tileset = server.Map.Rules.TileSets[server.Map.Tileset];
							var terrainColors = tileset.TerrainInfo.Where(ti => ti.RestrictPlayerColor).Select(ti => ti.Color);
							var playerColors = server.LobbyInfo.Clients.Select(c => c.Color.RGB)
								.Concat(server.MapPlayers.Players.Values.Select(p => p.Color.RGB));
							bot.Color = bot.PreferredColor = validator.RandomValidColor(server.Random, terrainColors, playerColors);

							server.LobbyInfo.Clients.Add(bot);
						}
						else
						{
							// Change the type of the existing bot
							bot.Name = botType;
							bot.Bot = botType;
						}

						S.SyncClientToPlayerReference(bot, server.MapPlayers.Players[parts[0]]);
						server.SyncLobbyClients();
						server.SyncLobbySlots();
						return true;
					}
				},
				{ "map",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can change the map.");
							return true;
						}

						if (server.ModData.MapCache[s].Status != MapStatus.Available)
						{
							server.SendOrderTo(conn, "Message", "Map was not found on server.");
							return true;
						}

						server.LobbyInfo.GlobalSettings.Map = s;

						var oldSlots = server.LobbyInfo.Slots.Keys.ToArray();
						LoadMap(server);
						SetDefaultDifficulty(server);

						// Reset client states
						foreach (var c in server.LobbyInfo.Clients)
							c.State = Session.ClientState.Invalid;

						// Reassign players into new slots based on their old slots:
						//  - Observers remain as observers
						//  - Players who now lack a slot are made observers
						//  - Bots who now lack a slot are dropped
						var slots = server.LobbyInfo.Slots.Keys.ToArray();
						var i = 0;
						foreach (var os in oldSlots)
						{
							var c = server.LobbyInfo.ClientInSlot(os);
							if (c == null)
								continue;

							c.SpawnPoint = 0;
							c.Slot = i < slots.Length ? slots[i++] : null;
							if (c.Slot != null)
							{
								// Remove Bot from slot if slot forbids bots
								if (c.Bot != null && !server.MapPlayers.Players[c.Slot].AllowBots)
									server.LobbyInfo.Clients.Remove(c);
								S.SyncClientToPlayerReference(c, server.MapPlayers.Players[c.Slot]);
							}
							else if (c.Bot != null)
								server.LobbyInfo.Clients.Remove(c);
						}

						// Validate if color is allowed and get an alternative it isn't
						foreach (var c in server.LobbyInfo.Clients)
							if (c.Slot == null || (c.Slot != null && !server.LobbyInfo.Slots[c.Slot].LockColor))
								c.Color = c.PreferredColor = SanitizePlayerColor(server, c.Color, c.Index, conn);

						server.SyncLobbyInfo();

						server.SendMessage("{0} changed the map to {1}.".F(client.Name, server.Map.Title));

						if (server.Map.RuleDefinitions.Any())
							server.SendMessage("This map contains custom rules. Game experience may change.");

						if (server.Settings.DisableSinglePlayer)
							server.SendMessage("Singleplayer games have been disabled on this server.");
						else if (server.MapPlayers.Players.Where(p => p.Value.Playable).All(p => !p.Value.AllowBots))
							server.SendMessage("Bots have been disabled on this map.");

						return true;
					}
				},
				{ "allowcheats",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (server.Map.Options.Cheats.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled cheat configuration.");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.AllowCheats);
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} {1} the Debug Menu."
							.F(client.Name, server.LobbyInfo.GlobalSettings.AllowCheats ? "enabled" : "disabled"));

						return true;
					}
				},
				{ "shroud",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (server.Map.Options.Shroud.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled shroud configuration.");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.Shroud);
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} {1} Explored map."
							.F(client.Name, server.LobbyInfo.GlobalSettings.Shroud ? "disabled" : "enabled"));

						return true;
					}
				},
				{ "fog",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (server.Map.Options.Fog.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled fog configuration.");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.Fog);
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} {1} Fog of War."
							.F(client.Name, server.LobbyInfo.GlobalSettings.Fog ? "enabled" : "disabled"));

						return true;
					}
				},
				{ "assignteams",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						int teamCount;
						if (!Exts.TryParseIntegerInvariant(s, out teamCount))
						{
							server.SendOrderTo(conn, "Message", "Number of teams could not be parsed: {0}".F(s));
							return true;
						}

						var maxTeams = (server.LobbyInfo.Clients.Count(c => c.Slot != null) + 1) / 2;
						teamCount = teamCount.Clamp(0, maxTeams);
						var clients = server.LobbyInfo.Slots
							.Select(slot => server.LobbyInfo.ClientInSlot(slot.Key))
							.Where(c => c != null && !server.LobbyInfo.Slots[c.Slot].LockTeam);

						var assigned = 0;
						var clientCount = clients.Count();
						foreach (var player in clients)
						{
							// Free for all
							if (teamCount == 0)
								player.Team = 0;

							// Humans vs Bots
							else if (teamCount == 1)
								player.Team = player.Bot == null ? 1 : 2;
							else
								player.Team = assigned++ * teamCount / clientCount + 1;
						}

						server.SyncLobbyClients();
						return true;
					}
				},
				{ "crates",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (server.Map.Options.Crates.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled crate configuration.");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.Crates);
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} {1} Crates."
							.F(client.Name, server.LobbyInfo.GlobalSettings.Crates ? "enabled" : "disabled"));

						return true;
					}
				},
				{ "creeps",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (server.Map.Options.Creeps.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled Creeps spawning configuration.");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.Creeps);
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} {1} Creeps spawning."
							.F(client.Name, server.LobbyInfo.GlobalSettings.Creeps ? "enabled" : "disabled"));

						return true;
					}
				},
				{ "allybuildradius",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (server.Map.Options.AllyBuildRadius.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled ally build radius configuration.");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.AllyBuildRadius);
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} {1} Build off Allies' ConYards."
							.F(client.Name, server.LobbyInfo.GlobalSettings.AllyBuildRadius ? "enabled" : "disabled"));

						return true;
					}
				},
				{ "difficulty",
					s =>
					{
						if (!server.Map.Options.Difficulties.Any())
							return true;

						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (s != null && !server.Map.Options.Difficulties.Contains(s))
						{
							server.SendOrderTo(conn, "Message", "Unsupported difficulty selected: {0}".F(s));
							server.SendOrderTo(conn, "Message", "Supported difficulties: {0}".F(server.Map.Options.Difficulties.JoinWith(",")));
							return true;
						}

						server.LobbyInfo.GlobalSettings.Difficulty = s;
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} changed difficulty to {1}.".F(client.Name, s));

						return true;
					}
				},
				{ "startingunits",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (!server.Map.Options.ConfigurableStartingUnits)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled start unit configuration.");
							return true;
						}

						var startUnitsInfo = server.Map.Rules.Actors["world"].TraitInfos<MPStartUnitsInfo>();
						var selectedClass = startUnitsInfo.Where(u => u.Class == s).Select(u => u.ClassName).FirstOrDefault();
						var className = selectedClass != null ? selectedClass : s;

						server.LobbyInfo.GlobalSettings.StartingUnitsClass = s;
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} changed Starting Units to {1}.".F(client.Name, className));

						return true;
					}
				},
				{ "startingcash",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (server.Map.Options.StartingCash.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled cash configuration.");
							return true;
						}

						server.LobbyInfo.GlobalSettings.StartingCash = Exts.ParseIntegerInvariant(s);
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} changed Starting Cash to ${1}.".F(client.Name, s));

						return true;
					}
				},
				{ "techlevel",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (server.Map.Options.TechLevel != null)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled Tech configuration.");
							return true;
						}

						server.LobbyInfo.GlobalSettings.TechLevel = s;
						server.SyncLobbyInfo();
						server.SendMessage("{0} changed Tech Level to {1}.".F(client.Name, s));

						return true;
					}
				},
				{ "gamespeed",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>();

						GameSpeed speed;
						if (!gameSpeeds.Speeds.TryGetValue(s, out speed))
						{
							server.SendOrderTo(conn, "Message", "Invalid game speed selected.");
							return true;
						}

						server.LobbyInfo.GlobalSettings.GameSpeedType = s;
						server.LobbyInfo.GlobalSettings.Timestep = speed.Timestep;
						server.LobbyInfo.GlobalSettings.OrderLatency =
							server.LobbyInfo.IsSinglePlayer ? 1 : speed.OrderLatency;

						server.SyncLobbyInfo();
						server.SendMessage("{0} changed Game Speed to {1}.".F(client.Name, speed.Name));

						return true;
					}
				},
				{ "kick",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can kick players.");
							return true;
						}

						var split = s.Split(' ');
						if (split.Length < 2)
						{
							server.SendOrderTo(conn, "Message", "Malformed kick command");
							return true;
						}

						int kickClientID;
						Exts.TryParseIntegerInvariant(split[0], out kickClientID);

						var kickConn = server.Conns.SingleOrDefault(c => server.GetClient(c) != null && server.GetClient(c).Index == kickClientID);
						if (kickConn == null)
						{
							server.SendOrderTo(conn, "Message", "No-one in that slot.");
							return true;
						}

						var kickClient = server.GetClient(kickConn);

						Log.Write("server", "Kicking client {0}.", kickClientID);
						server.SendMessage("{0} kicked {1} from the server.".F(client.Name, kickClient.Name));
						server.SendOrderTo(kickConn, "ServerError", "You have been kicked from the server.");
						server.DropClient(kickConn);

						bool tempBan;
						bool.TryParse(split[1], out tempBan);

						if (tempBan)
						{
							Log.Write("server", "Temporarily banning client {0} ({1}).", kickClientID, kickClient.IpAddress);
							server.SendMessage("{0} temporarily banned {1} from the server.".F(client.Name, kickClient.Name));
							server.TempBans.Add(kickClient.IpAddress);
						}

						server.SyncLobbyClients();
						server.SyncLobbySlots();

						return true;
					}
				},
				{ "name",
					s =>
					{
						var sanitizedName = Settings.SanitizedPlayerName(s);
						if (sanitizedName == client.Name)
							return true;

						Log.Write("server", "Player@{0} is now known as {1}.", conn.Socket.RemoteEndPoint, sanitizedName);
						server.SendMessage("{0} is now known as {1}.".F(client.Name, sanitizedName));
						client.Name = sanitizedName;
						server.SyncLobbyClients();
						return true;
					}
				},
				{ "faction",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseIntegerInvariant(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Map has disabled faction changes
						if (server.LobbyInfo.Slots[targetClient.Slot].LockFaction)
							return true;

						targetClient.Faction = parts[1];
						server.SyncLobbyClients();
						return true;
					}
				},
				{ "team",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseIntegerInvariant(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Map has disabled team changes
						if (server.LobbyInfo.Slots[targetClient.Slot].LockTeam)
							return true;

						int team;
						if (!Exts.TryParseIntegerInvariant(parts[1], out team))
						{
							Log.Write("server", "Invalid team: {0}", s);
							return false;
						}

						targetClient.Team = team;
						server.SyncLobbyClients();
						return true;
					}
				},
				{ "spawn",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseIntegerInvariant(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Spectators don't need a spawnpoint
						if (targetClient.Slot == null)
							return true;

						// Map has disabled spawn changes
						if (server.LobbyInfo.Slots[targetClient.Slot].LockSpawn)
							return true;

						int spawnPoint;
						if (!Exts.TryParseIntegerInvariant(parts[1], out spawnPoint)
							|| spawnPoint < 0 || spawnPoint > server.Map.SpawnPoints.Value.Length)
						{
							Log.Write("server", "Invalid spawn point: {0}", parts[1]);
							return true;
						}

						if (server.LobbyInfo.Clients.Where(cc => cc != client).Any(cc => (cc.SpawnPoint == spawnPoint) && (cc.SpawnPoint != 0)))
						{
							server.SendOrderTo(conn, "Message", "You cannot occupy the same spawn point as another player.");
							return true;
						}

						targetClient.SpawnPoint = spawnPoint;
						server.SyncLobbyClients();
						return true;
					}
				},
				{ "color",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseIntegerInvariant(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Spectator or map has disabled color changes
						if (targetClient.Slot == null || server.LobbyInfo.Slots[targetClient.Slot].LockColor)
							return true;

						// Validate if color is allowed and get an alternative it isn't
						var newColor = FieldLoader.GetValue<HSLColor>("(value)", parts[1]);
						targetClient.Color = SanitizePlayerColor(server, newColor, targetClient.Index, conn);

						// Only update player's preferred color if new color is valid
						if (newColor == targetClient.Color)
							targetClient.PreferredColor = targetClient.Color;

						server.SyncLobbyClients();
						return true;
					}
				},
				{ "shortgame",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option.");
							return true;
						}

						if (server.Map.Options.ShortGame.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled short game configuration.");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.ShortGame);
						server.SyncLobbyGlobalSettings();
						server.SendMessage("{0} {1} Short Game."
							.F(client.Name, server.LobbyInfo.GlobalSettings.ShortGame ? "enabled" : "disabled"));

						return true;
					}
				},
				{ "sync_lobby",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set lobby info");
							return true;
						}

						var lobbyInfo = Session.Deserialize(s);
						if (lobbyInfo == null)
						{
							server.SendOrderTo(conn, "Message", "Invalid Lobby Info Sent");
							return true;
						}

						server.LobbyInfo = lobbyInfo;

						server.SyncLobbyInfo();
						return true;
					}
				}
			};

			var cmdName = cmd.Split(' ').First();
			var cmdValue = cmd.Split(' ').Skip(1).JoinWith(" ");

			Func<string, bool> a;
			if (!dict.TryGetValue(cmdName, out a))
				return false;

			return a(cmdValue);
		}

		public void ServerStarted(S server)
		{
			LoadMap(server);
			SetDefaultDifficulty(server);
		}

		static Session.Slot MakeSlotFromPlayerReference(PlayerReference pr)
		{
			if (!pr.Playable) return null;
			return new Session.Slot
			{
				PlayerReference = pr.Name,
				Closed = false,
				AllowBots = pr.AllowBots,
				LockFaction = pr.LockFaction,
				LockColor = pr.LockColor,
				LockTeam = pr.LockTeam,
				LockSpawn = pr.LockSpawn,
				Required = pr.Required,
			};
		}

		static void LoadMap(S server)
		{
			server.Map = new Map(server.ModData.MapCache[server.LobbyInfo.GlobalSettings.Map].Path);

			server.MapPlayers = new MapPlayers(server.Map.PlayerDefinitions);
			server.LobbyInfo.Slots = server.MapPlayers.Players
				.Select(p => MakeSlotFromPlayerReference(p.Value))
				.Where(s => s != null)
				.ToDictionary(s => s.PlayerReference, s => s);

			server.Map.Options.UpdateServerSettings(server.LobbyInfo.GlobalSettings);
		}

		static void SetDefaultDifficulty(S server)
		{
			if (!server.Map.Options.Difficulties.Any())
			{
				server.LobbyInfo.GlobalSettings.Difficulty = null;
				return;
			}

			if (!server.Map.Options.Difficulties.Contains(server.LobbyInfo.GlobalSettings.Difficulty))
				server.LobbyInfo.GlobalSettings.Difficulty = server.Map.Options.Difficulties.First();
		}

		static HSLColor SanitizePlayerColor(S server, HSLColor askedColor, int playerIndex, Connection connectionToEcho = null)
		{
			var validator = server.ModData.Manifest.Get<ColorValidator>();
			var askColor = askedColor;

			Action<string> onError = message =>
			{
				if (connectionToEcho != null)
					server.SendOrderTo(connectionToEcho, "Message", message);
			};

			var tileset = server.Map.Rules.TileSets[server.Map.Tileset];
			var terrainColors = tileset.TerrainInfo.Where(ti => ti.RestrictPlayerColor).Select(ti => ti.Color).ToList();
			var playerColors = server.LobbyInfo.Clients.Where(c => c.Index != playerIndex).Select(c => c.Color.RGB)
				.Concat(server.MapPlayers.Players.Values.Select(p => p.Color.RGB)).ToList();

			return validator.MakeValid(askColor.RGB, server.Random, terrainColors, playerColors, onError);
		}

		public void ClientJoined(S server, Connection conn)
		{
			var client = server.GetClient(conn);

			// Validate whether color is allowed and get an alternative if it isn't
			if (client.Slot == null || !server.LobbyInfo.Slots[client.Slot].LockColor)
				client.Color = SanitizePlayerColor(server, client.Color, client.Index);
		}
	}
}
