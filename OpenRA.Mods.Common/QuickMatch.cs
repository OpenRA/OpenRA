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
using System.Linq;
using OpenRA.Network;
using OpenRA.Support;

namespace OpenRA.Mods.Common
{
	/// <summary>
	/// Finds a quick-match server on the master list, joins it as an ordinary
	/// lobby client, and readies up as the server's launch epoch closes.
	/// </summary>
	public static class QuickMatch
	{
		// ---- search state machine ----

		public enum Status
		{
			Idle,
			Searching,
			Waiting,
			Starting,
			NoServers
		}

		// ---- knobs ----

		const int RecheckSeconds = 5;

		internal const int ReadyLeadMs = 1000;

		// ---- delegate callbacks callers should replace ----

		/// <summary>Called once this client is in a quick-match lobby.</summary>
		public static Action LobbyJoined { get; set; } = () => { };

		/// <summary>Called before leaving one quick-match lobby for a fuller
		/// one, so an interface showing the old one can put it away.</summary>
		public static Action LobbyLeaving { get; set; } = () => { };

		/// <summary>Called as the launch epoch opens (0) and at each ten-second
		/// step left on it (5 down to 1).</summary>
		public static Action<int> CountdownStep { get; set; } = _ => { };

		/// <summary>Called once every player is ready and the server is about to
		/// start.</summary>
		public static Action MatchStarting { get; set; } = () => { };

		// ---- state ----

		public static Status State { get; private set; }

		static OrderManager orderManager;
		static DateTime lastRecheck;
		// Epoch is the protocol's epoch number.
		// For example if Alice waits for an opponent, Bob joins and server sends QMCountdown with ExtraData.
		// If Bob leaves, epoch closes by incrementing its number.
		static int epoch;
		static bool readySent;
		static bool readyPending;
		static int downloadErrors; // failed fetches of this lobby's map: retry once, then leave
		static int mapQueries; // visits to an unresolved map: two asks, then leave

		// ---- the launch epoch, as it arrives from the server ----

		static QuickMatch()
		{
			UnitOrders.QuickMatchCountdownReceived += order =>
			{
				if (order.TargetString == "abort")
				{
					// Quick! Prevent RunAfterDelay Ready()
					epoch++;
					// Quick! Prevent FlushReady
					readyPending = false;

					if (State == Status.Starting)
						State = Status.Waiting;

					EnsureNotReady();
				}
				else if (order.TargetString == "start")
				{
					var thisEpoch = ++epoch;
					// Ready ahead of time; a roundtrip for an RTS is assumed to be well below ReadyLeadMs
					var delay = (int)Math.Max(0, (long)order.ExtraData - ReadyLeadMs);
					Game.RunAfterDelay(delay, () =>
					{
						if (epoch == thisEpoch)
							Ready();
					});
					CountdownStep(0);
				}
				else if (Exts.TryParseInt32Invariant(order.TargetString, out var step) && step > 0 && step <= 5)
					CountdownStep(step);
			};
		}

		// ---- the hunt ----

		/// <summary>Advertises under the quick-match name prefix, and has room
		/// for another player right now.</summary>
		/// <remarks>
		/// Author's intention is that a prefix-based eligibility criterion
		/// will eventually be replaced by an active challenge-response protocol
		/// with a person-in-the-middle attack prevention via bookkeeping player
		/// allow list server-side, as follows:
		/// <code><![CDATA[
		///   Alice                master.openra.net            Candidate
		///     |                          |                        |
		///     | "What are blessed        |                        |
		///     |  servers?"          ---->|                        |
		///     |                          |                        |
		///     |<---- A, B, Candidate, D  |                        |
		///     |                          |                        |
		///     | Sig(Self, Chal) --------------- Msg ------------->|
		///     |                          |                Verify(Msg)
		///     |                          |             Allow(Alice, Chal2)
		///     |                          |                Sig(Self, Chal)
		///     |<------- Rsp: Sig(Self, Chal), Chal2 --------------|
		///     | Verify(Rsp)              |                        |
		///     |                          |                        |
		///     | Join, Msg2: Sig(Self, Chal2) -------------------->|
		///     |                          |               Verify(Msg2)
		///     |                          |                Seat(Alice)
		/// ]]></code>
		/// </remarks>
		static bool Joinable(GameServer server)
		{
			return server.Name != null && server.Name.StartsWith("(QM) ", StringComparison.Ordinal) &&
				server.IsJoinable && !server.Protected && server.Players < server.MaxPlayers;
		}

		public static void Start()
		{
			if (State != Status.Idle && State != Status.NoServers)
				return;

			readyPending = readySent = false;
			downloadErrors = mapQueries = 0;
			lastRecheck = DateTime.UtcNow;
			Search();
			Game.RunAfterDelay(RecheckSeconds * 1000, Tick);
		}

		public static void Cancel()
		{
			Game.LobbyInfoChanged -= LobbyInfoChanged;
			State = Status.Idle;

			if (orderManager != null)
			{
				orderManager = null;
				Game.Disconnect();
			}
		}

		/// <remarks>Happens once per several seconds.</remarks>
		static void Tick()
		{
			if (State == Status.Idle || State == Status.NoServers)
				return;

			Recheck();
			EnsureMapInstalled();
			Game.RunAfterDelay(RecheckSeconds * 1000, Tick);
		}

