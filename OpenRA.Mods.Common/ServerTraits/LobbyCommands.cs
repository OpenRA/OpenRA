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
	public partial class LobbyCommands : ServerTrait, IInterpretCommand, INotifyServerStart, INotifyServerEmpty, IClientJoined, OpenRA.Server.ITick
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

		[FluentReference("player", "map")]
		const string ChangedMap = "notification-changed-map";

		[FluentReference]
		const string MapBotsDisabled = "notification-map-bots-disabled";

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

		static void InitializeMapPool(S server)
		{
			if (server.Type != ServerType.Dedicated)
				return;

			var mapCache = server.ModData.MapCache;
			if (server.Settings.MapPool.Count > 0)
				server.MapPool = server.Settings.MapPool;
			else if (!server.Settings.QueryMapRepository)
				server.MapPool = mapCache
					.Where(p => p.Status == MapStatus.Available && p.Visibility.HasFlag(MapVisibility.Lobby))
					.Select(p => p.Uid)
					.ToFrozenSet();
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
				var mapRepository = server.ModData.GetOrCreate<WebServices>().MapRepository;
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
