#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.FileFormats;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.RA.Server
{
	public class LobbyCommands : ServerTrait, IInterpretCommand, INotifyServerStart
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
				server.SendOrderTo(conn, "Message", "Only the host can do that");
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
			else if (client.State == Session.ClientState.Ready && !(cmd == "ready" || cmd == "startgame"))
			{
				server.SendOrderTo(conn, "Message", "Cannot change state when marked as ready.");
				return false;
			}

			return true;
		}

		void CheckAutoStart(S server, Connection conn, Session.Client client)
		{
			var playerClients = server.LobbyInfo.Clients.Where(c => c.Bot == null && c.Slot != null);

			// Are all players ready?
			if (playerClients.Count() == 0 || playerClients.Any(c => c.State != Session.ClientState.Ready))
				return;

			// Are the map conditions satisfied?
			if (server.LobbyInfo.Slots.Any(sl => sl.Value.Required && server.LobbyInfo.ClientInSlot(sl.Key) == null))
				return;

			server.StartGame();
		}

		public bool InterpretCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (!ValidateCommand(server, conn, client, cmd))
				return false;

			var dict = new Dictionary<string, Func<string, bool>>
			{
				{ "ready",
					s =>
					{
						// if we're downloading, we can't ready up.
						if (client.State == Session.ClientState.NotReady)
							client.State = Session.ClientState.Ready;
						else if (client.State == Session.ClientState.Ready)
							client.State = Session.ClientState.NotReady;

						Log.Write("server", "Player @{0} is {1}",
							conn.socket.RemoteEndPoint, client.State);

						server.SyncLobbyInfo();

						CheckAutoStart(server, conn, client);

						return true;
					}},
				{ "startgame",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can start the game");
							return true;
						}

						if (server.LobbyInfo.Slots.Any(sl => sl.Value.Required && 
							server.LobbyInfo.ClientInSlot(sl.Key) == null))
						{
							server.SendOrderTo(conn, "Message", "Unable to start the game until required slots are full.");
							return true;
						}
						server.StartGame();
						return true;
					}},
				{ "slot",
					s =>
					{
						if (!server.LobbyInfo.Slots.ContainsKey(s))
						{
							Log.Write("server", "Invalid slot: {0}", s );
							return false;
						}
						var slot = server.LobbyInfo.Slots[s];

						if (slot.Closed || server.LobbyInfo.ClientInSlot(s) != null)
							return false;

						client.Slot = s;
						S.SyncClientToPlayerReference(client, server.Map.Players[s]);

						server.SyncLobbyInfo();
						CheckAutoStart(server, conn, client);

						return true;
					}},
				{ "allow_spectators",
					s =>
					{
						if (bool.TryParse(s, out server.LobbyInfo.GlobalSettings.AllowSpectators))
						{
							server.SyncLobbyInfo();
							return true;
						}
						else
						{
							server.SendOrderTo(conn, "Message", "Malformed allow_spectate command");
							return true;
						}
					}},
				{ "spectate",
					s =>
					{
						if (server.LobbyInfo.GlobalSettings.AllowSpectators || client.IsAdmin)
						{
							client.Slot = null;
							client.SpawnPoint = 0;
							client.Color = HSLColor.FromRGB(255, 255, 255);
							server.SyncLobbyInfo();
							return true;
						}
						else
							return false;
					}},
				{ "slot_close",
					s =>
					{
						if (!ValidateSlotCommand( server, conn, client, s, true ))
							return false;

						// kick any player that's in the slot
						var occupant = server.LobbyInfo.ClientInSlot(s);
						if (occupant != null)
						{
							if (occupant.Bot != null)
								server.LobbyInfo.Clients.Remove(occupant);
							else
							{
								var occupantConn = server.Conns.FirstOrDefault( c => c.PlayerIndex == occupant.Index );
								if (occupantConn != null)
								{
									server.SendOrderTo(occupantConn, "ServerError", "Your slot was closed by the host");
									server.DropClient(occupantConn);
								}
							}
						}

						server.LobbyInfo.Slots[s].Closed = true;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_open",
					s =>
					{
						if (!ValidateSlotCommand( server, conn, client, s, true ))
							return false;

						var slot = server.LobbyInfo.Slots[s];
						slot.Closed = false;

						// Slot may have a bot in it
						var occupant = server.LobbyInfo.ClientInSlot(s);
						if (occupant != null && occupant.Bot != null)
							server.LobbyInfo.Clients.Remove(occupant);

						server.SyncLobbyInfo();
						return true;
					}},
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
						if (!int.TryParse(parts[1], out controllerClientIndex))
						{
							Log.Write("server", "Invalid bot controller client index: {0}", parts[1]);
							return false;
						}
						var botType = parts.Skip(2).JoinWith(" ");

						// Invalid slot
						if (bot != null && bot.Bot == null)
						{
							server.SendOrderTo(conn, "Message", "Can't add bots to a slot with another client");
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
								Country = "random",
								SpawnPoint = 0,
								Team = 0,
								State = Session.ClientState.NotReady,
								BotControllerClientIndex = controllerClientIndex
							};

							// pick a random color for the bot
							var hue = (byte)server.Random.Next(255);
							var sat = (byte)server.Random.Next(255);
							var lum = (byte)server.Random.Next(51,255);
							bot.Color = bot.PreferredColor = new HSLColor(hue, sat, lum);

							server.LobbyInfo.Clients.Add(bot);
						}
						else
						{
							// Change the type of the existing bot
							bot.Name = botType;
							bot.Bot = botType;
						}

						S.SyncClientToPlayerReference(bot, server.Map.Players[parts[0]]);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "map",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can change the map");
							return true;
						}

						if (!server.ModData.AvailableMaps.ContainsKey(s))
						{
							server.SendOrderTo(conn, "Message", "Map was not found on server");
							return true;
						}
						server.LobbyInfo.GlobalSettings.Map = s;
						var oldSlots = server.LobbyInfo.Slots.Keys.ToArray();
						LoadMap(server);
						SetDefaultDifficulty(server);

						// Reassign players into new slots based on their old slots:
						//  - Observers remain as observers
						//  - Players who now lack a slot are made observers
						//  - Bots who now lack a slot are dropped
						var slots = server.LobbyInfo.Slots.Keys.ToArray();
						int i = 0;
						foreach (var os in oldSlots)
						{
							var c = server.LobbyInfo.ClientInSlot(os);
							if (c == null)
								continue;

							c.SpawnPoint = 0;
							c.State = Session.ClientState.NotReady;
							c.Slot = i < slots.Length ? slots[i++] : null;
							if (c.Slot != null)
							{
								// Remove Bot from slot if slot forbids bots
								if (c.Bot != null && !server.Map.Players[c.Slot].AllowBots)
									server.LobbyInfo.Clients.Remove(c);
								S.SyncClientToPlayerReference(c, server.Map.Players[c.Slot]);
							}
							else if (c.Bot != null)
								server.LobbyInfo.Clients.Remove(c);
						}

						server.SyncLobbyInfo();
						return true;
					}},
				{ "fragilealliance",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						if (server.Map.Options.FragileAlliances.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled alliance configuration");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.FragileAlliances);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "allowcheats",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						if (server.Map.Options.Cheats.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled cheat configuration");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.AllowCheats);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "shroud",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						if (server.Map.Options.Shroud.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled shroud configuration");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.Shroud);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "fog",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						if (server.Map.Options.Fog.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled fog configuration");
							return true;
						}


						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.Fog);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "assignteams",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						int teamCount;
						if (!int.TryParse(s, out teamCount))
						{
							server.SendOrderTo(conn, "Message", "Number of teams could not be parsed: {0}".F(s));
							return true;
						}

						var maxTeams = (server.LobbyInfo.Clients.Count(c => c.Slot != null) + 1) / 2;
						teamCount = teamCount.Clamp(0, maxTeams);
						var players = server.LobbyInfo.Slots
							.Select(slot => server.LobbyInfo.ClientInSlot(slot.Key))
							.Where(c => c != null && !server.LobbyInfo.Slots[c.Slot].LockTeam);

						var assigned = 0;
						var playerCount = players.Count();
						foreach (var player in players)
						{
							// Free for all
							if (teamCount == 0)
								player.Team = 0;

							// Humans vs Bots
							else if (teamCount == 1)
								player.Team = player.Bot == null ? 1 : 2;

							else
								player.Team = assigned++ * teamCount / playerCount + 1;
						}

						server.SyncLobbyInfo();
						return true;
					}},
				{ "crates",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						if (server.Map.Options.Crates.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled crate configuration");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.Crates);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "allybuildradius",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						if (server.Map.Options.AllyBuildRadius.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled ally build radius configuration");
							return true;
						}

						bool.TryParse(s, out server.LobbyInfo.GlobalSettings.AllyBuildRadius);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "difficulty",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						if (s != null && !server.Map.Options.Difficulties.Contains(s))
						{
							server.SendOrderTo(conn, "Message", "Unsupported difficulty selected: {0}".F(s));
							server.SendOrderTo(conn, "Message", "Supported difficulties: {0}".F(server.Map.Options.Difficulties.JoinWith(",")));
							return true;
						}

						server.LobbyInfo.GlobalSettings.Difficulty = s;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "startingunits",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						if (!server.Map.Options.ConfigurableStartingUnits)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled start unit configuration");
							return true;
						}

						server.LobbyInfo.GlobalSettings.StartingUnitsClass = s;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "startingcash",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can set that option");
							return true;
						}

						if (server.Map.Options.StartingCash.HasValue)
						{
							server.SendOrderTo(conn, "Message", "Map has disabled cash configuration");
							return true;
						}

						server.LobbyInfo.GlobalSettings.StartingCash = int.Parse(s);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "kick",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendOrderTo(conn, "Message", "Only the host can kick players");
							return true;
						}

						var split = s.Split(' ');
						if (split.Length < 2)
						{
							server.SendOrderTo(conn, "Message", "Malformed kick command");
							return true;
						}

						int kickClientID;
						int.TryParse(split[0], out kickClientID);

						var kickConn = server.Conns.SingleOrDefault(c => server.GetClient(c) != null && server.GetClient(c).Index == kickClientID);
						if (kickConn == null)
						{
							server.SendOrderTo(conn, "Message", "Noone in that slot.");
							return true;
						}

						var kickConnIP = server.GetClient(kickConn).IpAddress;

						Log.Write("server", "Kicking client {0} as requested", kickClientID);
						server.SendOrderTo(kickConn, "ServerError", "You have been kicked from the server");
						server.DropClient(kickConn);

						bool tempBan;
						bool.TryParse(split[1], out tempBan);

						if (tempBan)
						{
							Log.Write("server", "Temporarily banning client {0} ({1}) as requested", kickClientID, kickConnIP);
							server.TempBans.Add(kickConnIP);
						}

						server.SyncLobbyInfo();
						return true;
					}},
				{ "name",
					s =>
					{
						Log.Write("server", "Player@{0} is now known as {1}", conn.socket.RemoteEndPoint, s);
						client.Name = s;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "race",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.LobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Map has disabled race changes
						if (server.LobbyInfo.Slots[targetClient.Slot].LockRace)
							return true;

						targetClient.Country = parts[1];
						server.SyncLobbyInfo();
						return true;
					}},
				{ "team",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.LobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Map has disabled team changes
						if (server.LobbyInfo.Slots[targetClient.Slot].LockTeam)
							return true;

						int team;
						if (!int.TryParse(parts[1], out team))
						{
							Log.Write("server", "Invalid team: {0}", s );
							return false;
						}

						targetClient.Team = team;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "spawn",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.LobbyInfo.ClientWithIndex(int.Parse(parts[0]));

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
						if (!int.TryParse(parts[1], out spawnPoint) || spawnPoint < 0 || spawnPoint > server.Map.GetSpawnPoints().Length)
						{
							Log.Write("server", "Invalid spawn point: {0}", parts[1]);
							return true;
						}

						if (server.LobbyInfo.Clients.Where( cc => cc != client ).Any( cc => (cc.SpawnPoint == spawnPoint) && (cc.SpawnPoint != 0) ))
						{
							server.SendOrderTo(conn, "Message", "You can't be at the same spawn point as another player");
							return true;
						}

						targetClient.SpawnPoint = spawnPoint;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "color",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.LobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Spectator or map has disabled color changes
						if (targetClient.Slot == null || server.LobbyInfo.Slots[targetClient.Slot].LockColor)
							return true;

						var ci = parts[1].Split(',').Select(cc => int.Parse(cc)).ToArray();
						targetClient.Color = targetClient.PreferredColor = new HSLColor((byte)ci[0], (byte)ci[1], (byte)ci[2]);
						server.SyncLobbyInfo();
						return true;
					}}
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
			if (Game.Settings.Server.LockBots)
				pr.AllowBots = false;
			return new Session.Slot
			{
				PlayerReference = pr.Name,
				Closed = false,
				AllowBots = pr.AllowBots,
				LockRace = pr.LockRace,
				LockColor = pr.LockColor,
				LockTeam = pr.LockTeam,
				LockSpawn = pr.LockSpawn,
				Required = pr.Required,
			};
		}

		static void LoadMap(S server)
		{
			server.Map = new Map(server.ModData.AvailableMaps[server.LobbyInfo.GlobalSettings.Map].Path);
			server.LobbyInfo.Slots = server.Map.Players
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
	}
}
