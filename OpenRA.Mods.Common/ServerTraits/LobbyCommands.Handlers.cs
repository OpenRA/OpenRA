#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Server;
using OpenRA.Support;
using OpenRA.Traits;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public partial class LobbyCommands
	{
		static bool State(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!Enum.TryParse<Session.ClientState>(s, out var state))
				{
					server.SendFluentMessageTo(conn, MalformedCommand, ["command", "state"]);

					return true;
				}

				client.State = state;
				Log.Write("server", $"Player @{conn.EndPoint} is {client.State}");

				server.SyncLobbyClients();
				CheckAutoStart(server);

				return true;
			}
		}

		static bool StartGame(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendFluentMessageTo(conn, OnlyHostStartGame);
					return true;
				}

				if (server.LobbyInfo.Slots.Any(sl => sl.Value.Required && server.LobbyInfo.ClientInSlot(sl.Key) == null))
				{
					server.SendFluentMessageTo(conn, NoStartUntilRequiredSlotsFull);
					return true;
				}

				if (server.LobbyInfo.Slots.All(sl => server.LobbyInfo.ClientInSlot(sl.Key)?.State is null or Session.ClientState.Invalid))
				{
					server.SendFluentMessageTo(conn, NoStartWithoutPlayers);
					return true;
				}

				if (!server.LobbyInfo.GlobalSettings.EnableSingleplayer && server.LobbyInfo.NonBotPlayers.Count() < 2)
				{
					server.SendFluentMessageTo(conn, TwoHumansRequired);
					return true;
				}

				if (LobbyUtils.InsufficientEnabledSpawnPoints(server.Map, server.LobbyInfo))
				{
					server.SendFluentMessageTo(conn, InsufficientEnabledSpawnPoints);
					return true;
				}

				server.StartGame();

				return true;
			}
		}

		static bool Slot(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!server.LobbyInfo.Slots.TryGetValue(s, out var slot))
				{
					Log.Write("server", $"Invalid slot: {s}");
					return false;
				}

				if (slot.Closed || server.LobbyInfo.ClientInSlot(s) != null)
					return false;

				// If the previous slot had a locked spawn then we must not carry that to the new slot
				var oldSlot = client.Slot != null ? server.LobbyInfo.Slots[client.Slot] : null;
				if (oldSlot != null && oldSlot.LockSpawn)
					client.SpawnPoint = 0;

				client.Slot = s;
				S.SyncClientToPlayerReference(client, server.Map.Players.Players[s]);

				if (!slot.LockColor)
					client.PreferredColor = client.Color = SanitizePlayerColor(server, client.Color, client.Index, conn);

				server.SyncLobbyClients();
				CheckAutoStart(server);

				return true;
			}
		}

		static bool AllowSpectators(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (bool.TryParse(s, out server.LobbyInfo.GlobalSettings.AllowSpectators))
				{
					server.SyncLobbyGlobalSettings();
					return true;
				}

				server.SendFluentMessageTo(conn, MalformedCommand, ["command", "allow_spectate"]);

				return true;
			}
		}

		static bool Specate(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (server.LobbyInfo.GlobalSettings.AllowSpectators || client.IsAdmin)
				{
					client.Slot = null;
					client.SpawnPoint = 0;
					client.Team = 0;
					client.Handicap = 0;
					client.Color = Color.White;
					server.SyncLobbyClients();
					CheckAutoStart(server);
					return true;
				}

				return false;
			}
		}

		static bool SlotClose(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
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
					}
					else
					{
						var occupantConn = server.Conns.FirstOrDefault(c => c.PlayerIndex == occupant.Index);
						if (occupantConn != null)
						{
							server.SendOrderTo(conn, "ServerError", SlotClosed);
							server.DropClient(occupantConn);
						}
					}
				}

				server.LobbyInfo.Slots[s].Closed = true;
				server.SyncLobbySlots();

				return true;
			}
		}

		static bool SlotOpen(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!ValidateSlotCommand(server, conn, client, s, true))
					return false;

				var slot = server.LobbyInfo.Slots[s];
				slot.Closed = false;
				server.SyncLobbySlots();

				// Slot may have a bot in it
				var occupant = server.LobbyInfo.ClientInSlot(s);
				if (occupant != null && occupant.Bot != null)
					server.LobbyInfo.Clients.Remove(occupant);

				server.SyncLobbyClients();

				return true;
			}
		}

		static bool SlotBot(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				var parts = s.Split(' ');
				if (parts.Length < 3)
				{
					server.SendFluentMessageTo(conn, MalformedCommand, ["command", "slot_bot"]);
					return true;
				}

				if (!ValidateSlotCommand(server, conn, client, parts[0], true))
					return false;

				var slot = server.LobbyInfo.Slots[parts[0]];
				var bot = server.LobbyInfo.ClientInSlot(parts[0]);
				if (!Exts.TryParseInt32Invariant(parts[1], out var controllerClientIndex))
				{
					Log.Write("server", $"Invalid bot controller client index: {parts[1]}");
					return false;
				}

				// Invalid slot
				if (bot != null && bot.Bot == null)
				{
					server.SendFluentMessageTo(conn, InvalidBotSlot);
					return true;
				}

				var botType = parts[2];
				var botInfo = server.Map.PlayerActorInfo.TraitInfos<IBotInfo>()
					.FirstOrDefault(b => b.Type == botType);

				if (botInfo == null)
				{
					server.SendFluentMessageTo(conn, InvalidBotType);
					return true;
				}

				slot.Closed = false;
				if (bot == null)
				{
					// Create a new bot
					bot = new Session.Client()
					{
						Index = server.ChooseFreePlayerIndex(),
						Name = botInfo.Name,
						Bot = botType,
						Slot = parts[0],
						Faction = "Random",
						SpawnPoint = 0,
						Team = 0,
						Handicap = 0,
						State = Session.ClientState.NotReady,
						BotControllerClientIndex = controllerClientIndex
					};

					// Pick a random color for the bot
					var colorManager = server.ModData.DefaultRules.Actors[SystemActors.World].TraitInfo<IColorPickerManagerInfo>();
					var terrainColors = server.ModData.DefaultTerrainInfo[server.Map.TileSet].RestrictedPlayerColors.ToList();
					var playerColors = server.LobbyInfo.Clients.Select(c => c.Color)
						.Concat(server.Map.Players.Players.Values.Select(p => p.Color)).ToList();

					bot.Color = bot.PreferredColor = colorManager.RandomPresetColor(server.Random, terrainColors, playerColors);

					server.LobbyInfo.Clients.Add(bot);
				}
				else
				{
					// Change the type of the existing bot
					bot.Name = botInfo.Name;
					bot.Bot = botType;
				}

				S.SyncClientToPlayerReference(bot, server.Map.Players.Players[parts[0]]);
				server.SyncLobbyClients();
				server.SyncLobbySlots();

				return true;
			}
		}

		static bool Map(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendFluentMessageTo(conn, HostChangeMap);
					return true;
				}

				if (server.MapPool != null && !server.MapPool.Contains(s))
				{
					QueryFailed();
					return true;
				}

				var lastMap = server.LobbyInfo.GlobalSettings.Map;
				void SelectMap(MapPreview map)
				{
					lock (server.LobbyInfo)
					{
						// Make sure the map hasn't changed in the meantime
						if (server.LobbyInfo.GlobalSettings.Map != lastMap)
							return;

						server.LobbyInfo.GlobalSettings.Map = map.Uid;

						var oldSlots = server.LobbyInfo.Slots.Keys.ToArray();
						server.Map = server.ModData.MapCache[server.LobbyInfo.GlobalSettings.Map];
						server.LobbyInfo.GlobalSettings.MapStatus = server.MapStatusCache[server.Map];

						server.LobbyInfo.Slots = server.Map.Players.Players
							.Select(p => MakeSlotFromPlayerReference(p.Value))
							.Where(ss => ss != null)
							.ToDictionary(ss => ss.PlayerReference, ss => ss);

						LoadMapSettings(server, server.LobbyInfo.GlobalSettings, server.Map);

						// Reset client states
						var selectableFactions = server.Map.WorldActorInfo.TraitInfos<FactionInfo>()
							.Where(f => f.Selectable)
							.Select(f => f.InternalName)
							.ToList();

						foreach (var c in server.LobbyInfo.Clients)
						{
							c.Faction = SanitizePlayerFaction(server, c.Faction, selectableFactions);
							c.State = Session.ClientState.Invalid;
						}

						// Reassign players into new slots based on their old slots:
						//  - Observers remain as observers
						//  - Players who now lack a slot are made observers
						//  - Bots who now lack a slot are dropped
						//  - Bots who are not defined in the map rules are dropped
						var botTypes = server.Map.PlayerActorInfo.TraitInfos<IBotInfo>().Select(t => t.Type);
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
								if (c.Bot != null && (!server.Map.Players.Players[c.Slot].AllowBots || !botTypes.Contains(c.Bot)))
									server.LobbyInfo.Clients.Remove(c);
								S.SyncClientToPlayerReference(c, server.Map.Players.Players[c.Slot]);
							}
							else if (c.Bot != null)
								server.LobbyInfo.Clients.Remove(c);
							else
								c.Color = Color.White;
						}

						// Validate if color is allowed and get an alternative if it isn't
						foreach (var c in server.LobbyInfo.Clients)
							if (c.Slot != null && !server.LobbyInfo.Slots[c.Slot].LockColor)
								c.Color = c.PreferredColor = SanitizePlayerColor(server, c.Color, c.Index, conn);

						server.LobbyInfo.DisabledSpawnPoints.Clear();

						server.SendFluentMessage(ChangedMap, "player", client.Name, "map", server.Map.Title);

						server.SyncLobbyInfo();

						if ((server.LobbyInfo.GlobalSettings.MapStatus & Session.MapStatus.UnsafeCustomRules) != 0)
							server.SendFluentMessage(CustomRules);

						if (!server.LobbyInfo.GlobalSettings.EnableSingleplayer)
							server.SendFluentMessage(TwoHumansRequired);
						else if (server.Map.Players.Players.Where(p => p.Value.Playable).All(p => !p.Value.AllowBots))
							server.SendFluentMessage(MapBotsDisabled);
					}
				}

				var m = server.ModData.MapCache[s];
				if (m.Status is MapStatus.Available or MapStatus.DownloadAvailable or MapStatus.Generatable or MapStatus.Generating)
					SelectMap(m);
				else if (server.Settings.QueryMapRepository)
				{
					server.SendFluentMessageTo(conn, SearchingMap);
					var mapRepository = server.ModData.GetOrCreate<WebServices>().MapRepository;
					var reported = false;
					server.ModData.MapCache.QueryRemoteMapDetails(mapRepository, [s], SelectMap, _ =>
					{
						if (!reported)
							QueryFailed();

						reported = true;
					});
				}
				else
					QueryFailed();

				return true;
			}

			void QueryFailed() => server.SendFluentMessageTo(conn, UnknownMap);
		}

		static bool Option(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendFluentMessageTo(conn, NotAdmin);
					return true;
				}

				var allOptions = server.Map.PlayerActorInfo.TraitInfos<ILobbyOptions>()
					.Concat(server.Map.WorldActorInfo.TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(server.Map));

				// Overwrite keys with duplicate ids
				var options = new Dictionary<string, LobbyOption>();
				foreach (var o in allOptions)
					options[o.Id] = o;

				var split = s.Split(' ');
				if (split.Length < 2 || !options.TryGetValue(split[0], out var option) ||
					option.IsLocked || !option.Values.ContainsKey(split[1]))
				{
					server.SendFluentMessageTo(conn, InvalidConfigurationCommand);
					return true;
				}

				var oo = server.LobbyInfo.GlobalSettings.LobbyOptions[option.Id];
				if (oo.Value == split[1])
					return true;

				if (!option.Values.ContainsKey(split[1]))
				{
					server.SendFluentMessageTo(conn, InvalidConfigurationCommand);
					return true;
				}

				oo.Value = oo.PreferredValue = split[1];

				server.SyncLobbyGlobalSettings();
				foreach (var c in server.LobbyInfo.Clients)
					c.State = Session.ClientState.NotReady;

				server.SyncLobbyClients();

				return true;
			}
		}

		static bool ResetOptions(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendFluentMessageTo(conn, NotAdmin);
					return true;
				}

				server.LobbyInfo.GlobalSettings.LobbyOptions = server.Map.PlayerActorInfo.TraitInfos<ILobbyOptions>()
					.Concat(server.Map.WorldActorInfo.TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(server.Map))
					.ToDictionary(o => o.Id, o => new Session.LobbyOptionState
					{
						IsLocked = o.IsLocked,
						Value = o.DefaultValue,
						PreferredValue = o.DefaultValue
					});

				server.SyncLobbyGlobalSettings();

				foreach (var c in server.LobbyInfo.Clients)
					c.State = Session.ClientState.NotReady;

				server.SyncLobbyClients();

				return true;
			}
		}

		static bool AssignTeams(S server, Connection conn, Session.Client client, string raw)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendFluentMessageTo(conn, AdminOption);
					return true;
				}

				if (!Exts.TryParseInt32Invariant(raw, out var teamCount))
				{
					server.SendFluentMessageTo(conn, NumberTeams, ["raw", raw]);
					return true;
				}

				var maxTeams = (server.LobbyInfo.Clients.Count(c => c.Slot != null) + 1) / 2;
				teamCount = teamCount.Clamp(0, maxTeams);
				var clients = server.LobbyInfo.Slots
					.Select(slot => server.LobbyInfo.ClientInSlot(slot.Key))
					.Where(c => c != null && !server.LobbyInfo.Slots[c.Slot].LockTeam)
					.ToList();

				var assigned = 0;
				var clientCount = clients.Count;
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
		}

		static bool Kick(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendFluentMessageTo(conn, AdminKick);
					return true;
				}

				var split = s.Split(' ');
				if (split.Length < 2)
				{
					server.SendFluentMessageTo(conn, MalformedCommand, ["command", "kick"]);
					return true;
				}

				var kickConn = Exts.TryParseInt32Invariant(split[0], out var kickClientID)
					? server.Conns.SingleOrDefault(c => server.GetClient(c)?.Index == kickClientID) : null;

				if (kickConn == null)
				{
					server.SendFluentMessageTo(conn, KickNone);
					return true;
				}

				var kickClient = server.GetClient(kickConn);
				if (client == kickClient)
				{
					server.SendFluentMessageTo(conn, NoKickSelf);
					return true;
				}

				if (server.State == ServerState.GameStarted && !kickClient.IsObserver && !server.HasClientWonOrLost(kickClient))
				{
					server.SendFluentMessageTo(conn, NoKickGameStarted);
					return true;
				}

				Log.Write("server", $"Kicking client {kickClientID}.");
				server.SendFluentMessage(AdminKicked, "admin", client.Name, "player", kickClient.Name);
				server.SendOrderTo(kickConn, "ServerError", YouWereKicked);
				server.DropClient(kickConn);

				if (bool.TryParse(split[1], out var tempBan) && tempBan)
				{
					Log.Write("server", $"Temporarily banning client {kickClientID} ({kickClient.IPAddress}).");
					server.SendFluentMessage(TempBan, "admin", client.Name, "player", kickClient.Name);
					server.TempBans.Add(kickClient.IPAddress);
				}

				server.SyncLobbyClients();
				server.SyncLobbySlots();

				return true;
			}
		}

		static bool VoteKick(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				var split = s.Split(' ');
				if (split.Length != 2)
				{
					server.SendFluentMessageTo(conn, MalformedCommand, ["command", "vote_kick"]);
					return true;
				}

				if (!server.Settings.EnableVoteKick)
				{
					server.SendFluentMessageTo(conn, VoteKickDisabled);
					return true;
				}

				var kickConn = Exts.TryParseInt32Invariant(split[0], out var kickClientID)
					? server.Conns.SingleOrDefault(c => server.GetClient(c)?.Index == kickClientID) : null;

				if (kickConn == null)
				{
					server.SendFluentMessageTo(conn, KickNone);
					return true;
				}

				var kickClient = server.GetClient(kickConn);
				if (client == kickClient)
				{
					server.SendFluentMessageTo(conn, NoKickSelf);
					return true;
				}

				if (!bool.TryParse(split[1], out var vote))
				{
					server.SendFluentMessageTo(conn, MalformedCommand, ["command", "vote_kick"]);
					return true;
				}

				if (server.VoteKickTracker.VoteKick(conn, client, kickConn, kickClient, kickClientID, vote))
				{
					Log.Write("server", $"Kicking client {kickClientID}.");
					server.SendFluentMessage(Kicked, "player", kickClient.Name);
					server.SendOrderTo(kickConn, "ServerError", YouWereKicked);
					server.DropClient(kickConn);

					server.SyncLobbyClients();
					server.SyncLobbySlots();
				}

				return true;
			}
		}

		void OpenRA.Server.ITick.Tick(S server) => server.VoteKickTracker.Tick();

		static bool MakeAdmin(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendFluentMessageTo(conn, NoTransferAdmin);
					return true;
				}

				var newAdminConn = Exts.TryParseInt32Invariant(s, out var newAdminId)
					? server.Conns.SingleOrDefault(c => server.GetClient(c)?.Index == newAdminId) : null;

				if (newAdminConn == null)
				{
					server.SendFluentMessageTo(conn, EmptySlot);
					return true;
				}

				var newAdminClient = server.GetClient(newAdminConn);
				client.IsAdmin = false;
				newAdminClient.IsAdmin = true;

				var bots = server.LobbyInfo.Slots
					.Select(slot => server.LobbyInfo.ClientInSlot(slot.Key))
					.Where(c => c != null && c.Bot != null);
				foreach (var b in bots)
					b.BotControllerClientIndex = newAdminId;

				server.SendFluentMessage(NewAdmin, "player", newAdminClient.Name);
				Log.Write("server", $"{newAdminClient.Name} is now the admin.");
				server.SyncLobbyClients();

				return true;
			}
		}

		static bool MakeSpectator(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendFluentMessageTo(conn, NoMoveSpectators);
					return true;
				}

				var targetConn = Exts.TryParseInt32Invariant(s, out var targetId)
					? server.Conns.SingleOrDefault(c => server.GetClient(c)?.Index == targetId) : null;

				if (targetConn == null)
				{
					server.SendFluentMessageTo(conn, EmptySlot);
					return true;
				}

				var targetClient = server.GetClient(targetConn);
				targetClient.Slot = null;
				targetClient.SpawnPoint = 0;
				targetClient.Team = 0;
				targetClient.Handicap = 0;
				targetClient.Color = Color.White;
				targetClient.State = Session.ClientState.NotReady;
				server.SendFluentMessage(MoveSpectators, "admin", client.Name, "player", targetClient.Name);
				Log.Write("server", $"{client.Name} moved {targetClient.Name} to spectators.");
				server.SyncLobbyClients();
				CheckAutoStart(server);

				return true;
			}
		}

		static bool Name(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				var sanitizedName = Settings.SanitizedPlayerName(s);
				if (sanitizedName == client.Name)
					return true;

				Log.Write("server", $"Player@{conn.EndPoint} is now known as {sanitizedName}.");
				server.SendFluentMessage(Nick, "player", client.Name, "name", sanitizedName);
				client.Name = sanitizedName;
				server.SyncLobbyClients();

				return true;
			}
		}

		static bool Faction(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				var parts = s.Split(' ');
				var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseInt32Invariant(parts[0]));

				// Only the host can change other client's info
				if (targetClient.Index != client.Index && !client.IsAdmin)
					return true;

				// Map has disabled faction changes
				if (server.LobbyInfo.Slots[targetClient.Slot].LockFaction)
					return true;

				var faction = parts[1];
				var isValidFaction = server.Map.WorldActorInfo.TraitInfos<FactionInfo>()
					.Any(f => f.Selectable && f.InternalName == client.Faction);

				if (!isValidFaction)
				{
					server.SendFluentMessageTo(conn, InvalidFactionSelected, ["faction", faction]);
					return true;
				}

				targetClient.Faction = faction;
				server.SyncLobbyClients();

				return true;
			}
		}

		static bool Team(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				var parts = s.Split(' ');
				var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseInt32Invariant(parts[0]));

				// Only the host can change other client's info
				if (targetClient.Index != client.Index && !client.IsAdmin)
					return true;

				// Map has disabled team changes
				if (server.LobbyInfo.Slots[targetClient.Slot].LockTeam)
					return true;

				if (!Exts.TryParseInt32Invariant(parts[1], out var team))
				{
					Log.Write("server", $"Invalid team: {s}");
					return false;
				}

				targetClient.Team = team;
				server.SyncLobbyClients();

				return true;
			}
		}

		static bool Handicap(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				var parts = s.Split(' ');
				var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseInt32Invariant(parts[0]));

				// Only the host can change other client's info
				if (targetClient.Index != client.Index && !client.IsAdmin)
					return true;

				// Map has disabled handicap changes
				if (server.LobbyInfo.Slots[targetClient.Slot].LockHandicap)
					return true;

				if (!Exts.TryParseInt32Invariant(parts[1], out var handicap))
				{
					Log.Write("server", $"Invalid handicap: {s}");
					return false;
				}

				// Handicaps may be set between 0 - 95% in steps of 5%
				var options = Enumerable.Range(0, 20).Select(i => 5 * i);
				if (!options.Contains(handicap))
				{
					Log.Write("server", $"Invalid handicap: {s}");
					return false;
				}

				targetClient.Handicap = handicap;
				server.SyncLobbyClients();

				return true;
			}
		}

		static bool ClearPlayerSpawn(S server, Connection conn, Session.Client client, string s)
		{
			var spawnPoint = Exts.ParseInt32Invariant(s);
			if (spawnPoint == 0)
				return true;

			var existingClient = server.LobbyInfo.Clients.FirstOrDefault(cc => cc.SpawnPoint == spawnPoint);
			if (client != existingClient && !client.IsAdmin)
			{
				server.SendFluentMessageTo(conn, AdminClearSpawn);
				return true;
			}

			// Clearing a selected spawn point removes the player
			if (existingClient != null)
			{
				// Prevent a map-defined lock spawn from being affected
				if (existingClient.Slot != null && server.LobbyInfo.Slots[existingClient.Slot].LockSpawn)
					return true;

				existingClient.SpawnPoint = 0;
				if (existingClient.State == Session.ClientState.Ready)
					existingClient.State = Session.ClientState.NotReady;

				server.SyncLobbyClients();
				return true;
			}

			// Clearing an empty spawn point prevents it from being selected
			// Clearing a disabled spawn restores it for use
			if (!server.LobbyInfo.DisabledSpawnPoints.Add(spawnPoint))
				server.LobbyInfo.DisabledSpawnPoints.Remove(spawnPoint);

			server.SyncLobbyInfo();
			return true;
		}

		static bool Spawn(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				var parts = s.Split(' ');
				var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseInt32Invariant(parts[0]));

				// Only the host can change other client's info
				if (targetClient.Index != client.Index && !client.IsAdmin)
					return true;

				// Spectators don't need a spawnpoint
				if (targetClient.Slot == null)
					return true;

				// Map has disabled spawn changes
				if (server.LobbyInfo.Slots[targetClient.Slot].LockSpawn)
					return true;

				if (!Exts.TryParseInt32Invariant(parts[1], out var spawnPoint)
					|| spawnPoint < 0 || spawnPoint > server.Map.SpawnPoints.Length)
				{
					Log.Write("server", $"Invalid spawn point: {parts[1]}");
					return true;
				}

				if (server.LobbyInfo.Clients.Any(cc => cc != client && (cc.SpawnPoint == spawnPoint) && (cc.SpawnPoint != 0)))
				{
					server.SendFluentMessageTo(conn, SpawnOccupied);
					return true;
				}

				// Check if any other slot has locked the requested spawn
				if (spawnPoint > 0)
				{
					var spawnLockedByAnotherSlot = server.LobbyInfo.Slots.Where(ss => ss.Value.LockSpawn).Any(ss =>
					{
						var pr = PlayerReferenceForSlot(server, ss.Value);
						return pr != null && pr.Spawn == spawnPoint;
					});

					if (spawnLockedByAnotherSlot)
					{
						server.SendFluentMessageTo(conn, SpawnLocked);
						return true;
					}
				}

				targetClient.SpawnPoint = spawnPoint;
				server.SyncLobbyClients();

				return true;
			}
		}

		static bool PlayerColor(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				var parts = s.Split(' ');
				var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseInt32Invariant(parts[0]));

				// Only the host can change other client's info
				if (targetClient.Index != client.Index && !client.IsAdmin)
					return true;

				// Spectator or map has disabled color changes
				if (targetClient.Slot == null || server.LobbyInfo.Slots[targetClient.Slot].LockColor)
					return true;

				// Validate if color is allowed and get an alternative it isn't
				var newColor = FieldLoader.GetValue<Color>("(value)", parts[1]);
				targetClient.Color = SanitizePlayerColor(server, newColor, targetClient.Index, conn);

				// Only update player's preferred color if new color is valid
				if (newColor == targetClient.Color)
					targetClient.PreferredColor = targetClient.Color;

				server.SyncLobbyClients();

				return true;
			}
		}

		static bool SyncLobby(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendFluentMessageTo(conn, AdminLobbyInfo);
					return true;
				}

				try
				{
					server.LobbyInfo = Session.Deserialize(s, nameof(SyncLobby));
					server.SyncLobbyInfo();
				}
				catch (Exception)
				{
					server.SendFluentMessageTo(conn, InvalidLobbyInfo);
				}

				return true;
			}
	}
}