		static void Search()
		{
			State = Status.Searching;
			Game.LobbyInfoChanged -= LobbyInfoChanged;
			Game.LobbyInfoChanged += LobbyInfoChanged;

			MasterServerList.Fetch(Game.ModData, servers =>
			{
				if (State != Status.Searching)
					return;

				var joinable = servers.Where(Joinable).ToList();
				var fullest = joinable.Count > 0 ? joinable.Max(s => s.Players) : 0;
				var server = joinable.Where(s => s.Players == fullest)
					.RandomOrDefault(new MersenneTwister());

				if (server == null)
				{
					State = Status.NoServers;
					return;
				}

				Join(server);
			});
		}

		static void Join(GameServer server)
		{
			readyPending = readySent = false;
			downloadErrors = mapQueries = 0;
			State = Status.Searching;
			lastRecheck = DateTime.UtcNow;

			if (orderManager != null)
			{
				LobbyLeaving();
				orderManager = null;
				Game.Disconnect();
			}

			var address = server.Address.Split(':');
			orderManager = Game.JoinServer(new ConnectionTarget(address[0], Exts.ParseInt32Invariant(address[1])), "");
		}

		/// <summary>
		/// Waiting alone is prone to racing! 
		///
		/// Look again once in a while, deterministically moving players down the ordinality.
		/// </summary>
		static void Recheck()
		{
			var om = orderManager;
			if (om == null)
				return;

			if (om.Connection is NetworkConnection nc && nc.ConnectionState == ConnectionState.NotConnected)
			{
				orderManager = null;
				Game.Disconnect();
				Search();
				return;
			}

			var here = om.LobbyInfo.GlobalSettings.ServerName;
			if (State != Status.Waiting || here == null || om.LobbyInfo.NonBotPlayers.Count() > 1)
				return;

			if (DateTime.UtcNow - lastRecheck < TimeSpan.FromSeconds(RecheckSeconds))
				return;

			lastRecheck = DateTime.UtcNow;
			MasterServerList.Fetch(Game.ModData, servers =>
			{
				var still = orderManager;
				if (State != Status.Waiting || still == null ||
					still.LobbyInfo.GlobalSettings.ServerName != here || still.LobbyInfo.NonBotPlayers.Count() > 1)
					return;

				var target = servers
					.Where(s => Joinable(s) && s.Players > 0 && string.CompareOrdinal(s.Name, here) < 0)
					.OrderBy(s => s.Name, StringComparer.Ordinal)
					.FirstOrDefault();

				if (target != null)
					Join(target);
			});
		}

		// ---- sitting in a lobby ----

		static void LobbyInfoChanged()
		{
			var om = orderManager;
			var client = om?.LocalClient;
			if (client == null)
				return;

			readySent = client.IsReady;
			if (State == Status.Searching)
			{
				State = Status.Waiting;
				LobbyJoined();
			}

			var players = om.LobbyInfo.NonBotPlayers.ToList();
			if (State == Status.Waiting && players.Count > 1 && players.All(c => c.IsReady))
			{
				State = Status.Starting;
				MatchStarting();
			}

			EnsureMapInstalled();
		}

		/// <summary>This lobby cannot feed us its map: let go of the seat and hunt elsewhere.</summary>
		static void GiveUpLobby()
		{
			Log.Write("debug", "Quick match: this lobby cannot feed us its map; hunting elsewhere.");
			LobbyLeaving();
			orderManager = null;
			Game.Disconnect();
			Search();
		}

		/// <remarks>We need to pump map.Install because of potential races over shared MapCache in a given mod.</remarks>
		static void EnsureMapInstalled()
		{
			var om = orderManager;
			if (State != Status.Waiting || om == null)
				return;

			var repository = Game.ModData.GetOrCreate<WebServices>().MapRepository;
			var map = Game.ModData.MapCache[om.LobbyInfo.GlobalSettings.Map];

			if (map.Status == MapStatus.Available)
				FlushPendingReady();

			else if (map.Status == MapStatus.DownloadAvailable || map.Status == MapStatus.DownloadError)
				// Has a side effect flicking MapStatus to Downloading.
				// We still need a pump because a slower-than-ourselves
				// UpdateRemoteSearch will flick to DownloadAvailable.
				map.Install(repository);

			else if ((map.Status == MapStatus.DownloadError && ++downloadErrors > 1) ||
					(map.Status == MapStatus.Unavailable && ++mapQueries > 2))
				GiveUpLobby();

			else if (map.Status == MapStatus.Unavailable)
				Game.ModData.MapCache.QueryRemoteMapDetails(repository, new[] { map.Uid },
					p => p.Install(repository));
		}

		static void Ready()
		{
			var om = orderManager;
			if (om == null || readySent || State != Status.Waiting)
				return;

			// The game cannot start for us without the map; ready up as soon as
			// the download that is already running finishes.
			if (Game.ModData.MapCache[om.LobbyInfo.GlobalSettings.Map].Status != MapStatus.Available)
			{
				readyPending = true;
				EnsureMapInstalled();
				return;
			}

			readySent = true;
			om.IssueOrder(Order.Command($"state {Session.ClientState.Ready}"));
		}

		/// <summary>Something unlikely has happened (for example, the opponent left
		/// the lobby just as the game was about to begin and after we readied up),
		/// so we need to unready.</summary>
		static void EnsureNotReady()
		{
			if (readySent)
			{
				readySent = false;
				orderManager?.IssueOrder(Order.Command($"state {Session.ClientState.NotReady}"));
			}

		}

		/// <summary>Readies up if the player asked while the map was still
		/// downloading.</summary>
		static void FlushPendingReady()
		{
			if (!readyPending)
				return;

			readyPending = false;
			Ready();
		}
	}
}
