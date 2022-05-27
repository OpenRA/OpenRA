#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Server;
using OpenRA.Traits;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class LobbyCommands : ServerTrait, IInterpretCommand, INotifyServerStart, INotifyServerEmpty, IClientJoined
	{
		[TranslationReference]
		static readonly string CustomRules = "custom-rules";

		[TranslationReference]
		static readonly string OnlyHostStartGame = "only-only-host-start-game";

		[TranslationReference]
		static readonly string NoStartUntilRequiredSlotsFull = "no-start-until-required-slots-full";

		[TranslationReference]
		static readonly string TwoHumansRequired = "two-humans-required";

		[TranslationReference]
		static readonly string InsufficientEnabledSpawnPoints = "insufficient-enabled-spawnPoints";

		[TranslationReference("command")]
		static readonly string MalformedCommand = "malformed-command";

		[TranslationReference]
		static readonly string KickNone = "kick-none";

		[TranslationReference]
		static readonly string NoKickGameStarted = "no-kick-game-started";

		[TranslationReference("admin", "player")]
		static readonly string Kicked = "kicked";

		[TranslationReference("admin", "player")]
		static readonly string TempBan = "temp-ban";

		[TranslationReference]
		static readonly string NoTransferAdmin = "only-host-transfer-admin";

		[TranslationReference]
		static readonly string EmptySlot = "empty-slot";

		[TranslationReference("admin", "player")]
		static readonly string MoveSpectators = "move-spectators";

		[TranslationReference("player", "name")]
		static readonly string Nick = "nick";

		[TranslationReference]
		static readonly string StateUnchangedReady = "state-unchanged-ready";

		[TranslationReference("command")]
		static readonly string StateUnchangedGameStarted = "state-unchanged-game-started";

		[TranslationReference("faction")]
		static readonly string InvalidFactionSelected = "invalid-faction-selected";

		[TranslationReference("factions")]
		static readonly string SupportedFactions = "supported-factions";

		[TranslationReference]
		static readonly string RequiresHost = "requires-host";

		[TranslationReference]
		static readonly string InvalidBotSlot = "invalid-bot-slot";

		[TranslationReference]
		static readonly string InvalidBotType = "invalid-bot-type";

		[TranslationReference]
		static readonly string HostChangeMap = "only-host-change-map";

		[TranslationReference]
		static readonly string UnknownMap = "unknown-map";

		[TranslationReference]
		static readonly string SearchingMap = "searching-map";

		[TranslationReference]
		static readonly string NotAdmin = "only-host-change-configuration";

		[TranslationReference]
		static readonly string InvalidConfigurationCommand = "invalid-configuration-command";

		[TranslationReference("option")]
		static readonly string OptionLocked = "option-locked";

		[TranslationReference("player", "map")]
		static readonly string ChangedMap = "changed-map";

		[TranslationReference]
		static readonly string BotsDisabled = "bots-disabled";

		[TranslationReference("player", "name", "value")]
		static readonly string ValueChanged = "value-changed";

		[TranslationReference]
		static readonly string NoMoveSpectators = "only-host-move-spectators";

		[TranslationReference]
		static readonly string AdminOption = "admin-option";

		[TranslationReference("raw")]
		static readonly string NumberTeams = "number-teams";

		[TranslationReference]
		static readonly string AdminClearSpawn = "admin-clear-spawn";

		[TranslationReference]
		static readonly string SpawnOccupied = "spawn-occupied";

		[TranslationReference]
		static readonly string SpawnLocked = "spawn-locked";

		[TranslationReference]
		static readonly string AdminLobbyInfo = "admin-lobby-info";

		[TranslationReference]
		static readonly string InvalidLobbyInfo = "invalid-lobby-info";

		[TranslationReference]
		static readonly string AdminKick = "admin-kick";

		[TranslationReference]
		static readonly string SlotClosed = "slot-closed";

		[TranslationReference("player")]
		public static readonly string NewAdmin = "new-admin";

		[TranslationReference]
		static readonly string YouWereKicked = "you-were-kicked";

		readonly IDictionary<string, Func<S, Connection, Session.Client, string, bool>> commandHandlers = new Dictionary<string, Func<S, Connection, Session.Client, string, bool>>
		{
			{ "state", State },
			{ "startgame", StartGame },
			{ "slot", Slot },
			{ "allow_spectators", AllowSpectators },
			{ "spectate", Specate },
			{ "slot_close", SlotClose },
			{ "slot_open", SlotOpen },
			{ "slot_bot", SlotBot },
			{ "map", Map },
			{ "option", Option },
			{ "assignteams", AssignTeams },
			{ "kick", Kick },
			{ "make_admin", MakeAdmin },
			{ "make_spectator", MakeSpectator },
			{ "name", Name },
			{ "faction", Faction },
			{ "team", Team },
			{ "handicap", Handicap },
			{ "spawn", Spawn },
			{ "clear_spawn", ClearPlayerSpawn },
			{ "color", PlayerColor },
			{ "sync_lobby", SyncLobby }
		};

		static bool ValidateSlotCommand(S server, Connection conn, Session.Client client, string arg, bool requiresHost)
		{
			lock (server.LobbyInfo)
			{
				if (!server.LobbyInfo.Slots.ContainsKey(arg))
				{
					Log.Write("server", $"Invalid slot: {arg}");
					return false;
				}

				if (requiresHost && !client.IsAdmin)
				{
					server.SendLocalizedMessageTo(conn, RequiresHost);
					return false;
				}

				return true;
			}
		}

		public static bool ValidateCommand(S server, Connection conn, Session.Client client, string command)
		{
			lock (server.LobbyInfo)
			{
				// Kick command is always valid for the host
				if (command.StartsWith("kick "))
					return true;

				if (server.State == ServerState.GameStarted)
				{
					server.SendLocalizedMessageTo(conn, StateUnchangedGameStarted, Translation.Arguments("command", command));
					return false;
				}
				else if (client.State == Session.ClientState.Ready && !(command.StartsWith("state") || command == "startgame"))
				{
					server.SendLocalizedMessageTo(conn, StateUnchangedReady);
					return false;
				}

				return true;
			}
		}

		public bool InterpretCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (server == null || conn == null || client == null || !ValidateCommand(server, conn, client, cmd))
				return false;

			var cmdName = cmd.Split(' ').First();
			var cmdValue = cmd.Split(' ').Skip(1).JoinWith(" ");

			if (!commandHandlers.TryGetValue(cmdName, out var a))
				return false;

			return a(server, conn, client, cmdValue);
		}

		static void CheckAutoStart(S server)
		{
			lock (server.LobbyInfo)
			{
				var nonBotPlayers = server.LobbyInfo.NonBotPlayers;

				// Are all players and admin (could be spectating) ready?
				if (nonBotPlayers.Any(c => c.State != Session.ClientState.Ready) ||
					server.LobbyInfo.Clients.First(c => c.IsAdmin).State != Session.ClientState.Ready)
					return;

				// Does server have at least 2 human players?
				if (!server.LobbyInfo.GlobalSettings.EnableSingleplayer && nonBotPlayers.Count() < 2)
					return;

				// Are the map conditions satisfied?
				if (server.LobbyInfo.Slots.Any(sl => sl.Value.Required && server.LobbyInfo.ClientInSlot(sl.Key) == null))
					return;

				if (LobbyUtils.InsufficientEnabledSpawnPoints(server.Map, server.LobbyInfo))
					return;

				server.StartGame();
			}
		}

		static bool State(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!Enum<Session.ClientState>.TryParse(s, false, out var state))
				{
					server.SendLocalizedMessageTo(conn, MalformedCommand, Translation.Arguments("command", "state"));

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
					server.SendOrderTo(conn, "Message", OnlyHostStartGame);
					return true;
				}

				if (server.LobbyInfo.Slots.Any(sl => sl.Value.Required &&
													 server.LobbyInfo.ClientInSlot(sl.Key) == null))
				{
					server.SendOrderTo(conn, "Message", NoStartUntilRequiredSlotsFull);
					return true;
				}

				if (!server.LobbyInfo.GlobalSettings.EnableSingleplayer && server.LobbyInfo.NonBotPlayers.Count() < 2)
				{
					server.SendOrderTo(conn, "Message", TwoHumansRequired);
					return true;
				}

				if (LobbyUtils.InsufficientEnabledSpawnPoints(server.Map, server.LobbyInfo))
				{
					server.SendOrderTo(conn, "Message", InsufficientEnabledSpawnPoints);
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
				if (!server.LobbyInfo.Slots.ContainsKey(s))
				{
					Log.Write("server", $"Invalid slot: {s}");
					return false;
				}

				var slot = server.LobbyInfo.Slots[s];

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

				server.SendLocalizedMessageTo(conn, MalformedCommand, Translation.Arguments("command", "allow_spectate"));

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
					server.SendLocalizedMessageTo(conn, MalformedCommand, Translation.Arguments("command", "slot_bot"));
					return true;
				}

				if (!ValidateSlotCommand(server, conn, client, parts[0], true))
					return false;

				var slot = server.LobbyInfo.Slots[parts[0]];
				var bot = server.LobbyInfo.ClientInSlot(parts[0]);
				if (!Exts.TryParseIntegerInvariant(parts[1], out var controllerClientIndex))
				{
					Log.Write("server", $"Invalid bot controller client index: {parts[1]}");
					return false;
				}

				// Invalid slot
				if (bot != null && bot.Bot == null)
				{
					server.SendLocalizedMessageTo(conn, InvalidBotSlot);
					return true;
				}

				var botType = parts[2];
				var botInfo = server.Map.PlayerActorInfo.TraitInfos<IBotInfo>()
					.FirstOrDefault(b => b.Type == botType);

				if (botInfo == null)
				{
					server.SendLocalizedMessageTo(conn, InvalidBotType);
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
					var colorManager = server.ModData.DefaultRules.Actors[SystemActors.World].TraitInfo<ColorPickerManagerInfo>();
					var terrainColors = server.ModData.DefaultTerrainInfo[server.Map.TileSet].RestrictedPlayerColors;
					var playerColors = server.LobbyInfo.Clients.Select(c => c.Color)
						.Concat(server.Map.Players.Players.Values.Select(p => p.Color));
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
					server.SendLocalizedMessageTo(conn, HostChangeMap);
					return true;
				}

				var lastMap = server.LobbyInfo.GlobalSettings.Map;
				Action<MapPreview> selectMap = map =>
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
							c.State = Session.ClientState.Invalid;
							if (!selectableFactions.Contains(c.Faction))
								c.Faction = "Random";
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

						server.SyncLobbyInfo();

						server.SendLocalizedMessage(ChangedMap, Translation.Arguments("player", client.Name, "map", server.Map.Title));

						if ((server.LobbyInfo.GlobalSettings.MapStatus & Session.MapStatus.UnsafeCustomRules) != 0)
							server.SendLocalizedMessage(CustomRules);

						if (!server.LobbyInfo.GlobalSettings.EnableSingleplayer)
							server.SendLocalizedMessage(TwoHumansRequired);
						else if (server.Map.Players.Players.Where(p => p.Value.Playable).All(p => !p.Value.AllowBots))
							server.SendLocalizedMessage(BotsDisabled);

						var briefing = MissionBriefingOrDefault(server);
						if (briefing != null)
							server.SendMessage(briefing);
					}
				};

				Action queryFailed = () => server.SendLocalizedMessageTo(conn, UnknownMap);

				var m = server.ModData.MapCache[s];
				if (m.Status == MapStatus.Available || m.Status == MapStatus.DownloadAvailable)
					selectMap(m);
				else if (server.Settings.QueryMapRepository)
				{
					server.SendLocalizedMessageTo(conn, SearchingMap);
					var mapRepository = server.ModData.Manifest.Get<WebServices>().MapRepository;
					var reported = false;
					server.ModData.MapCache.QueryRemoteMapDetails(mapRepository, new[] { s }, selectMap, _ =>
					{
						if (!reported)
							queryFailed();

						reported = true;
					});
				}
				else
					queryFailed();

				return true;
			}
		}

		static bool Option(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendLocalizedMessageTo(conn, NotAdmin);
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
					!option.Values.ContainsKey(split[1]))
				{
					server.SendLocalizedMessageTo(conn, InvalidConfigurationCommand);
					return true;
				}

				if (option.IsLocked)
				{
					server.SendLocalizedMessageTo(conn, OptionLocked, Translation.Arguments("option", option.Name));
					return true;
				}

				var oo = server.LobbyInfo.GlobalSettings.LobbyOptions[option.Id];
				if (oo.Value == split[1])
					return true;

				oo.Value = oo.PreferredValue = split[1];

				server.SyncLobbyGlobalSettings();
				server.SendLocalizedMessage(ValueChanged, Translation.Arguments("player", client.Name, "name", option.Name, "value", option.Label(split[1])));

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
					server.SendLocalizedMessageTo(conn, AdminOption);
					return true;
				}

				if (!Exts.TryParseIntegerInvariant(raw, out var teamCount))
				{
					server.SendLocalizedMessageTo(conn, NumberTeams, Translation.Arguments("raw", raw));
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
		}

		static bool Kick(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendLocalizedMessageTo(conn, AdminKick);
					return true;
				}

				var split = s.Split(' ');
				if (split.Length < 2)
				{
					server.SendLocalizedMessageTo(conn, MalformedCommand, Translation.Arguments("command", "kick"));
					return true;
				}

				Exts.TryParseIntegerInvariant(split[0], out var kickClientID);

				var kickConn = server.Conns.SingleOrDefault(c => server.GetClient(c)?.Index == kickClientID);
				if (kickConn == null)
				{
					server.SendLocalizedMessageTo(conn, KickNone);
					return true;
				}

				var kickClient = server.GetClient(kickConn);
				if (server.State == ServerState.GameStarted && !kickClient.IsObserver)
				{
					server.SendLocalizedMessageTo(conn, NoKickGameStarted);
					return true;
				}

				Log.Write("server", $"Kicking client {kickClientID}.");
				server.SendLocalizedMessage(Kicked, Translation.Arguments("admin", client.Name, "client", kickClient.Name));
				server.SendOrderTo(kickConn, "ServerError", YouWereKicked);
				server.DropClient(kickConn);

				if (bool.TryParse(split[1], out var tempBan) && tempBan)
				{
					Log.Write("server", $"Temporarily banning client {kickClientID} ({kickClient.IPAddress}).");
					server.SendLocalizedMessage(TempBan, Translation.Arguments("admin", client.Name, "client", kickClient.Name));
					server.TempBans.Add(kickClient.IPAddress);
				}

				server.SyncLobbyClients();
				server.SyncLobbySlots();

				return true;
			}
		}

		static bool MakeAdmin(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
			{
				if (!client.IsAdmin)
				{
					server.SendLocalizedMessageTo(conn, NoTransferAdmin);
					return true;
				}

				Exts.TryParseIntegerInvariant(s, out var newAdminId);
				var newAdminConn = server.Conns.SingleOrDefault(c => server.GetClient(c)?.Index == newAdminId);

				if (newAdminConn == null)
				{
					server.SendLocalizedMessageTo(conn, EmptySlot);
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

				server.SendLocalizedMessage(NewAdmin, Translation.Arguments("player", newAdminClient.Name));
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
					server.SendLocalizedMessageTo(conn, NoMoveSpectators);
					return true;
				}

				Exts.TryParseIntegerInvariant(s, out var targetId);
				var targetConn = server.Conns.SingleOrDefault(c => server.GetClient(c)?.Index == targetId);
				if (targetConn == null)
				{
					server.SendLocalizedMessageTo(conn, EmptySlot);
					return true;
				}

				var targetClient = server.GetClient(targetConn);
				targetClient.Slot = null;
				targetClient.SpawnPoint = 0;
				targetClient.Team = 0;
				targetClient.Handicap = 0;
				targetClient.Color = Color.White;
				targetClient.State = Session.ClientState.NotReady;
				server.SendLocalizedMessage(MoveSpectators, Translation.Arguments("admin", client.Name, "player", targetClient.Name));
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
				server.SendLocalizedMessage(Nick, Translation.Arguments("player", client.Name, "name", sanitizedName));
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
				var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseIntegerInvariant(parts[0]));

				// Only the host can change other client's info
				if (targetClient.Index != client.Index && !client.IsAdmin)
					return true;

				// Map has disabled faction changes
				if (server.LobbyInfo.Slots[targetClient.Slot].LockFaction)
					return true;

				var factions = server.Map.WorldActorInfo.TraitInfos<FactionInfo>()
					.Where(f => f.Selectable).Select(f => f.InternalName);

				var faction = parts[1];
				if (!factions.Contains(faction))
				{
					server.SendLocalizedMessageTo(conn, InvalidFactionSelected, Translation.Arguments("faction", faction));
					server.SendLocalizedMessageTo(conn, SupportedFactions, Translation.Arguments("factions", factions.JoinWith(", ")));
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
				var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseIntegerInvariant(parts[0]));

				// Only the host can change other client's info
				if (targetClient.Index != client.Index && !client.IsAdmin)
					return true;

				// Map has disabled team changes
				if (server.LobbyInfo.Slots[targetClient.Slot].LockTeam)
					return true;

				if (!Exts.TryParseIntegerInvariant(parts[1], out var team))
				{
					Log.Write("server", "Invalid team: {0}", s);
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
				var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseIntegerInvariant(parts[0]));

				// Only the host can change other client's info
				if (targetClient.Index != client.Index && !client.IsAdmin)
					return true;

				// Map has disabled handicap changes
				if (server.LobbyInfo.Slots[targetClient.Slot].LockHandicap)
					return true;

				if (!Exts.TryParseIntegerInvariant(parts[1], out var handicap))
				{
					Log.Write("server", "Invalid handicap: {0}", s);
					return false;
				}

				// Handicaps may be set between 0 - 95% in steps of 5%
				var options = Enumerable.Range(0, 20).Select(i => 5 * i);
				if (!options.Contains(handicap))
				{
					Log.Write("server", "Invalid handicap: {0}", s);
					return false;
				}

				targetClient.Handicap = handicap;
				server.SyncLobbyClients();

				return true;
			}
		}

		static bool ClearPlayerSpawn(S server, Connection conn, Session.Client client, string s)
		{
			var spawnPoint = Exts.ParseIntegerInvariant(s);
			if (spawnPoint == 0)
				return true;

			var existingClient = server.LobbyInfo.Clients.FirstOrDefault(cc => cc.SpawnPoint == spawnPoint);
			if (client != existingClient && !client.IsAdmin)
			{
				server.SendLocalizedMessageTo(conn, AdminClearSpawn);
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
			if (!server.LobbyInfo.DisabledSpawnPoints.Contains(spawnPoint))
				server.LobbyInfo.DisabledSpawnPoints.Add(spawnPoint);
			else
				server.LobbyInfo.DisabledSpawnPoints.Remove(spawnPoint);

			server.SyncLobbyInfo();
			return true;
		}

		static bool Spawn(S server, Connection conn, Session.Client client, string s)
		{
			lock (server.LobbyInfo)
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

				if (!Exts.TryParseIntegerInvariant(parts[1], out var spawnPoint)
				    || spawnPoint < 0 || spawnPoint > server.Map.SpawnPoints.Length)
				{
					Log.Write("server", $"Invalid spawn point: {parts[1]}");
					return true;
				}

				if (server.LobbyInfo.Clients.Where(cc => cc != client).Any(cc => (cc.SpawnPoint == spawnPoint) && (cc.SpawnPoint != 0)))
				{
					server.SendLocalizedMessageTo(conn, SpawnOccupied);
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
						server.SendLocalizedMessageTo(conn, SpawnLocked);
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
				var targetClient = server.LobbyInfo.ClientWithIndex(Exts.ParseIntegerInvariant(parts[0]));

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
					server.SendLocalizedMessageTo(conn, AdminLobbyInfo);
					return true;
				}

				try
				{
					server.LobbyInfo = Session.Deserialize(s);
					server.SyncLobbyInfo();
				}
				catch (Exception)
				{
					server.SendLocalizedMessageTo(conn, InvalidLobbyInfo);
				}

				return true;
			}
		}

		public void ServerStarted(S server)
		{
			lock (server.LobbyInfo)
			{
				// Remote maps are not supported for the initial map
				var uid = server.LobbyInfo.GlobalSettings.Map;
				server.Map = server.ModData.MapCache[uid];
				if (server.Map.Status != MapStatus.Available)
					throw new InvalidOperationException($"Map {uid} not found");

				server.LobbyInfo.Slots = server.Map.Players.Players
					.Select(p => MakeSlotFromPlayerReference(p.Value))
					.Where(s => s != null)
					.ToDictionary(s => s.PlayerReference, s => s);

				LoadMapSettings(server, server.LobbyInfo.GlobalSettings, server.Map);
			}
		}

		static Session.Slot MakeSlotFromPlayerReference(PlayerReference pr)
		{
			if (!pr.Playable)
				return null;

			return new Session.Slot
			{
				PlayerReference = pr.Name,
				Closed = false,
				AllowBots = pr.AllowBots,
				LockFaction = pr.LockFaction,
				LockColor = pr.LockColor,
				LockTeam = pr.LockTeam,
				LockHandicap = pr.LockHandicap,
				LockSpawn = pr.LockSpawn,
				Required = pr.Required,
			};
		}

		public static void LoadMapSettings(S server, Session.Global gs, MapPreview map)
		{
			lock (server.LobbyInfo)
			{
				var options = map.PlayerActorInfo.TraitInfos<ILobbyOptions>()
					.Concat(map.WorldActorInfo.TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(map));

				foreach (var o in options)
				{
					var value = o.DefaultValue;
					var preferredValue = o.DefaultValue;
					if (gs.LobbyOptions.TryGetValue(o.Id, out var state))
					{
						// Propagate old state on map change
						if (!o.IsLocked)
						{
							if (o.Values.Keys.Contains(state.PreferredValue))
								value = state.PreferredValue;
							else if (o.Values.Keys.Contains(state.Value))
								value = state.Value;
						}

						preferredValue = state.PreferredValue;
					}
					else
						state = new Session.LobbyOptionState();

					state.IsLocked = o.IsLocked;
					state.Value = value;
					state.PreferredValue = preferredValue;
					gs.LobbyOptions[o.Id] = state;
				}
			}
		}

		static Color SanitizePlayerColor(S server, Color askedColor, int playerIndex, Connection connectionToEcho = null)
		{
			lock (server.LobbyInfo)
			{
				var colorManager = server.ModData.DefaultRules.Actors[SystemActors.World].TraitInfo<ColorPickerManagerInfo>();
				var askColor = askedColor;

				Action<string> onError = message =>
				{
					if (connectionToEcho != null && message != null)
						server.SendLocalizedMessageTo(connectionToEcho, message);
				};

				var terrainColors = server.ModData.DefaultTerrainInfo[server.Map.TileSet].RestrictedPlayerColors;
				var playerColors = server.LobbyInfo.Clients.Where(c => c.Index != playerIndex).Select(c => c.Color)
					.Concat(server.Map.Players.Players.Values.Select(p => p.Color)).ToList();

				return colorManager.MakeValid(askColor, server.Random, terrainColors, playerColors, onError);
			}
		}

		static string MissionBriefingOrDefault(S server)
		{
			var missionData = server.Map.WorldActorInfo.TraitInfoOrDefault<MissionDataInfo>();
			if (missionData != null && !string.IsNullOrEmpty(missionData.Briefing))
				return missionData.Briefing.Replace("\\n", "\n");

			return null;
		}

		public void ClientJoined(S server, Connection conn)
		{
			lock (server.LobbyInfo)
			{
				var client = server.GetClient(conn);

				// Validate whether color is allowed and get an alternative if it isn't
				if (client.Slot != null && !server.LobbyInfo.Slots[client.Slot].LockColor)
					client.Color = SanitizePlayerColor(server, client.Color, client.Index);

				// Report any custom map details
				// HACK: this isn't the best place for this to live, but if we move it somewhere else
				// then we need a larger hack to hook the map change event.
				var briefing = MissionBriefingOrDefault(server);
				if (briefing != null)
					server.SendOrderTo(conn, "Message", briefing);
			}
		}

		void INotifyServerEmpty.ServerEmpty(S server)
		{
			lock (server.LobbyInfo)
			{
				// Expire any temporary bans
				server.TempBans.Clear();

				// Re-enable spectators
				server.LobbyInfo.GlobalSettings.AllowSpectators = true;

				// Reset player slots
				server.LobbyInfo.Slots = server.Map.Players.Players
					.Select(p => MakeSlotFromPlayerReference(p.Value))
					.Where(ss => ss != null)
					.ToDictionary(ss => ss.PlayerReference, ss => ss);
			}
		}

		public static PlayerReference PlayerReferenceForSlot(S server, Session.Slot slot)
		{
			if (slot == null)
				return null;

			return server.Map.Players.Players[slot.PlayerReference];
		}
	}
}
