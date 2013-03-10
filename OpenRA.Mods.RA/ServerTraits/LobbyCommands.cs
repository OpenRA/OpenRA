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
			if (!server.lobbyInfo.Slots.ContainsKey(arg))
			{
				Log.Write("server", "Invalid slot: {0}", arg);
				return false;
			}

			if (requiresHost && !client.IsAdmin)
			{
				server.SendChatTo(conn, "Only the host can do that");
				return false;
			}

			return true;
		}

		public static bool ValidateCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (server.State == ServerState.GameStarted)
			{
				server.SendChatTo(conn, "Cannot change state when game started. ({0})".F(cmd));
				return false;
			}
			else if (client.State == Session.ClientState.Ready && !(cmd == "ready" || cmd == "startgame"))
			{
				server.SendChatTo(conn, "Cannot change state when marked as ready.");
				return false;
			}

			return true;
		}

		void CheckAutoStart(S server, Connection conn, Session.Client client)
		{
			var actualPlayers = server.conns
				.Select(c => server.GetClient(c))
				.Where(c => c.Slot != null);

			if (actualPlayers.Count() > 0 && actualPlayers.All(c => c.State == Session.ClientState.Ready))
				InterpretCommand(server, conn, client, "startgame");
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
						if (server.lobbyInfo.Slots.Any(sl => sl.Value.Required && 
							server.lobbyInfo.ClientInSlot(sl.Key) == null))
						{
							server.SendChat(conn, "Unable to start the game until required slots are full.");
							return true;
						}
						server.StartGame();
						return true;
					}},
				{ "lag",
					s =>
					{
						int lag;
						if (!int.TryParse(s, out lag)) { Log.Write("server", "Invalid order lag: {0}", s); return false; }

						Log.Write("server", "Order lag is now {0} frames.", lag);

						server.lobbyInfo.GlobalSettings.OrderLatency = lag;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot",
					s =>
					{
						if (!server.lobbyInfo.Slots.ContainsKey(s))
						{
							Log.Write("server", "Invalid slot: {0}", s );
							return false;
						}
						var slot = server.lobbyInfo.Slots[s];

						if (slot.Closed || server.lobbyInfo.ClientInSlot(s) != null)
							return false;

						client.Slot = s;
						S.SyncClientToPlayerReference(client, server.Map.Players[s]);

						server.SyncLobbyInfo();
						CheckAutoStart(server, conn, client);

						return true;
					}},
				{ "spectate",
					s =>
					{
						client.Slot = null;
						client.SpawnPoint = 0;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_close",
					s =>
					{
						if (!ValidateSlotCommand( server, conn, client, s, true ))
							return false;

						// kick any player that's in the slot
						var occupant = server.lobbyInfo.ClientInSlot(s);
						if (occupant != null)
						{
							if (occupant.Bot != null)
								server.lobbyInfo.Clients.Remove(occupant);
							else
							{
								var occupantConn = server.conns.FirstOrDefault( c => c.PlayerIndex == occupant.Index );
								if (occupantConn != null)
								{
									server.SendOrderTo(occupantConn, "ServerError", "Your slot was closed by the host");
									server.DropClient(occupantConn);
								}
							}
						}

						server.lobbyInfo.Slots[s].Closed = true;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_open",
					s =>
					{
						if (!ValidateSlotCommand( server, conn, client, s, true ))
							return false;

						var slot = server.lobbyInfo.Slots[s];
						slot.Closed = false;

						// Slot may have a bot in it
						var occupant = server.lobbyInfo.ClientInSlot(s);
						if (occupant != null && occupant.Bot != null)
							server.lobbyInfo.Clients.Remove(occupant);

						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_bot",
					s =>
					{
						var parts = s.Split(' ');

						if (parts.Length < 2)
						{
							server.SendChatTo( conn, "Malformed slot_bot command" );
							return true;
						}

						if (!ValidateSlotCommand( server, conn, client, parts[0], true ))
							return false;

						var slot = server.lobbyInfo.Slots[parts[0]];
						var bot = server.lobbyInfo.ClientInSlot(parts[0]);
						var botType = parts.Skip(1).JoinWith(" ");

						// Invalid slot
						if (bot != null && bot.Bot == null)
						{
							server.SendChatTo( conn, "Can't add bots to a slot with another client" );
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
								State = Session.ClientState.NotReady
							};

							// pick a random color for the bot
							var hue = (byte)server.Random.Next(255);
							var sat = (byte)server.Random.Next(255);
							var lum = (byte)server.Random.Next(51,255);
							bot.ColorRamp = bot.PreferredColorRamp = new ColorRamp(hue, sat, lum, 10);

							server.lobbyInfo.Clients.Add(bot);
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
							server.SendChatTo( conn, "Only the host can change the map" );
							return true;
						}
						if(!server.ModData.AvailableMaps.ContainsKey(s))
						{
							server.SendChatTo( conn, "Map not found");
							return true;
						}
						server.lobbyInfo.GlobalSettings.Map = s;
						var oldSlots = server.lobbyInfo.Slots.Keys.ToArray();
						LoadMap(server);
						SetDefaultDifficulty(server);

						// Reassign players into new slots based on their old slots:
						//  - Observers remain as observers
						//  - Players who now lack a slot are made observers
						//  - Bots who now lack a slot are dropped
						var slots = server.lobbyInfo.Slots.Keys.ToArray();
						int i = 0;
						foreach (var os in oldSlots)
						{
							var c = server.lobbyInfo.ClientInSlot(os);
							if (c == null)
								continue;

							c.SpawnPoint = 0;
							c.State = Session.ClientState.NotReady;
							c.Slot = i < slots.Length ? slots[i++] : null;
							if (c.Slot != null)
							{
								// Remove Bot from slot if slot forbids bots
								if (c.Bot != null && !server.Map.Players[c.Slot].AllowBots)
									server.lobbyInfo.Clients.Remove(c);
								S.SyncClientToPlayerReference(c, server.Map.Players[c.Slot]);
							}
							else if (c.Bot != null)
								server.lobbyInfo.Clients.Remove(c);
						}

						server.SyncLobbyInfo();
						return true;
					}},
				{ "lockteams",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendChatTo( conn, "Only the host can set that option" );
							return true;
						}

						bool.TryParse(s, out server.lobbyInfo.GlobalSettings.LockTeams);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "allowcheats",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendChatTo( conn, "Only the host can set that option" );
							return true;
						}

						bool.TryParse(s, out server.lobbyInfo.GlobalSettings.AllowCheats);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "assignteams",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendChatTo(conn, "Only the host can set that option");
							return true;
						}

						int teams;
						if (!int.TryParse(s, out teams))
						{
							server.SendChatTo(conn, "Number of teams could not be parsed: {0}".F(s));
							return true;
						}
						teams = teams.Clamp(2, 8);

						var clients = server.lobbyInfo.Slots
							.Select(slot => server.lobbyInfo.Clients.SingleOrDefault(c => c.Slot == slot.Key))
							.Where(c => c != null && !server.lobbyInfo.Slots[c.Slot].LockTeam).ToArray();
						if (clients.Length < 2)
						{
							server.SendChatTo(conn, "Not enough clients to assign teams");
							return true;
						}

						var teamSizes = new int[clients.Length];
						for (var i = 0; i < clients.Length; i++)
							teamSizes[i % teams]++;

						var clientIndex = 0;
						for (var team = 1; team <= teams; team++)
						{
							for (var teamClientIndex = 0; teamClientIndex < teamSizes[team - 1]; clientIndex++, teamClientIndex++)
							{
								var cl = clients[clientIndex];
								if (cl.Bot == null)
									cl.State = Session.ClientState.NotReady;
								cl.Team = team;
							}
						}
						server.SyncLobbyInfo();
						return true;
					}},
				{ "crates",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendChatTo(conn, "Only the host can set that option");
							return true;
						}

						bool.TryParse(s, out server.lobbyInfo.GlobalSettings.Crates);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "difficulty",
					s =>
					{
						if (!client.IsAdmin)
						{
							server.SendChatTo(conn, "Only the host can set that option");
							return true;
						}
						if ((server.Map.Difficulties == null && s != null) || (server.Map.Difficulties != null && !server.Map.Difficulties.Contains(s)))
						{
							server.SendChatTo(conn, "Unsupported difficulty selected: {0}".F(s));
							server.SendChatTo(conn, "Supported difficulties: {0}".F(server.Map.Difficulties.JoinWith(",")));
							return true;
						}

						server.lobbyInfo.GlobalSettings.Difficulty = s;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "kick",
					s =>
					{

						if (!client.IsAdmin)
						{
							server.SendChatTo( conn, "Only the host can kick players" );
							return true;
						}

						int clientID;
						int.TryParse( s, out clientID );

						var connToKick = server.conns.SingleOrDefault( c => server.GetClient(c) != null && server.GetClient(c).Index == clientID);
						if (connToKick == null)
						{
							server.SendChatTo( conn, "Noone in that slot." );
							return true;
						}

						server.SendOrderTo(connToKick, "ServerError", "You have been kicked from the server");
						server.DropClient(connToKick);
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
						var targetClient = server.lobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Map has disabled race changes
						if (server.lobbyInfo.Slots[targetClient.Slot].LockRace)
							return true;

						targetClient.Country = parts[1];
						server.SyncLobbyInfo();
						return true;
					}},
				{ "team",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.lobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Map has disabled team changes
						if (server.lobbyInfo.Slots[targetClient.Slot].LockTeam)
							return true;

						int team;
						if (!int.TryParse(parts[1], out team)) { Log.Write("server", "Invalid team: {0}", s ); return false; }

						targetClient.Team = team;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "spawn",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.lobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Spectators don't need a spawnpoint
						if (targetClient.Slot == null)
							return true;

						// Map has disabled spawn changes
						if (server.lobbyInfo.Slots[targetClient.Slot].LockSpawn)
							return true;

						int spawnPoint;
						if (!int.TryParse(parts[1], out spawnPoint) || spawnPoint < 0 || spawnPoint > server.Map.GetSpawnPoints().Length)
						{
							Log.Write("server", "Invalid spawn point: {0}", parts[1]);
							return true;
						}

						if (server.lobbyInfo.Clients.Where( cc => cc != client ).Any( cc => (cc.SpawnPoint == spawnPoint) && (cc.SpawnPoint != 0) ))
						{
							server.SendChatTo( conn, "You can't be at the same spawn point as another player" );
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
						var targetClient = server.lobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && !client.IsAdmin)
							return true;

						// Map has disabled color changes
						if (targetClient.Slot != null && server.lobbyInfo.Slots[targetClient.Slot].LockColor)
							return true;

						var ci = parts[1].Split(',').Select(cc => int.Parse(cc)).ToArray();
						targetClient.ColorRamp = targetClient.PreferredColorRamp = new ColorRamp((byte)ci[0], (byte)ci[1], (byte)ci[2], (byte)ci[3]);
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
			server.Map = new Map(server.ModData.AvailableMaps[server.lobbyInfo.GlobalSettings.Map].Path);
			server.lobbyInfo.Slots = server.Map.Players
				.Select(p => MakeSlotFromPlayerReference(p.Value))
				.Where(s => s != null)
				.ToDictionary(s => s.PlayerReference, s => s);
		}

		static void SetDefaultDifficulty(S server)
		{
			if (server.Map.Difficulties != null && server.Map.Difficulties.Any())
			{
				if (!server.Map.Difficulties.Contains(server.lobbyInfo.GlobalSettings.Difficulty))
					server.lobbyInfo.GlobalSettings.Difficulty = server.Map.Difficulties.First();
			}
			else server.lobbyInfo.GlobalSettings.Difficulty = null;
		}
	}
}
