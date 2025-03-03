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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Server;
using OpenRA.Support;
using OpenRA.Traits;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class LobbyCommands : ServerTrait, IInterpretCommand, INotifyServerStart, INotifyServerEmpty, IClientJoined, OpenRA.Server.ITick
	{
		[FluentReference]
		const string CustomRules = "notification-custom-rules";

		[FluentReference]
		const string OnlyHostStartGame = "notification-admin-start-game";

		[FluentReference]
		const string NoStartUntilRequiredSlotsFull = "notification-no-start-until-required-slots-full";

		[FluentReference]
		const string NoStartWithoutPlayers = "notification-no-start-without-players";

		[FluentReference]
		const string TwoHumansRequired = "notification-two-humans-required";

		[FluentReference]
		const string InsufficientEnabledSpawnPoints = "notification-insufficient-enabled-spawn-points";

		[FluentReference("command")]
		const string MalformedCommand = "notification-malformed-command";

		[FluentReference]
		const string KickNone = "notification-kick-none";

		[FluentReference]
		const string NoKickSelf = "notification-kick-self";

		[FluentReference]
		const string NoKickGameStarted = "notification-no-kick-game-started";

		[FluentReference("admin", "player")]
		const string AdminKicked = "notification-admin-kicked";

		[FluentReference("player")]
		const string Kicked = "notification-kicked";

		[FluentReference("admin", "player")]
		const string TempBan = "notification-temp-ban";

		[FluentReference]
		const string NoTransferAdmin = "notification-admin-transfer-admin";

		[FluentReference]
		const string EmptySlot = "notification-empty-slot";

		[FluentReference("admin", "player")]
		const string MoveSpectators = "notification-move-spectators";

		[FluentReference("player", "name")]
		const string Nick = "notification-nick-changed";

		[FluentReference]
		const string StateUnchangedReady = "notification-state-unchanged-ready";

		[FluentReference("command")]
		const string StateUnchangedGameStarted = "notification-state-unchanged-game-started";

		[FluentReference("faction")]
		const string InvalidFactionSelected = "notification-invalid-faction-selected";

		[FluentReference]
		const string RequiresHost = "notification-requires-host";

		[FluentReference]
		const string InvalidBotSlot = "notification-invalid-bot-slot";

		[FluentReference]
		const string InvalidBotType = "notification-invalid-bot-type";

		[FluentReference]
		const string HostChangeMap = "notification-admin-change-map";

		[FluentReference]
		const string UnknownMap = "notification-unknown-map";

		[FluentReference]
		const string SearchingMap = "notification-searching-map";

		[FluentReference]
		const string NotAdmin = "notification-admin-change-configuration";

		[FluentReference]
		const string InvalidConfigurationCommand = "notification-invalid-configuration-command";

		[FluentReference("option")]
		const string OptionLocked = "notification-option-locked";

		[FluentReference("player", "map")]
		const string ChangedMap = "notification-changed-map";

		[FluentReference]
		const string MapBotsDisabled = "notification-map-bots-disabled";

		[FluentReference("player", "name", "value")]
		const string ValueChanged = "notification-option-changed";

		[FluentReference]
		const string NoMoveSpectators = "notification-admin-move-spectators";

		[FluentReference]
		const string AdminOption = "notification-admin-option";

		[FluentReference("raw")]
		const string NumberTeams = "notification-error-number-teams";

		[FluentReference]
		const string AdminClearSpawn = "notification-admin-clear-spawn";

		[FluentReference]
		const string SpawnOccupied = "notification-spawn-occupied";

		[FluentReference]
		const string SpawnLocked = "notification-spawn-locked";

		[FluentReference]
		const string AdminLobbyInfo = "notification-admin-lobby-info";

		[FluentReference]
		const string InvalidLobbyInfo = "notification-invalid-lobby-info";

		[FluentReference]
		const string AdminKick = "notification-admin-kick";

		[FluentReference]
		const string SlotClosed = "notification-slot-closed";

		[FluentReference("player")]
		const string NewAdmin = "notification-new-admin";

		[FluentReference]
		const string YouWereKicked = "notification-you-were-kicked";

		[FluentReference]
		const string VoteKickDisabled = "notification-vote-kick-disabled";

		readonly IDictionary<string, Func<S, Connection, Session.Client, string, bool>> commandHandlers =
			new Dictionary<string, Func<S, Connection, Session.Client, string, bool>>
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
				{ "reset_options", ResetOptions },
				{ "assignteams", AssignTeams },
				{ "kick", Kick },
				{ "vote_kick", VoteKick },
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
					server.SendFluentMessageTo(conn, RequiresHost);
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
				if (command.StartsWith("kick ", StringComparison.Ordinal) || command.StartsWith("vote_kick ", StringComparison.Ordinal))
					return true;

				if (server.State == ServerState.GameStarted)
				{
					server.SendFluentMessageTo(conn, StateUnchangedGameStarted, ["command", command]);
					return false;
				}
				else if (client.State == Session.ClientState.Ready && !(command.StartsWith("state", StringComparison.Ordinal) || command == "startgame"))
				{
					server.SendFluentMessageTo(conn, StateUnchangedReady);
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

				// Don't start without any players
				if (server.LobbyInfo.Slots.All(sl => server.LobbyInfo.ClientInSlot(sl.Key) == null))
					return;

				// Does the host have the map installed?
				if (server.Type != ServerType.Dedicated && server.ModData.MapCache[server.Map.Uid].Status != MapStatus.Available)
				{
					// Client 0 will always be the Host
					// In some cases client 0 doesn't exist, so we untick all players
					var host = server.LobbyInfo.Clients.FirstOrDefault(c => c.Index == 0);
					if (host != null)
						host.State = Session.ClientState.NotReady;
					else
						foreach (var client in server.LobbyInfo.Clients)
							client.State = Session.ClientState.NotReady;

					server.SyncLobbyClients();
					return;
				}

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

				if (server.LobbyInfo.Slots.All(sl => server.LobbyInfo.ClientInSlot(sl.Key) == null))
				{
					server.SendOrderTo(conn, "Message", NoStartWithoutPlayers);
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

						server.SyncLobbyInfo();

						server.SendFluentMessage(ChangedMap, "player", client.Name, "map", server.Map.Title);

						if ((server.LobbyInfo.GlobalSettings.MapStatus & Session.MapStatus.UnsafeCustomRules) != 0)
							server.SendFluentMessage(CustomRules);

						if (!server.LobbyInfo.GlobalSettings.EnableSingleplayer)
							server.SendFluentMessage(TwoHumansRequired);
						else if (server.Map.Players.Players.Where(p => p.Value.Playable).All(p => !p.Value.AllowBots))
							server.SendFluentMessage(MapBotsDisabled);

						var briefing = MissionBriefingOrDefault(server);
						if (briefing != null)
							server.SendMessage(briefing);
					}
				}

				var m = server.ModData.MapCache[s];
				if (m.Status == MapStatus.Available || m.Status == MapStatus.DownloadAvailable)
					SelectMap(m);
				else if (server.Settings.QueryMapRepository)
				{
					server.SendFluentMessageTo(conn, SearchingMap);
					var mapRepository = server.ModData.Manifest.Get<WebServices>().MapRepository;
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
					!option.Values.ContainsKey(split[1]))
				{
					server.SendFluentMessageTo(conn, InvalidConfigurationCommand);
					return true;
				}

				if (option.IsLocked)
				{
					server.SendFluentMessageTo(conn, OptionLocked, ["option", option.Name]);
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
				server.SendFluentMessage(ValueChanged, "player", client.Name, "name", option.Name, "value", option.Label(split[1]));

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

				var allOptions = server.Map.PlayerActorInfo.TraitInfos<ILobbyOptions>()
					.Concat(server.Map.WorldActorInfo.TraitInfos<ILobbyOptions>())
					.SelectMany(t => t.LobbyOptions(server.Map));

				var options = new Dictionary<string, Session.LobbyOptionState>();
				foreach (var o in allOptions)
				{
					if (o.DefaultValue != server.LobbyInfo.GlobalSettings.LobbyOptions[o.Id].Value)
						server.SendFluentMessage(ValueChanged,
							"player", client.Name,
							"name", o.Name,
							"value", o.Label(o.DefaultValue));

					options[o.Id] = new Session.LobbyOptionState
					{
						IsLocked = o.IsLocked,
						Value = o.DefaultValue,
						PreferredValue = o.DefaultValue
					};
				}

				server.LobbyInfo.GlobalSettings.LobbyOptions = options;
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

		static void InitializeMapPool(S server)
		{
			if (server.Type != ServerType.Dedicated)
				return;

			var mapCache = server.ModData.MapCache;
			if (server.Settings.MapPool.Length > 0)
				server.MapPool = server.Settings.MapPool.ToHashSet();
			else if (!server.Settings.QueryMapRepository)
				server.MapPool = mapCache
					.Where(p => p.Status == MapStatus.Available && p.Visibility.HasFlag(MapVisibility.Lobby))
					.Select(p => p.Uid)
					.ToHashSet();
			else
				return;

			var unknownMaps = server.MapPool.Where(server.MapIsUnknown).ToList();
			if (unknownMaps.Count == 0)
				return;

			if (server.Settings.QueryMapRepository)
			{
				Log.Write("server", $"Querying Resource Center for information on {unknownMaps.Count} maps...");

				// Query any missing maps and wait up to 10 seconds for a response
				// Maps that have not resolved will not be valid for the initial map choice
				var mapRepository = server.ModData.Manifest.Get<WebServices>().MapRepository;
				mapCache.QueryRemoteMapDetails(mapRepository, unknownMaps);

				var searchingMaps = server.MapPool.Where(uid => mapCache[uid].Status == MapStatus.Searching);
				var stopwatch = Stopwatch.StartNew();

				// Each time we check, some map statuses may have updated.
#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
				while (searchingMaps.Any() && stopwatch.ElapsedMilliseconds < 10000)
					Thread.Sleep(100);
#pragma warning restore CA1851
			}

			var stillUnknownMaps = server.MapPool.Where(server.MapIsUnknown).ToList();
			if (stillUnknownMaps.Count != 0)
				Log.Write("server", "Failed to resolve maps: " + stillUnknownMaps.JoinWith(", "));
		}

		static string ChooseInitialMap(S server)
		{
			if (server.MapIsKnown(server.Settings.Map))
				return server.Settings.Map;

			if (server.MapPool == null)
				return server.ModData.MapCache.ChooseInitialMap(server.Settings.Map, new MersenneTwister());

			return server.MapPool
				.Where(server.MapIsKnown)
				.RandomOrDefault(new MersenneTwister());
		}

		public void ServerStarted(S server)
		{
			lock (server.LobbyInfo)
			{
				InitializeMapPool(server);

				var uid = ChooseInitialMap(server);
				if (string.IsNullOrEmpty(uid))
					throw new InvalidOperationException("Unable to resolve a valid initial map");

				server.LobbyInfo.GlobalSettings.Map = server.Settings.Map = uid;
				server.Map = server.ModData.MapCache[uid];
				server.LobbyInfo.GlobalSettings.MapStatus = server.MapStatusCache[server.Map];
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

		public static Color SanitizePlayerColor(S server, Color askedColor, int playerIndex, Connection connectionToEcho = null)
		{
			lock (server.LobbyInfo)
			{
				var colorManager = server.ModData.DefaultRules.Actors[SystemActors.World].TraitInfo<IColorPickerManagerInfo>();
				var askColor = askedColor;

				void OnError(string message)
				{
					if (connectionToEcho != null && message != null)
						server.SendFluentMessageTo(connectionToEcho, message);
				}

				var terrainColors = server.ModData.DefaultTerrainInfo[server.Map.TileSet].RestrictedPlayerColors.ToList();
				var playerColors = server.LobbyInfo.Clients.Where(c => c.Index != playerIndex).Select(c => c.Color)
					.Concat(server.Map.Players.Players.Values.Select(p => p.Color)).ToList();

				return colorManager.MakeValid(askColor, server.Random, terrainColors, playerColors, OnError);
			}
		}

		public static string SanitizePlayerFaction(S server, string askedFaction, IEnumerable<string> validFactions)
		{
			return !validFactions.Contains(askedFaction) ? "Random" : askedFaction;
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
				if (server.MapPool != null)
					server.SendOrderTo(conn, "SyncMapPool", FieldSaver.FormatValue(server.MapPool));

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
