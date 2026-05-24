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
using System.Collections.Frozen;
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
		Skirmish = 1,
		Multiplayer = 2,
		Dedicated = 3
	}

	[IncludeStaticFluentReferences(typeof(PlayerMessageTracker), typeof(VoteKickTracker))]
	public sealed partial class Server
	{
		[FluentReference]
		const string CustomRules = "notification-custom-rules";

		[FluentReference]
		const string TwoHumansRequired = "notification-two-humans-required";

		[FluentReference]
		const string ErrorGameStarted = "notification-error-game-started";

		[FluentReference]
		const string RequiresPassword = "notification-requires-password";

		[FluentReference]
		const string IncorrectPassword = "notification-incorrect-password";

		[FluentReference]
		const string IncompatibleMod = "notification-incompatible-mod";

		[FluentReference]
		const string IncompatibleVersion = "notification-incompatible-version";

		[FluentReference]
		const string IncompatibleProtocol = "notification-incompatible-protocol";

		[FluentReference]
		const string Banned = "notification-you-were-banned";

		[FluentReference]
		const string TempBanned = "notification-you-were-temp-banned";

		[FluentReference]
		const string Full = "notification-game-full";

		[FluentReference("player")]
		const string Joined = "notification-joined";

		[FluentReference]
		const string RequiresAuthentication = "notification-requires-authentication";

		[FluentReference]
		const string NoPermission = "notification-no-permission-to-join";

		[FluentReference("command")]
		const string UnknownServerCommand = "notification-unknown-server-command";

		[FluentReference("player")]
		const string LobbyDisconnected = "notification-lobby-disconnected";

		[FluentReference("player")]
		const string PlayerDisconnected = "notification-player-disconnected";

		[FluentReference("player", "team")]
		const string PlayerTeamDisconnected = "notification-team-player-disconnected";

		[FluentReference("player")]
		const string ObserverDisconnected = "notification-observer-disconnected";

		[FluentReference("player")]
		const string NewAdmin = "notification-new-admin";

		[FluentReference]
		const string YouWereKicked = "notification-you-were-kicked";

		[FluentReference]
		const string GameStarted = "notification-game-started";

		public readonly MersenneTwister Random = new();
		public readonly ServerType Type;
		public bool IsMultiplayer => Type == ServerType.Dedicated || Type == ServerType.Multiplayer;

		public readonly List<Connection> Conns = [];

		public Session LobbyInfo;
		public ServerSettings Settings;
		public ModData ModData;
		public List<string> TempBans = [];
		public string GeneratedMapData;

		// Managed by LobbyCommands
		public MapPreview Map;
		public readonly MapStatusCache MapStatusCache;
		public GameSave GameSave;
		public FrozenSet<string> MapPool;

		// Default to the next frame for ServerType.Local - MP servers take the value from the selected GameSpeed.
		public int OrderLatency = 1;

		readonly int randomSeed;
		readonly List<TcpListener> listeners = [];
		readonly TypeDictionary serverTraits = [];
		readonly PlayerDatabase playerDatabase;

		OrderBuffer orderBuffer;

		volatile ServerState internalState = ServerState.WaitingPlayers;

		readonly BlockingCollection<IServerEvent> events = [];

		ReplayRecorder recorder;
		GameInformation gameInfo;
		readonly List<GameInformation.Player> worldPlayers = [];
		readonly Stopwatch pingUpdated = Stopwatch.StartNew();

		public readonly VoteKickTracker VoteKickTracker;
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
					catch (Exception ex) when (ex is SocketException || ex is ArgumentException)
					{
						Log.Write("server", $"Failed to set socket option on {endpoint}: {ex.Message}");
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
					})
					{ Name = $"Connection listener ({listener.LocalEndpoint})", IsBackground = true }.Start();
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

			playerDatabase = modData.GetOrCreate<PlayerDatabase>();

			randomSeed = (int)DateTime.Now.ToBinary();

			if (IsMultiplayer && settings.EnableGeoIP)
				GeoIP.Initialize();

			if (IsMultiplayer)
				Nat.TryForwardPort(Settings.ListenPort, Settings.ListenPort);

			foreach (var trait in modData.Manifest.ServerTraits)
				serverTraits.Add(modData.ObjectCreator.CreateObject<ServerTrait>(trait));

			serverTraits.TrimExcess();

			MapStatusCache = new MapStatusCache(modData, MapStatusChanged, type == ServerType.Dedicated && settings.EnableLintChecks);

			playerMessageTracker = new PlayerMessageTracker(this, DispatchOrdersToClient, SendFluentMessageTo);
			VoteKickTracker = new VoteKickTracker(this);

			LobbyInfo = new Session
			{
				GlobalSettings =
				{
					RandomSeed = randomSeed,
					ServerName = settings.Name,
					EnableSingleplayer = settings.EnableSingleplayer || Type != ServerType.Dedicated,
					EnableMapGeneration = settings.EnableMapGeneration,
					EnableSyncReports = settings.EnableSyncReports,
					GameUid = Guid.NewGuid().ToString(),
					Dedicated = Type == ServerType.Dedicated
				}
			};

			if (Settings.RecordReplays && Type == ServerType.Dedicated)
			{
				recorder = new ReplayRecorder(() => Game.TimestampedFilename(extra: "-Server"));

				// We only need one handshake to initialize the replay.
				// Add it now, then ignore the redundant handshakes from each client
				RecordFakeHandshake();
			}

			new Thread(_ =>
			{
				// Note: at least one of these is required to set the initial LobbyInfo.Map and MapStatus
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
						if (IsMultiplayer)
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
			{ IsBackground = true, Name = "ServerThread" }.Start();
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
				ms.Write(ProtocolVersion.Handshake);
				ms.Write(newConn.PlayerIndex);
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

		void ValidateClient(Connection newConn, string data, string name)
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

				var handshake = HandshakeResponse.Deserialize(data, name);

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
					AnonymizedIPAddress = IsMultiplayer && Settings.ShareAnonymizedIPs ? Session.AnonymizeIP(ipAddress) : null,
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

				void CompleteConnection()
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

						var p = ModData.MapCache[LobbyInfo.GlobalSettings.Map];
						if (p.Class == MapClassification.Generated && !string.IsNullOrEmpty(GeneratedMapData))
							SendOrderTo(newConn, "GenerateMap", GeneratedMapData);

						SyncLobbyInfo();

						Log.Write("server", $"{client.Name} ({newConn.EndPoint}) has joined the game.");

						var otherConns = Conns.Where(c => c != newConn).ToArray();
						SendFluentMessage(otherConns.AsSpan(), Joined, "player", client.Name);

						if (Type == ServerType.Dedicated)
						{
							var motdFile = Path.Combine(Platform.SupportDir, "motd.txt");
							if (!File.Exists(motdFile))
								File.WriteAllText(motdFile, "Welcome, have fun and good luck!");

							var motd = File.ReadAllText(motdFile);
							if (!string.IsNullOrEmpty(motd))
								SendOrderTo(newConn, "Message", motd);

							if (!LobbyInfo.GlobalSettings.EnableSingleplayer)
								SendFluentMessageTo(newConn, TwoHumansRequired);
						}

						if (Type != ServerType.Local && (LobbyInfo.GlobalSettings.MapStatus & Session.MapStatus.UnsafeCustomRules) != 0)
							SendFluentMessageTo(newConn, CustomRules);
					}
				}

				if (!IsMultiplayer)
				{
					// Local servers can only be joined by the local client, so we can trust their identity without validation
					client.Fingerprint = handshake.Fingerprint;
					CompleteConnection();
				}
				else if (!string.IsNullOrEmpty(handshake.Fingerprint) && !string.IsNullOrEmpty(handshake.AuthSignature))
				{
					Task.Run(async () =>
					{
						PlayerProfile profile = null;

						try
						{
							var httpClient = HttpClientFactory.Create();
							var url = playerDatabase.Profile + handshake.Fingerprint;
							var httpResponseMessage = await httpClient.GetAsync(url);
							var result = await httpResponseMessage.Content.ReadAsStreamAsync();

							var yaml = MiniYaml.FromStream(result, url).First();
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
							var notAuthenticated = Type == ServerType.Dedicated && profile == null && (Settings.RequireAuthentication || Settings.ProfileIDWhitelist.Count > 0);
							var blacklisted = Type == ServerType.Dedicated && profile != null && Settings.ProfileIDBlacklist.Contains(profile.ProfileID);
							var notWhitelisted = Type == ServerType.Dedicated && Settings.ProfileIDWhitelist.Count > 0 &&
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
								CompleteConnection();
						}));
					});
				}
				else
				{
					if (Type == ServerType.Dedicated && (Settings.RequireAuthentication || Settings.ProfileIDWhitelist.Count > 0))
					{
						Log.Write("server", $"Rejected connection from {newConn.EndPoint}; Not authenticated.");
						SendOrderTo(newConn, "ServerError", RequiresAuthentication);
						DropClient(newConn);
					}
					else
						CompleteConnection();
				}
			}
			catch (Exception ex)
			{
				Log.Write("server", $"Dropping connection {newConn.EndPoint} because an error occurred:");
				Log.Write("server", ex.ToString());
				DropClient(newConn);
			}
		}

		public void SendOrderTo(Connection conn, string order, string data)
		{
			DispatchOrdersToClient(conn, 0, 0, Order.FromTargetString(order, data, true).Serialize());
		}

		public void SendFluentMessage(string key, params object[] args)
		{
			var conns = Conns.ToArray();
			SendFluentMessage(conns, key, args);
		}

		public void SendFluentMessage(ReadOnlySpan<Connection> conns, string key, params object[] args)
		{
			var text = FluentMessage.Serialize(key, args);
			DispatchServerOrdersToClients(conns, Order.FromTargetString("FluentMessage", text, true).Serialize());

			if (Type == ServerType.Dedicated)
				WriteLineWithTimeStamp(FluentProvider.GetMessage(key, args));
		}

		public void SendFluentMessageTo(Connection conn, string key, object[] args = null)
		{
			var text = FluentMessage.Serialize(key, args);
			DispatchOrdersToClient(conn, 0, 0, Order.FromTargetString("FluentMessage", text, true).Serialize());
		}

		void WriteLineWithTimeStamp(string line)
		{
			Console.WriteLine($"[{DateTime.Now.ToString(Settings.TimestampFormat, CultureInfo.CurrentCulture)}] {line}");
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

		public bool HasClientWonOrLost(Session.Client client) =>
			worldPlayers.FirstOrDefault(p => p?.ClientIndex == client.Index)?.Outcome != WinState.Undefined;

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
						SendFluentMessage(ObserverDisconnected, "player", dropClient.Name);
					else if (dropClient.Team > 0)
						SendFluentMessage(PlayerTeamDisconnected, "player", dropClient.Name, "team", dropClient.Team);
					else
						SendFluentMessage(PlayerDisconnected, "player", dropClient.Name);
				}
				else
					SendFluentMessage(LobbyDisconnected, "player", dropClient.Name);

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
						SendFluentMessage(NewAdmin, "player", nextAdmin.Name);
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
				var clientData = LobbyInfo.Clients.ConvertAll(client => client.Serialize());

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
				WriteLineWithTimeStamp(FluentProvider.GetMessage(GameStarted));

				// Drop any players who are not ready
				foreach (var c in Conns.Where(c => !c.Validated || GetClient(c).IsInvalid).ToArray())
				{
					SendOrderTo(c, "ServerError", YouWereKicked);
					DropClient(c);
				}

				// Enable game saves for singleplayer missions only
				// TODO: Enable for multiplayer (non-dedicated servers only) once the lobby UI has been created
				LobbyInfo.GlobalSettings.EnableGameSaves = Type != ServerType.Dedicated && LobbyInfo.NonBotClients.Count() == 1;

				// Player list for win/loss tracking
				// HACK: NonCombatant and non-Playable players are set to null to simplify replay tracking
				// The null padding is needed to keep the player indexes in sync with world.Players on the clients
				// This will need to change if future code wants to use worldPlayers for other purposes
				var playerRandom = new MersenneTwister(LobbyInfo.GlobalSettings.RandomSeed);
				foreach (var cmpi in Map.WorldActorInfo.TraitInfos<ICreatePlayersInfo>())
					cmpi.CreateServerPlayers(Map, LobbyInfo, worldPlayers, playerRandom);

				gameInfo = new GameInformation()
				{
					Mod = Game.ModData.Manifest.Id,
					Version = Game.ModData.Manifest.Metadata.Version,
					MapUid = Map.Uid,
					MapTitle = Map.Title,
					StartTimeUtc = DateTime.UtcNow,
				};

				if (Map.Class == MapClassification.Generated)
					gameInfo.MapGenerationArgs = Map.GenerationArgs;

				// Replay metadata should only include the playable players
				foreach (var p in worldPlayers)
					if (p != null)
						gameInfo.Players.Add(p);

				if (recorder != null)
					recorder.Metadata = new ReplayMetadata(gameInfo);

				SyncLobbyInfo();

				var gameSpeeds = Game.ModData.GetOrCreate<GameSpeeds>();
				var gameSpeedName = LobbyInfo.GlobalSettings.OptionOrDefault("gamespeed", gameSpeeds.DefaultSpeed);

				var gameSpeed = gameSpeeds.Speeds[gameSpeedName];

				orderBuffer = new OrderBuffer();
				orderBuffer.Start(gameSpeed, Conns.Where(c => c.Validated).Select(c => c.PlayerIndex));

				State = ServerState.GameStarted;

				if (IsMultiplayer)
					OrderLatency = gameSpeed.OrderLatency;

				LobbyInfo.GlobalSettings.GameTimestep = gameSpeed.Timestep;

				if (GameSave == null && LobbyInfo.GlobalSettings.EnableGameSaves)
					GameSave = new GameSave();

				var startGameData = "";
				if (GameSave != null)
				{
					GameSave.StartGame(LobbyInfo, Map);
					if (GameSave.LastOrdersFrame >= 0)
					{
						startGameData = new List<MiniYamlNode>()
						{
							new("SaveLastOrdersFrame", GameSave.LastOrdersFrame.ToStringInvariant()),
							new("SaveSyncFrame", GameSave.LastSyncFrame.ToStringInvariant())
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
						var frameData = CreateFrame(from.PlayerIndex, from.LastOrdersFrame, []);
						foreach (var to in conns)
							DispatchFrameToClient(to, from.PlayerIndex, frameData);

						RecordOrder(from.LastOrdersFrame, [], from.PlayerIndex);
						GameSave?.DispatchOrders(from, from.LastOrdersFrame, []);
					}
				}
			}
		}

		public bool InterpretCommand(string command, Connection conn)
		{
			foreach (var t in serverTraits.WithInterface<IInterpretCommand>())
				if (t.InterpretCommand(this, conn, GetClient(conn), command))
					return true;

			return false;
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

		public bool MapIsUnknown(string uid)
		{
			if (string.IsNullOrEmpty(uid))
				return true;

			var status = ModData.MapCache[uid].Status;
			return status != MapStatus.Available && status != MapStatus.DownloadAvailable;
		}

		public bool MapIsKnown(string uid)
		{
			if (string.IsNullOrEmpty(uid))
				return false;

			if (MapPool != null && !MapPool.Contains(uid))
				return false;

			var status = ModData.MapCache[uid].Status;
			return status == MapStatus.Available || status == MapStatus.DownloadAvailable;
		}

		interface IServerEvent { void Invoke(Server server); }

		sealed class ConnectionConnectEvent : IServerEvent
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

		sealed class ConnectionDisconnectEvent : IServerEvent
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

		sealed class ConnectionPacketEvent : IServerEvent
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

		sealed class ConnectionPingEvent : IServerEvent
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

		sealed class CallbackEvent : IServerEvent
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
