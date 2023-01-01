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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenRA;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Server
{
	public enum ServerState
	{
		WaitingPlayers = 1,
		GameStarted = 2,
		ShuttingDown = 3
	}

	public enum ServerType
	{
		Local = 0,
		Multiplayer = 1,
		Dedicated = 2
	}

	public sealed class Server
	{
		[TranslationReference]
		const string CustomRules = "notification-custom-rules";

		[TranslationReference]
		const string BotsDisabled = "notification-map-bots-disabled";

		[TranslationReference]
		const string TwoHumansRequired = "notification-two-humans-required";

		[TranslationReference]
		const string ErrorGameStarted = "notification-error-game-started";

		[TranslationReference]
		const string RequiresPassword = "notification-requires-password";

		[TranslationReference]
		const string IncorrectPassword = "notification-incorrect-password";

		[TranslationReference]
		const string IncompatibleMod = "notification-incompatible-mod";

		[TranslationReference]
		const string IncompatibleVersion = "notification-incompatible-version";

		[TranslationReference]
		const string IncompatibleProtocol = "notification-incompatible-protocol";

		[TranslationReference]
		const string Banned = "notification-you-were-banned";

		[TranslationReference]
		const string TempBanned = "notification-you-were-temp-banned";

		[TranslationReference]
		const string Full = "notification-game-full";

		[TranslationReference("player")]
		const string Joined = "notification-joined";

		[TranslationReference]
		const string RequiresAuthentication = "notification-requires-authentication";

		[TranslationReference]
		const string NoPermission = "notification-no-permission-to-join";

		[TranslationReference("command")]
		const string UnknownServerCommand = "notification-unknown-server-command";

		[TranslationReference("player")]
		const string LobbyDisconnected = "notification-lobby-disconnected";

		[TranslationReference("player")]
		const string PlayerDisconnected = "notification-player-disconnected";

		[TranslationReference("player", "team")]
		const string PlayerTeamDisconnected = "notification-team-player-disconnected";

		[TranslationReference("player")]
		const string ObserverDisconnected = "notification-observer-disconnected";

		[TranslationReference("player")]
		const string NewAdmin = "notification-new-admin";

		[TranslationReference]
		const string YouWereKicked = "notification-you-were-kicked";

		[TranslationReference]
		const string GameStarted = "notification-game-started";

		public readonly MersenneTwister Random = new MersenneTwister();
		public readonly ServerType Type;

		public readonly List<Connection> Conns = new List<Connection>();

		public Session LobbyInfo;
		public ServerSettings Settings;
		public ModData ModData;
		public List<string> TempBans = new List<string>();

		// Managed by LobbyCommands
		public MapPreview Map;
		public readonly MapStatusCache MapStatusCache;
		public GameSave GameSave = null;

		// Default to the next frame for ServerType.Local - MP servers take the value from the selected GameSpeed.
		public int OrderLatency = 1;

		readonly int randomSeed;
		readonly List<TcpListener> listeners = new List<TcpListener>();
		readonly TypeDictionary serverTraits = new TypeDictionary();
		readonly PlayerDatabase playerDatabase;

		OrderBuffer orderBuffer;

		volatile ServerState internalState = ServerState.WaitingPlayers;

		readonly BlockingCollection<IServerEvent> events = new BlockingCollection<IServerEvent>();

		ReplayRecorder recorder;
		GameInformation gameInfo;
		readonly List<GameInformation.Player> worldPlayers = new List<GameInformation.Player>();
		readonly Stopwatch pingUpdated = Stopwatch.StartNew();
		readonly PlayerMessageTracker playerMessageTracker;

		public ServerState State
		{
			get => internalState;
			set => internalState = value;
		}

		public static void SyncClientToPlayerReference(Session.Client c, PlayerReference pr)
		{
			if (pr == null)
				return;

			if (pr.LockFaction)
				c.Faction = pr.Faction;
			if (pr.LockSpawn)
				c.SpawnPoint = pr.Spawn;
			if (pr.LockTeam)
				c.Team = pr.Team;
			if (pr.LockHandicap)
				c.Handicap = pr.Handicap;

			c.Color = pr.LockColor ? pr.Color : c.PreferredColor;
		}

		public void Shutdown()
		{
			State = ServerState.ShuttingDown;
		}

		public void EndGame()
		{
			foreach (var t in serverTraits.WithInterface<IEndGame>())
				t.GameEnded(this);

			recorder?.Dispose();
			recorder = null;
		}

		// Craft a fake handshake request/response because that's the
		// only way to expose the Version and OrdersProtocol.
		public void RecordFakeHandshake()
		{
			var request = new HandshakeRequest
			{
				Mod = ModData.Manifest.Id,
				Version = ModData.Manifest.Metadata.Version,
			};

			recorder.ReceiveFrame(0, 0, new Order("HandshakeRequest", null, false)
			{
				Type = OrderType.Handshake,
				IsImmediate = true,
				TargetString = request.Serialize(),
			}.Serialize());

			var response = new HandshakeResponse()
			{
				Mod = ModData.Manifest.Id,
				Version = ModData.Manifest.Metadata.Version,
				OrdersProtocol = ProtocolVersion.Orders,
				Client = new Session.Client(),
			};

			recorder.ReceiveFrame(0, 0, new Order("HandshakeResponse", null, false)
			{
				Type = OrderType.Handshake,
				IsImmediate = true,
				TargetString = response.Serialize(),
			}.Serialize());
		}

		void MapStatusChanged(string uid, Session.MapStatus status)
		{
			lock (LobbyInfo)
			{
				if (LobbyInfo.GlobalSettings.Map == uid)
					LobbyInfo.GlobalSettings.MapStatus = status;

				SyncLobbyInfo();
			}
		}

		public Server(List<IPEndPoint> endpoints, ServerSettings settings, ModData modData, ServerType type)
		{
			Log.AddChannel("server", "server.log", true);

			SocketException lastException = null;
			foreach (var endpoint in endpoints)
			{
				var listener = new TcpListener(endpoint);
				try
				{
					try
					{
						listener.Server.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 1);
					}
					catch (Exception ex)
					{
						if (ex is SocketException || ex is ArgumentException)
							Log.Write("server", $"Failed to set socket option on {endpoint}: {ex.Message}");
						else
							throw;
					}

					listener.Start();
					listeners.Add(listener);

					new Thread(() =>
					{
						while (true)
						{
							if (State != ServerState.WaitingPlayers)
							{
								listener.Stop();
								return;
							}

							// Use a 1s timeout so we can stop listening once the game starts
							if (listener.Server.Poll(1000000, SelectMode.SelectRead))
							{
								try
								{
									events.Add(new ConnectionConnectEvent(listener.AcceptSocket()));
								}
								catch (Exception)
								{
									// Ignore the exception that may be generated if the connection
									// drops while we are trying to connect
								}
							}
						}
					}) { Name = $"Connection listener ({listener.LocalEndpoint})", IsBackground = true }.Start();
				}
				catch (SocketException ex)
				{
					lastException = ex;
					Log.Write("server", $"Failed to listen on {endpoint}: {ex.Message}");
				}
			}

			if (listeners.Count == 0)
				throw lastException;

			Type = type;
			Settings = settings;

			Settings.Name = Game.Settings.SanitizedServerName(Settings.Name);

			ModData = modData;

			playerDatabase = modData.Manifest.Get<PlayerDatabase>();

			randomSeed = (int)DateTime.Now.ToBinary();

			if (type != ServerType.Local && settings.EnableGeoIP)
				GeoIP.Initialize();

			if (type != ServerType.Local)
				Nat.TryForwardPort(Settings.ListenPort, Settings.ListenPort);

			foreach (var trait in modData.Manifest.ServerTraits)
				serverTraits.Add(modData.ObjectCreator.CreateObject<ServerTrait>(trait));

			serverTraits.TrimExcess();

			Map = ModData.MapCache[settings.Map];
			MapStatusCache = new MapStatusCache(modData, MapStatusChanged, type == ServerType.Dedicated && settings.EnableLintChecks);

			playerMessageTracker = new PlayerMessageTracker(this, DispatchOrdersToClient, SendLocalizedMessageTo);

			LobbyInfo = new Session
			{
				GlobalSettings =
				{
					RandomSeed = randomSeed,
					Map = Map.Uid,
					MapStatus = Session.MapStatus.Unknown,
					ServerName = settings.Name,
					EnableSingleplayer = settings.EnableSingleplayer || Type != ServerType.Dedicated,
					EnableSyncReports = settings.EnableSyncReports,
					GameUid = Guid.NewGuid().ToString(),
					Dedicated = Type == ServerType.Dedicated
				}
			};

			if (Settings.RecordReplays && Type == ServerType.Dedicated)
			{
				recorder = new ReplayRecorder(() => { return Game.TimestampedFilename(extra: "-Server"); });

				// We only need one handshake to initialize the replay.
				// Add it now, then ignore the redundant handshakes from each client
				RecordFakeHandshake();
			}

			new Thread(_ =>
			{
				// Initial status is set off the main thread to avoid triggering a load screen when joining a skirmish game
				LobbyInfo.GlobalSettings.MapStatus = MapStatusCache[Map];
				foreach (var t in serverTraits.WithInterface<INotifyServerStart>())
					t.ServerStarted(this);

				Log.Write("server", $"Initial mod: {ModData.Manifest.Id}");
				Log.Write("server", $"Initial map: {LobbyInfo.GlobalSettings.Map}");

				while (true)
				{
					if (State != ServerState.ShuttingDown)
					{
						if (events.TryTake(out var e, 1000))
							e.Invoke(this);

						// PERF: Dedicated servers need to drain the action queue to remove references blocking the GC from cleaning up disposed objects.
						if (Type == ServerType.Dedicated)
							Game.PerformDelayedActions();

						foreach (var t in serverTraits.WithInterface<ITick>())
							t.Tick(this);

						if (State == ServerState.GameStarted)
						{
							foreach (var (playerIndex, scale) in orderBuffer.GetTickScales())
							{
								var frame = CreateTickScaleFrame(scale);
								var con = Conns.SingleOrDefault(c => c.PlayerIndex == playerIndex);

								if (con != null && con.Validated)
									DispatchFrameToClient(con, playerIndex, frame);
							}
						}
					}

					if (State == ServerState.ShuttingDown)
					{
						EndGame();
						if (type != ServerType.Local)
							Nat.TryRemovePortForward();
						break;
					}
				}

				foreach (var t in serverTraits.WithInterface<INotifyServerShutdown>())
					t.ServerShutdown(this);

				// Make sure to immediately close connections after the server is shutdown, we don't want to keep clients waiting
				foreach (var c in Conns)
					c.Dispose();

				Conns.Clear();
			})
			{ IsBackground = true }.Start();
		}

		int nextPlayerIndex;
		public int ChooseFreePlayerIndex()
		{
			return nextPlayerIndex++;
		}

		internal void OnConnectionPacket(Connection conn, int frame, byte[] data)
		{
			events.Add(new ConnectionPacketEvent(conn, frame, data));
		}

		internal void OnConnectionPing(Connection conn, int[] pingHistory, byte queueLength)
		{
			events.Add(new ConnectionPingEvent(conn, pingHistory, queueLength));
		}

		internal void OnConnectionDisconnect(Connection conn)
		{
			events.Add(new ConnectionDisconnectEvent(conn));
		}

		void AcceptConnection(Socket socket)
		{
			if (State != ServerState.WaitingPlayers)
				return;

			// Validate player identity by asking them to sign a random blob of data
			// which we can then verify against the player public key database
			var token = Convert.ToBase64String(OpenRA.Exts.MakeArray(256, _ => (byte)Random.Next()));

			var newConn = new Connection(this, socket, token);
			try
			{
				// Send handshake and client index.
				var ms = new MemoryStream(8);
				ms.WriteArray(BitConverter.GetBytes(ProtocolVersion.Handshake));
				ms.WriteArray(BitConverter.GetBytes(newConn.PlayerIndex));
				newConn.TrySendData(ms.ToArray());

				// Dispatch a handshake order
				var request = new HandshakeRequest
				{
					Mod = ModData.Manifest.Id,
					Version = ModData.Manifest.Metadata.Version,
					AuthToken = token
				};

				DispatchOrdersToClient(newConn, 0, 0, new Order("HandshakeRequest", null, false)
				{
					Type = OrderType.Handshake,
					IsImmediate = true,
					TargetString = request.Serialize()
				}.Serialize());
			}
			catch (Exception e)
			{
				Log.Write("server", $"Handshake for client {newConn.EndPoint} failed: {e}");
			}

			Conns.Add(newConn);
		}

		void ValidateClient(Connection newConn, string data)
		{
			try
			{
				if (State == ServerState.GameStarted)
				{
					Log.Write("server", $"Rejected connection from {newConn.EndPoint}; game is already started.");

					SendOrderTo(newConn, "ServerError", ErrorGameStarted);
					DropClient(newConn);
					return;
				}

				var handshake = HandshakeResponse.Deserialize(data);

				if (!string.IsNullOrEmpty(Settings.Password) && handshake.Password != Settings.Password)
				{
					var message = string.IsNullOrEmpty(handshake.Password) ? RequiresPassword : IncorrectPassword;
					SendOrderTo(newConn, "AuthenticationError", message);
					DropClient(newConn);
					return;
				}

				var ipAddress = ((IPEndPoint)newConn.EndPoint).Address;
				var client = new Session.Client
				{
					Name = OpenRA.Settings.SanitizedPlayerName(handshake.Client.Name),
					IPAddress = ipAddress.ToString(),
					AnonymizedIPAddress = Type != ServerType.Local && Settings.ShareAnonymizedIPs ? Session.AnonymizeIP(ipAddress) : null,
					Location = GeoIP.LookupCountry(ipAddress),
					Index = newConn.PlayerIndex,
					PreferredColor = handshake.Client.PreferredColor,
					Color = handshake.Client.Color,
					Faction = "Random",
					SpawnPoint = 0,
					Team = 0,
					Handicap = 0,
					State = Session.ClientState.Invalid,
				};

				if (ModData.Manifest.Id != handshake.Mod)
				{
					Log.Write("server", $"Rejected connection from {newConn.EndPoint}; mods do not match.");

					SendOrderTo(newConn, "ServerError", IncompatibleMod);
					DropClient(newConn);
					return;
				}

				if (ModData.Manifest.Metadata.Version != handshake.Version)
				{
					Log.Write("server", $"Rejected connection from {newConn.EndPoint}; Not running the same version.");

					SendOrderTo(newConn, "ServerError", IncompatibleVersion);
					DropClient(newConn);
					return;
				}

				if (handshake.OrdersProtocol != ProtocolVersion.Orders)
				{
					Log.Write("server", $"Rejected connection from {newConn.EndPoint}; incompatible Orders protocol version {handshake.OrdersProtocol}.");

					SendOrderTo(newConn, "ServerError", IncompatibleProtocol);
					DropClient(newConn);
					return;
				}

				// Check if IP is banned
				var bans = Settings.Ban.Union(TempBans);
				if (bans.Contains(client.IPAddress))
				{
					Log.Write("server", $"Rejected connection from {newConn.EndPoint}; Banned.");
					var message = Settings.Ban.Contains(client.IPAddress) ? Banned : TempBanned;
					SendOrderTo(newConn, "ServerError", message);
					DropClient(newConn);
					return;
				}

				Action completeConnection = () =>
				{
					lock (LobbyInfo)
					{
						client.Slot = LobbyInfo.FirstEmptySlot();
						client.IsAdmin = !LobbyInfo.Clients.Any(c => c.IsAdmin);

						if (client.IsObserver && !LobbyInfo.GlobalSettings.AllowSpectators)
						{
							SendOrderTo(newConn, "ServerError", Full);
							DropClient(newConn);
							return;
						}

						if (client.Slot != null)
							SyncClientToPlayerReference(client, Map.Players.Players[client.Slot]);
						else
							client.Color = Color.White;

						// Promote connection to a valid client
						LobbyInfo.Clients.Add(client);
						newConn.Validated = true;

						// Disable chat UI to stop the client sending messages that we know we will reject
						if (!client.IsAdmin && Settings.FloodLimitJoinCooldown > 0)
							playerMessageTracker.DisableChatUI(newConn, Settings.FloodLimitJoinCooldown);

						Log.Write("server", $"Client {newConn.PlayerIndex}: Accepted connection from {newConn.EndPoint}.");

						if (client.Fingerprint != null)
							Log.Write("server", $"Client {newConn.PlayerIndex}: Player fingerprint is {client.Fingerprint}.");

						foreach (var t in serverTraits.WithInterface<IClientJoined>())
							t.ClientJoined(this, newConn);

						SyncLobbyInfo();

						Log.Write("server", $"{client.Name} ({newConn.EndPoint}) has joined the game.");

						if (Type != ServerType.Local)
							SendLocalizedMessage(Joined, Translation.Arguments("player", client.Name));

						if (Type == ServerType.Dedicated)
						{
							var motdFile = Path.Combine(Platform.SupportDir, "motd.txt");
							if (!File.Exists(motdFile))
								File.WriteAllText(motdFile, "Welcome, have fun and good luck!");

							var motd = File.ReadAllText(motdFile);
							if (!string.IsNullOrEmpty(motd))
								SendOrderTo(newConn, "Message", motd);
						}

						if ((LobbyInfo.GlobalSettings.MapStatus & Session.MapStatus.UnsafeCustomRules) != 0)
							SendLocalizedMessageTo(newConn, CustomRules);

						if (!LobbyInfo.GlobalSettings.EnableSingleplayer)
							SendLocalizedMessageTo(newConn, TwoHumansRequired);
						else if (Map.Players.Players.Where(p => p.Value.Playable).All(p => !p.Value.AllowBots))
							SendLocalizedMessageTo(newConn, BotsDisabled);
					}
				};

				if (Type == ServerType.Local)
				{
					// Local servers can only be joined by the local client, so we can trust their identity without validation
					client.Fingerprint = handshake.Fingerprint;
					completeConnection();
				}
				else if (!string.IsNullOrEmpty(handshake.Fingerprint) && !string.IsNullOrEmpty(handshake.AuthSignature))
				{
					Task.Run(async () =>
					{
						PlayerProfile profile = null;

						try
						{
							var httpClient = HttpClientFactory.Create();
							var httpResponseMessage = await httpClient.GetAsync(playerDatabase.Profile + handshake.Fingerprint);
							var result = await httpResponseMessage.Content.ReadAsStreamAsync();

							var yaml = MiniYaml.FromStream(result).First();
							if (yaml.Key == "Player")
							{
								profile = FieldLoader.Load<PlayerProfile>(yaml.Value);

								var publicKey = Encoding.ASCII.GetString(Convert.FromBase64String(profile.PublicKey));
								var parameters = CryptoUtil.DecodePEMPublicKey(publicKey);
								if (!profile.KeyRevoked && CryptoUtil.VerifySignature(parameters, newConn.AuthToken, handshake.AuthSignature))
								{
									client.Fingerprint = handshake.Fingerprint;
									Log.Write("server", $"{newConn.EndPoint} authenticated as {profile.ProfileName} (UID {profile.ProfileID})");
								}
								else if (profile.KeyRevoked)
								{
									profile = null;
									Log.Write("server", $"{newConn.EndPoint} failed to authenticate as {handshake.Fingerprint} (key revoked)");
								}
								else
								{
									profile = null;
									Log.Write("server", $"{newConn.EndPoint} failed to authenticate as {handshake.Fingerprint} (signature verification failed)");
								}
							}
							else
								Log.Write("server", $"{newConn.EndPoint} failed to authenticate as {handshake.Fingerprint} (invalid server response: `{yaml.Key}` is not `Player`)");
						}
						catch (Exception ex)
						{
							Log.Write("server", $"{newConn.EndPoint} failed to authenticate as {handshake.Fingerprint} (exception occurred)");
							Log.Write("server", ex.ToString());
						}

						events.Add(new CallbackEvent(() =>
						{
							var notAuthenticated = Type == ServerType.Dedicated && profile == null && (Settings.RequireAuthentication || Settings.ProfileIDWhitelist.Length > 0);
							var blacklisted = Type == ServerType.Dedicated && profile != null && Settings.ProfileIDBlacklist.Contains(profile.ProfileID);
							var notWhitelisted = Type == ServerType.Dedicated && Settings.ProfileIDWhitelist.Length > 0 &&
								(profile == null || !Settings.ProfileIDWhitelist.Contains(profile.ProfileID));

							if (notAuthenticated)
							{
								Log.Write("server", $"Rejected connection from {newConn.EndPoint}; Not authenticated.");
								SendOrderTo(newConn, "ServerError", RequiresAuthentication);
								DropClient(newConn);
							}
							else if (blacklisted || notWhitelisted)
							{
								if (blacklisted)
									Log.Write("server", $"Rejected connection from {newConn.EndPoint}; In server blacklist.");
								else
									Log.Write("server", $"Rejected connection from {newConn.EndPoint}; Not in server whitelist.");

								SendOrderTo(newConn, "ServerError", NoPermission);
								DropClient(newConn);
							}
							else
								completeConnection();
						}));
					});
				}
				else
				{
					if (Type == ServerType.Dedicated && (Settings.RequireAuthentication || Settings.ProfileIDWhitelist.Length > 0))
					{
						Log.Write("server", $"Rejected connection from {newConn.EndPoint}; Not authenticated.");
						SendOrderTo(newConn, "ServerError", RequiresAuthentication);
						DropClient(newConn);
					}
					else
						completeConnection();
				}
			}
			catch (Exception ex)
			{
				Log.Write("server", $"Dropping connection {newConn.EndPoint} because an error occurred:");
				Log.Write("server", ex.ToString());
				DropClient(newConn);
			}
		}

		byte[] CreateFrame(int client, int frame, byte[] data)
		{
			var ms = new MemoryStream(data.Length + 12);
			ms.WriteArray(BitConverter.GetBytes(data.Length + 4));
			ms.WriteArray(BitConverter.GetBytes(client));
			ms.WriteArray(BitConverter.GetBytes(frame));
			ms.WriteArray(data);
			return ms.GetBuffer();
		}

		byte[] CreateAckFrame(int frame, byte count)
		{
			var ms = new MemoryStream(14);
			ms.WriteArray(BitConverter.GetBytes(6));
			ms.WriteArray(BitConverter.GetBytes(0));
			ms.WriteArray(BitConverter.GetBytes(frame));
			ms.WriteByte((byte)OrderType.Ack);
			ms.WriteByte(count);
			return ms.GetBuffer();
		}

		byte[] CreateTickScaleFrame(float scale)
		{
			var ms = new MemoryStream(17);
			ms.WriteArray(BitConverter.GetBytes(9));
			ms.WriteArray(BitConverter.GetBytes(0));
			ms.WriteArray(BitConverter.GetBytes(0));
			ms.WriteByte((byte)OrderType.TickScale);
			ms.Write(scale);
			return ms.GetBuffer();
		}

		void DispatchOrdersToClient(Connection c, int client, int frame, byte[] data)
		{
			DispatchFrameToClient(c, client, CreateFrame(client, frame, data));
		}

		void DispatchFrameToClient(Connection c, int client, byte[] frameData)
		{
			if (!c.TrySendData(frameData))
			{
				DropClient(c);
				Log.Write("server", $"Dropping client {client.ToString(CultureInfo.InvariantCulture)} because dispatching orders failed!");
			}
		}

		bool AnyUndefinedWinStates()
		{
			var lastTeam = -1;
			var remainingPlayers = gameInfo.Players.Where(p => p.Outcome == WinState.Undefined);
			foreach (var player in remainingPlayers)
			{
				if (lastTeam >= 0 && (player.Team != lastTeam || player.Team == 0))
					return true;

				lastTeam = player.Team;
			}

			return false;
		}

		void SetPlayerDefeat(int playerIndex)
		{
			var defeatedPlayer = worldPlayers[playerIndex];
			if (defeatedPlayer == null || defeatedPlayer.Outcome != WinState.Undefined)
				return;

			defeatedPlayer.Outcome = WinState.Lost;
			defeatedPlayer.OutcomeTimestampUtc = DateTime.UtcNow;

			// Set remaining players as winners if only one side remains
			if (!AnyUndefinedWinStates())
			{
				var now = DateTime.UtcNow;
				var remainingPlayers = gameInfo.Players.Where(p => p.Outcome == WinState.Undefined);
				foreach (var winner in remainingPlayers)
				{
					winner.Outcome = WinState.Won;
					winner.OutcomeTimestampUtc = now;
				}
			}
		}

		void OutOfSync(int frame)
		{
			Log.Write("server", $"Out of sync detected at frame {frame}, cancel replay recording");

			// Make sure the written file is not valid
			// TODO: storing a serverside replay on desync would be extremely useful
			recorder.Metadata = null;

			recorder.Dispose();

			// Stop the recording
			recorder = null;
		}

		readonly Dictionary<int, byte[]> syncForFrame = new Dictionary<int, byte[]>();
		int lastDefeatStateFrame;
		ulong lastDefeatState;

		void HandleSyncOrder(int frame, byte[] packet)
		{
			if (syncForFrame.TryGetValue(frame, out var existingSync))
			{
				if (packet.Length != existingSync.Length)
				{
					OutOfSync(frame);
					return;
				}

				for (var i = 0; i < packet.Length; i++)
				{
					if (packet[i] != existingSync[i])
					{
						OutOfSync(frame);
						return;
					}
				}
			}
			else
			{
				// Update player losses based on the new defeat state.
				// Do this once for the first player, the check above
				// guarantees a desync if any other player disagrees.
				var playerDefeatState = BitConverter.ToUInt64(packet, 1 + 4);
				if (frame > lastDefeatStateFrame && lastDefeatState != playerDefeatState)
				{
					var newDefeats = playerDefeatState & ~lastDefeatState;
					for (var i = 0; i < worldPlayers.Count; i++)
						if ((newDefeats & (1UL << i)) != 0)
							SetPlayerDefeat(i);

					lastDefeatState = playerDefeatState;
					lastDefeatStateFrame = frame;
				}

				syncForFrame.Add(frame, packet);
			}
		}

		public void DispatchOrdersToClients(Connection conn, int frame, byte[] data)
		{
			var from = conn.PlayerIndex;
			var frameData = CreateFrame(from, frame, data);
			foreach (var c in Conns.ToList())
				if (c != conn && c.Validated)
					DispatchFrameToClient(c, from, frameData);

			RecordOrder(frame, data, from);
		}

		void RecordOrder(int frame, byte[] data, int from)
		{
			if (recorder != null)
			{
				recorder.ReceiveFrame(from, frame, data);

				if (data.Length > 0 && data[0] == (byte)OrderType.SyncHash)
				{
					if (data.Length == Order.SyncHashOrderLength)
						HandleSyncOrder(frame, data);
					else
						Log.Write("server", $"Dropped sync order with length {data.Length} from client {from}. Expected length {Order.SyncHashOrderLength}.");
				}
			}
		}

		public void DispatchServerOrdersToClients(Order order)
		{
			DispatchServerOrdersToClients(order.Serialize());
		}

		public void DispatchServerOrdersToClients(byte[] data, int frame = 0)
		{
			var from = 0;
			var frameData = CreateFrame(from, frame, data);
			foreach (var c in Conns.ToList())
				if (c.Validated)
					DispatchFrameToClient(c, from, frameData);

			RecordOrder(frame, data, from);
		}

		public void ReceiveOrders(Connection conn, int frame, byte[] data)
		{
			// Make sure we don't accidentally forward on orders from clients who we have just dropped
			if (!Conns.Contains(conn))
				return;

			if (frame == 0)
				InterpretServerOrders(conn, data);
			else
			{
				// Non-immediate orders must be projected into the future so that all players can
				// apply them on the same world tick. We can do this directly when forwarding the
				// packet on to other clients, but sending the same data back to the client that
				// sent it just to update the frame number would be wasteful. We instead send them
				// a separate Ack packet that tells them to apply the order from a locally stored queue.
				// TODO: Replace static latency with a dynamic order buffering system
				if (data.Length == 0 || data[0] != (byte)OrderType.SyncHash)
				{
					frame += OrderLatency;
					DispatchFrameToClient(conn, conn.PlayerIndex, CreateAckFrame(frame, 1));

					orderBuffer.AddOrderTimestamp(conn.PlayerIndex);

					// Track the last frame for each client so the disconnect handling can write
					// an EndOfOrders marker with the correct frame number.
					// TODO: This should be handled by the order buffering system too
					conn.LastOrdersFrame = frame;
				}

				DispatchOrdersToClients(conn, frame, data);
			}

			GameSave?.DispatchOrders(conn, frame, data);
		}

		void InterpretServerOrders(Connection conn, byte[] data)
		{
			var ms = new MemoryStream(data);
			var br = new BinaryReader(ms);

			try
			{
				while (ms.Position < ms.Length)
				{
					var o = Order.Deserialize(null, br);
					if (o != null)
						InterpretServerOrder(conn, o);
				}
			}
			catch (EndOfStreamException) { }
			catch (NotImplementedException) { }
		}

		public void SendOrderTo(Connection conn, string order, string data)
		{
			DispatchOrdersToClient(conn, 0, 0, Order.FromTargetString(order, data, true).Serialize());
		}

		public void SendMessage(string text)
		{
			DispatchServerOrdersToClients(Order.FromTargetString("Message", text, true));

			if (Type == ServerType.Dedicated)
				WriteLineWithTimeStamp(text);
		}

		public void SendLocalizedMessage(string key, Dictionary<string, object> arguments = null)
		{
			var text = LocalizedMessage.Serialize(key, arguments);
			DispatchServerOrdersToClients(Order.FromTargetString("LocalizedMessage", text, true));

			if (Type == ServerType.Dedicated)
				WriteLineWithTimeStamp(ModData.Translation.GetString(key, arguments));
		}

		public void SendLocalizedMessageTo(Connection conn, string key, Dictionary<string, object> arguments = null)
		{
			var text = LocalizedMessage.Serialize(key, arguments);
			DispatchOrdersToClient(conn, 0, 0, Order.FromTargetString("LocalizedMessage", text, true).Serialize());
		}

		void WriteLineWithTimeStamp(string line)
		{
			Console.WriteLine($"[{DateTime.Now.ToString(Settings.TimestampFormat)}] {line}");
		}

		void InterpretServerOrder(Connection conn, Order o)
		{
			lock (LobbyInfo)
			{
				// Only accept handshake responses from unvalidated clients
				// Anything else may be an attempt to exploit the server
				if (!conn.Validated)
				{
					if (o.OrderString == "HandshakeResponse")
						ValidateClient(conn, o.TargetString);
					else
					{
						Log.Write("server", $"Rejected connection from {conn.EndPoint}; Order `{o.OrderString}` is not a `HandshakeResponse`.");
						DropClient(conn);
					}

					return;
				}

				switch (o.OrderString)
				{
					case "Command":
						{
							var handledBy = serverTraits.WithInterface<IInterpretCommand>()
								.FirstOrDefault(t => t.InterpretCommand(this, conn, GetClient(conn), o.TargetString));

							if (handledBy == null)
							{
								Log.Write("server", $"Unknown server command: {o.TargetString}");
								SendLocalizedMessageTo(conn, UnknownServerCommand, Translation.Arguments("command", o.TargetString));
							}

							break;
						}

					case "Chat":
						{
							if (Type == ServerType.Local || !playerMessageTracker.IsPlayerAtFloodLimit(conn))
								DispatchOrdersToClients(conn, 0, o.Serialize());

							break;
						}

					case "GameSaveTraitData":
						{
							if (GameSave != null)
							{
								var data = MiniYaml.FromString(o.TargetString)[0];
								GameSave.AddTraitData(int.Parse(data.Key), data.Value);
							}

							break;
						}

					case "CreateGameSave":
						{
							if (GameSave != null)
							{
								// Sanitize potentially malicious input
								var filename = o.TargetString;
								var invalidIndex = -1;
								var invalidChars = Path.GetInvalidFileNameChars();
								while ((invalidIndex = filename.IndexOfAny(invalidChars)) != -1)
									filename = filename.Remove(invalidIndex, 1);

								var baseSavePath = Path.Combine(
									Platform.SupportDir,
									"Saves",
									ModData.Manifest.Id,
									ModData.Manifest.Metadata.Version);

								if (!Directory.Exists(baseSavePath))
									Directory.CreateDirectory(baseSavePath);

								GameSave.Save(Path.Combine(baseSavePath, filename));
								DispatchServerOrdersToClients(Order.FromTargetString("GameSaved", filename, true));
							}

							break;
						}

					case "LoadGameSave":
						{
							if (Type == ServerType.Dedicated || State >= ServerState.GameStarted)
								break;

							// Sanitize potentially malicious input
							var filename = o.TargetString;
							var invalidIndex = -1;
							var invalidChars = Path.GetInvalidFileNameChars();
							while ((invalidIndex = filename.IndexOfAny(invalidChars)) != -1)
								filename = filename.Remove(invalidIndex, 1);

							var savePath = Path.Combine(
								Platform.SupportDir,
								"Saves",
								ModData.Manifest.Id,
								ModData.Manifest.Metadata.Version,
								filename);

							GameSave = new GameSave(savePath);
							LobbyInfo.GlobalSettings = GameSave.GlobalSettings;
							LobbyInfo.Slots = GameSave.Slots;

							// Reassign clients to slots
							//  - Bot ordering is preserved
							//  - Humans are assigned on a first-come-first-serve basis
							//  - Leftover humans become spectators

							// Start by removing all bots and assigning all players as spectators
							foreach (var c in LobbyInfo.Clients)
							{
								if (c.Bot != null)
									LobbyInfo.Clients.Remove(c);
								else
									c.Slot = null;
							}

							// Rebuild/remap the saved client state
							// TODO: Multiplayer saves should leave all humans as spectators so they can manually pick slots
							var adminClientIndex = LobbyInfo.Clients.First(c => c.IsAdmin).Index;
							foreach (var kv in GameSave.SlotClients)
							{
								if (kv.Value.Bot != null)
								{
									var bot = new Session.Client()
									{
										Index = ChooseFreePlayerIndex(),
										State = Session.ClientState.NotReady,
										BotControllerClientIndex = adminClientIndex
									};

									kv.Value.ApplyTo(bot);
									LobbyInfo.Clients.Add(bot);
								}
								else
								{
									// This will throw if the server doesn't have enough human clients to fill all player slots
									// See TODO above - this isn't a problem in practice because MP saves won't use this
									var client = LobbyInfo.Clients.First(c => c.Slot == null);
									kv.Value.ApplyTo(client);
								}
							}

							SyncLobbyInfo();
							SyncLobbyClients();

							break;
						}
				}
			}
		}

		public void ReceivePing(Connection conn, int[] pingHistory)
		{
			// Levels set relative to the default order lag of 3 net ticks (360ms)
			// TODO: Adjust this once dynamic lag is implemented
			var latency = pingHistory.Sum() / pingHistory.Length;

			var quality = latency < 240 ? Session.ConnectionQuality.Good :
				latency < 360 ? Session.ConnectionQuality.Moderate :
				Session.ConnectionQuality.Poor;

			lock (LobbyInfo)
			{
				foreach (var c in LobbyInfo.Clients)
					if (c.Index == conn.PlayerIndex || (c.Bot != null && c.BotControllerClientIndex == conn.PlayerIndex))
						c.ConnectionQuality = quality;

				// Update ping without forcing a full update
				// Note that syncing pings doesn't trigger INotifySyncLobbyInfo
				if (pingUpdated.ElapsedMilliseconds > 5000)
				{
					var nodes = new List<MiniYamlNode>();
					foreach (var c in LobbyInfo.Clients)
						nodes.Add(new MiniYamlNode($"ConnectionQuality@{c.Index}", FieldSaver.FormatValue(c.ConnectionQuality)));

					DispatchServerOrdersToClients(Order.FromTargetString("SyncConnectionQuality", nodes.WriteToString(), true));
					pingUpdated.Restart();
				}
			}
		}

		public Session.Client GetClient(Connection conn)
		{
			if (conn == null)
				return null;

			return LobbyInfo.ClientWithIndex(conn.PlayerIndex);
		}

		public void DropClient(Connection toDrop)
		{
			lock (LobbyInfo)
			{
				orderBuffer?.RemovePlayer(toDrop.PlayerIndex);
				Conns.Remove(toDrop);

				var dropClient = LobbyInfo.Clients.FirstOrDefault(c => c.Index == toDrop.PlayerIndex);
				if (dropClient == null)
				{
					toDrop.Dispose();
					return;
				}

				if (State == ServerState.GameStarted)
				{
					if (dropClient.IsObserver)
						SendLocalizedMessage(ObserverDisconnected, Translation.Arguments("player", dropClient.Name));
					else if (dropClient.Team > 0)
						SendLocalizedMessage(PlayerTeamDisconnected, Translation.Arguments("player", dropClient.Name, "team", dropClient.Team));
					else
						SendLocalizedMessage(PlayerDisconnected, Translation.Arguments("player", dropClient.Name));
				}
				else
					SendLocalizedMessage(LobbyDisconnected, Translation.Arguments("player", dropClient.Name));

				LobbyInfo.Clients.RemoveAll(c => c.Index == toDrop.PlayerIndex);

				// Client was the server admin
				// TODO: Reassign admin for game in progress via an order
				if (Type == ServerType.Dedicated && dropClient.IsAdmin && State == ServerState.WaitingPlayers)
				{
					// Remove any bots controlled by the admin
					LobbyInfo.Clients.RemoveAll(c => c.Bot != null && c.BotControllerClientIndex == toDrop.PlayerIndex);

					var nextAdmin = LobbyInfo.Clients.Where(c1 => c1.Bot == null)
						.MinByOrDefault(c => c.Index);

					if (nextAdmin != null)
					{
						nextAdmin.IsAdmin = true;
						SendLocalizedMessage(NewAdmin, Translation.Arguments("player", nextAdmin.Name));
					}
				}

				var disconnectPacket = new MemoryStream(5);
				disconnectPacket.WriteByte((byte)OrderType.Disconnect);
				disconnectPacket.Write(toDrop.PlayerIndex);
				DispatchServerOrdersToClients(disconnectPacket.ToArray(), toDrop.LastOrdersFrame + 1);

				if (gameInfo != null)
					foreach (var player in gameInfo.Players.Where(p => p.ClientIndex == toDrop.PlayerIndex))
						player.DisconnectFrame = toDrop.LastOrdersFrame + 1;

				// All clients have left: clean up
				if (!Conns.Any(c => c.Validated))
					foreach (var t in serverTraits.WithInterface<INotifyServerEmpty>())
						t.ServerEmpty(this);

				if (Conns.Any(c => c.Validated) || Type == ServerType.Dedicated)
					SyncLobbyClients();

				if (Type != ServerType.Dedicated && dropClient.IsAdmin)
					Shutdown();
			}

			toDrop.Dispose();
		}

		public void SyncLobbyInfo()
		{
			lock (LobbyInfo)
			{
				if (State == ServerState.WaitingPlayers) // Don't do this while the game is running, it breaks things!
					DispatchServerOrdersToClients(Order.FromTargetString("SyncInfo", LobbyInfo.Serialize(), true));

				foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
					t.LobbyInfoSynced(this);
			}
		}

		public void SyncLobbyClients()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			lock (LobbyInfo)
			{
				// TODO: Only need to sync the specific client that has changed to avoid conflicts!
				var clientData = LobbyInfo.Clients.Select(client => client.Serialize()).ToList();

				DispatchServerOrdersToClients(Order.FromTargetString("SyncLobbyClients", clientData.WriteToString(), true));

				foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
					t.LobbyInfoSynced(this);

				// The full LobbyInfo includes ping info, so we can delay the next partial ping update
				// TODO: Replace the special-case ping updates with more general LobbyInfo delta updates
				pingUpdated.Restart();
			}
		}

		public void SyncLobbySlots()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			lock (LobbyInfo)
			{
				// TODO: Don't sync all the slots if just one changed!
				var slotData = LobbyInfo.Slots.Select(slot => slot.Value.Serialize()).ToList();

				DispatchServerOrdersToClients(Order.FromTargetString("SyncLobbySlots", slotData.WriteToString(), true));

				foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
					t.LobbyInfoSynced(this);
			}
		}

		public void SyncLobbyGlobalSettings()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			lock (LobbyInfo)
			{
				var sessionData = new List<MiniYamlNode> { LobbyInfo.GlobalSettings.Serialize() };

				DispatchServerOrdersToClients(Order.FromTargetString("SyncLobbyGlobalSettings", sessionData.WriteToString(), true));

				foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
					t.LobbyInfoSynced(this);
			}
		}

		public void StartGame()
		{
			lock (LobbyInfo)
			{
				WriteLineWithTimeStamp(ModData.Translation.GetString(GameStarted));

				// Drop any players who are not ready
				foreach (var c in Conns.Where(c => !c.Validated || GetClient(c).IsInvalid).ToArray())
				{
					SendOrderTo(c, "ServerError", YouWereKicked);
					DropClient(c);
				}

				// Enable game saves for singleplayer missions only
				// TODO: Enable for multiplayer (non-dedicated servers only) once the lobby UI has been created
				LobbyInfo.GlobalSettings.GameSavesEnabled = Type != ServerType.Dedicated && LobbyInfo.NonBotClients.Count() == 1;

				// Player list for win/loss tracking
				// HACK: NonCombatant and non-Playable players are set to null to simplify replay tracking
				// The null padding is needed to keep the player indexes in sync with world.Players on the clients
				// This will need to change if future code wants to use worldPlayers for other purposes
				var playerRandom = new MersenneTwister(LobbyInfo.GlobalSettings.RandomSeed);
				foreach (var cmpi in Map.WorldActorInfo.TraitInfos<ICreatePlayersInfo>())
					cmpi.CreateServerPlayers(Map, LobbyInfo, worldPlayers, playerRandom);

				if (recorder != null)
				{
					gameInfo = new GameInformation
					{
						Mod = Game.ModData.Manifest.Id,
						Version = Game.ModData.Manifest.Metadata.Version,
						MapUid = Map.Uid,
						MapTitle = Map.Title,
						StartTimeUtc = DateTime.UtcNow,
					};

					// Replay metadata should only include the playable players
					foreach (var p in worldPlayers)
						if (p != null)
							gameInfo.Players.Add(p);

					recorder.Metadata = new ReplayMetadata(gameInfo);
				}

				SyncLobbyInfo();

				var gameSpeeds = Game.ModData.Manifest.Get<GameSpeeds>();
				var gameSpeedName = LobbyInfo.GlobalSettings.OptionOrDefault("gamespeed", gameSpeeds.DefaultSpeed);

				var gameSpeed = gameSpeeds.Speeds[gameSpeedName];

				orderBuffer = new OrderBuffer();
				orderBuffer.Start(gameSpeed, Conns.Where(c => c.Validated).Select(c => c.PlayerIndex));

				State = ServerState.GameStarted;

				if (Type != ServerType.Local)
					OrderLatency = gameSpeed.OrderLatency;

				if (GameSave == null && LobbyInfo.GlobalSettings.GameSavesEnabled)
					GameSave = new GameSave();

				var startGameData = "";
				if (GameSave != null)
				{
					GameSave.StartGame(LobbyInfo, Map);
					if (GameSave.LastOrdersFrame >= 0)
					{
						startGameData = new List<MiniYamlNode>()
						{
							new MiniYamlNode("SaveLastOrdersFrame", GameSave.LastOrdersFrame.ToString()),
							new MiniYamlNode("SaveSyncFrame", GameSave.LastSyncFrame.ToString())
						}.WriteToString();
					}
				}

				DispatchServerOrdersToClients(Order.FromTargetString("StartGame", startGameData, true));

				foreach (var t in serverTraits.WithInterface<IStartGame>())
					t.GameStarted(this);

				var firstFrame = 1;
				if (GameSave != null && GameSave.LastOrdersFrame >= 0)
				{
					GameSave.ParseOrders(LobbyInfo, (frame, client, data) =>
					{
						foreach (var c in Conns)
							if (c.Validated)
								DispatchOrdersToClient(c, client, frame, data);
					});

					firstFrame += GameSave.LastOrdersFrame;
				}

				// ReceiveOrders projects player orders into the future so that all players can
				// apply them on the same world tick.
				// Clients require every frame to have an orders packet associated with it, so we must
				// inject an empty packet for each frame that we are skipping forwards.
				// TODO: Replace static latency with a dynamic order buffering system
				var conns = Conns.Where(c => c.Validated).ToList();
				foreach (var from in conns)
				{
					for (var i = 0; i < OrderLatency; i++)
					{
						from.LastOrdersFrame = firstFrame + i;
						var frameData = CreateFrame(from.PlayerIndex, from.LastOrdersFrame, Array.Empty<byte>());
						foreach (var to in conns)
							DispatchFrameToClient(to, from.PlayerIndex, frameData);

						RecordOrder(from.LastOrdersFrame, Array.Empty<byte>(), from.PlayerIndex);
						GameSave?.DispatchOrders(from, from.LastOrdersFrame, Array.Empty<byte>());
					}
				}
			}
		}

		public ConnectionTarget GetEndpointForLocalConnection()
		{
			var endpoints = new List<DnsEndPoint>();
			foreach (var listener in listeners)
			{
				var endpoint = (IPEndPoint)listener.LocalEndpoint;
				if (IPAddress.IPv6Any.Equals(endpoint.Address))
					endpoints.Add(new DnsEndPoint(IPAddress.IPv6Loopback.ToString(), endpoint.Port));
				else if (IPAddress.Any.Equals(endpoint.Address))
					endpoints.Add(new DnsEndPoint(IPAddress.Loopback.ToString(), endpoint.Port));
				else
					endpoints.Add(new DnsEndPoint(endpoint.Address.ToString(), endpoint.Port));
			}

			return new ConnectionTarget(endpoints);
		}

		interface IServerEvent { void Invoke(Server server); }

		class ConnectionConnectEvent : IServerEvent
		{
			readonly Socket socket;
			public ConnectionConnectEvent(Socket socket)
			{
				this.socket = socket;
			}

			void IServerEvent.Invoke(Server server)
			{
				server.AcceptConnection(socket);
			}
		}

		class ConnectionDisconnectEvent : IServerEvent
		{
			readonly Connection connection;
			public ConnectionDisconnectEvent(Connection connection)
			{
				this.connection = connection;
			}

			void IServerEvent.Invoke(Server server)
			{
				server.DropClient(connection);
			}
		}

		class ConnectionPacketEvent : IServerEvent
		{
			readonly Connection connection;
			readonly int frame;
			readonly byte[] data;

			public ConnectionPacketEvent(Connection connection, int frame, byte[] data)
			{
				this.connection = connection;
				this.frame = frame;
				this.data = data;
			}

			void IServerEvent.Invoke(Server server)
			{
				server.ReceiveOrders(connection, frame, data);
			}
		}

		class ConnectionPingEvent : IServerEvent
		{
			readonly Connection connection;
			readonly int[] pingHistory;

			// TODO: future net code changes
			#pragma warning disable IDE0052
			readonly byte queueLength;
			#pragma warning restore IDE0052

			public ConnectionPingEvent(Connection connection, int[] pingHistory, byte queueLength)
			{
				this.connection = connection;
				this.pingHistory = pingHistory;
				this.queueLength = queueLength;
			}

			void IServerEvent.Invoke(Server server)
			{
				server.ReceivePing(connection, pingHistory);
			}
		}

		class CallbackEvent : IServerEvent
		{
			readonly Action action;

			public CallbackEvent(Action action)
			{
				this.action = action;
			}

			void IServerEvent.Invoke(Server server)
			{
				action();
			}
		}
	}
}
