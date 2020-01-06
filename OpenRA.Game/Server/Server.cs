#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;

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

	public class Server
	{
		public readonly string TwoHumansRequiredText = "This server requires at least two human players to start a match.";

		public readonly MersenneTwister Random = new MersenneTwister();
		public readonly ServerType Type;

		// Valid player connections
		public List<Connection> Conns = new List<Connection>();

		// Pre-verified player connections
		public List<Connection> PreConns = new List<Connection>();

		public Session LobbyInfo;
		public ServerSettings Settings;
		public ModData ModData;
		public List<string> TempBans = new List<string>();

		// Managed by LobbyCommands
		public MapPreview Map;
		public GameSave GameSave = null;

		readonly int randomSeed;
		readonly List<TcpListener> listeners = new List<TcpListener>();
		readonly TypeDictionary serverTraits = new TypeDictionary();
		readonly PlayerDatabase playerDatabase;

		protected volatile ServerState internalState = ServerState.WaitingPlayers;

		volatile ActionQueue delayedActions = new ActionQueue();
		int waitingForAuthenticationCallback = 0;

		public ServerState State
		{
			get { return internalState; }
			protected set { internalState = value; }
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

			c.Color = pr.LockColor ? pr.Color : c.PreferredColor;
		}

		static void SendData(Socket s, byte[] data)
		{
			var start = 0;
			var length = data.Length;

			// Non-blocking sends are free to send only part of the data
			while (start < length)
			{
				SocketError error;
				var sent = s.Send(data, start, length - start, SocketFlags.None, out error);
				if (error == SocketError.WouldBlock)
				{
					Log.Write("server", "Non-blocking send of {0} bytes failed. Falling back to blocking send.", length - start);
					s.Blocking = true;
					sent = s.Send(data, start, length - start, SocketFlags.None);
					s.Blocking = false;
				}
				else if (error != SocketError.Success)
					throw new SocketException((int)error);

				start += sent;
			}
		}

		public void Shutdown()
		{
			State = ServerState.ShuttingDown;
		}

		public void EndGame()
		{
			foreach (var t in serverTraits.WithInterface<IEndGame>())
				t.GameEnded(this);
		}

		public Server(List<IPEndPoint> endpoints, ServerSettings settings, ModData modData, ServerType type)
		{
			Log.AddChannel("server", "server.log", true);

			SocketException lastException = null;
			var checkReadServer = new List<Socket>();
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
							Log.Write("server", "Failed to set socket option on {0}: {1}", endpoint.ToString(), ex.Message);
						else
							throw;
					}

					listener.Start();
					listeners.Add(listener);
					checkReadServer.Add(listener.Server);
				}
				catch (SocketException ex)
				{
					lastException = ex;
					Log.Write("server", "Failed to listen on {0}: {1}", endpoint.ToString(), ex.Message);
				}
			}

			if (listeners.Count == 0)
				throw lastException;

			Type = type;
			Settings = settings;

			Settings.Name = OpenRA.Settings.SanitizedServerName(Settings.Name);

			ModData = modData;

			playerDatabase = modData.Manifest.Get<PlayerDatabase>();

			randomSeed = (int)DateTime.Now.ToBinary();

			if (type != ServerType.Local && settings.EnableGeoIP)
				GeoIP.Initialize();

			if (UPnP.Status == UPnPStatus.Enabled)
				UPnP.ForwardPort(Settings.ListenPort, Settings.ListenPort).Wait();

			foreach (var trait in modData.Manifest.ServerTraits)
				serverTraits.Add(modData.ObjectCreator.CreateObject<ServerTrait>(trait));

			serverTraits.TrimExcess();

			LobbyInfo = new Session
			{
				GlobalSettings =
				{
					RandomSeed = randomSeed,
					Map = settings.Map,
					ServerName = settings.Name,
					EnableSingleplayer = settings.EnableSingleplayer || Type != ServerType.Dedicated,
					EnableSyncReports = settings.EnableSyncReports,
					GameUid = Guid.NewGuid().ToString(),
					Dedicated = Type == ServerType.Dedicated
				}
			};

			new Thread(_ =>
			{
				foreach (var t in serverTraits.WithInterface<INotifyServerStart>())
					t.ServerStarted(this);

				Log.Write("server", "Initial mod: {0}", ModData.Manifest.Id);
				Log.Write("server", "Initial map: {0}", LobbyInfo.GlobalSettings.Map);

				while (true)
				{
					var checkRead = new List<Socket>();
					if (State == ServerState.WaitingPlayers)
						checkRead.AddRange(checkReadServer);

					checkRead.AddRange(Conns.Select(c => c.Socket));
					checkRead.AddRange(PreConns.Select(c => c.Socket));

					// Block for at most 1 second in order to guarantee a minimum tick rate for ServerTraits
					// Decrease this to 100ms to improve responsiveness if we are waiting for an authentication query
					var localTimeout = waitingForAuthenticationCallback > 0 ? 100000 : 1000000;
					if (checkRead.Count > 0)
						Socket.Select(checkRead, null, null, localTimeout);

					if (State == ServerState.ShuttingDown)
					{
						EndGame();
						break;
					}

					foreach (var s in checkRead)
					{
						var serverIndex = checkReadServer.IndexOf(s);
						if (serverIndex >= 0)
						{
							AcceptConnection(listeners[serverIndex]);
							continue;
						}

						var preConn = PreConns.SingleOrDefault(c => c.Socket == s);
						if (preConn != null)
						{
							preConn.ReadData(this);
							continue;
						}

						var conn = Conns.SingleOrDefault(c => c.Socket == s);
						if (conn != null)
							conn.ReadData(this);
					}

					delayedActions.PerformActions(0);

					// PERF: Dedicated servers need to drain the action queue to remove references blocking the GC from cleaning up disposed objects.
					if (Type == ServerType.Dedicated)
						Game.PerformDelayedActions();

					foreach (var t in serverTraits.WithInterface<ITick>())
						t.Tick(this);

					if (State == ServerState.ShuttingDown)
					{
						EndGame();
						if (UPnP.Status == UPnPStatus.Enabled)
							UPnP.RemovePortForward().Wait();
						break;
					}
				}

				foreach (var t in serverTraits.WithInterface<INotifyServerShutdown>())
					t.ServerShutdown(this);

				PreConns.Clear();
				Conns.Clear();

				foreach (var listener in listeners)
				{
					try { listener.Stop(); }
					catch { }
				}
			})
			{ IsBackground = true }.Start();
		}

		int nextPlayerIndex;
		public int ChooseFreePlayerIndex()
		{
			return nextPlayerIndex++;
		}

		void AcceptConnection(TcpListener listener)
		{
			Socket newSocket;

			try
			{
				if (!listener.Server.IsBound)
					return;

				newSocket = listener.AcceptSocket();
			}
			catch (Exception e)
			{
				/* TODO: Could have an exception here when listener 'goes away' when calling AcceptConnection! */
				/* Alternative would be to use locking but the listener doesn't go away without a reason. */
				Log.Write("server", "Accepting the connection failed.", e);
				return;
			}

			var newConn = new Connection { Socket = newSocket };
			try
			{
				newConn.Socket.Blocking = false;
				newConn.Socket.NoDelay = true;

				// Validate player identity by asking them to sign a random blob of data
				// which we can then verify against the player public key database
				var token = Convert.ToBase64String(OpenRA.Exts.MakeArray(256, _ => (byte)Random.Next()));

				// Assign the player number.
				newConn.PlayerIndex = ChooseFreePlayerIndex();
				newConn.AuthToken = token;

				// Send handshake and client index.
				var ms = new MemoryStream(8);
				ms.WriteArray(BitConverter.GetBytes(ProtocolVersion.Handshake));
				ms.WriteArray(BitConverter.GetBytes(newConn.PlayerIndex));
				SendData(newConn.Socket, ms.ToArray());

				PreConns.Add(newConn);

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
				DropClient(newConn);
				Log.Write("server", "Dropping client {0} because handshake failed: {1}", newConn.PlayerIndex.ToString(CultureInfo.InvariantCulture), e);
			}
		}

		void ValidateClient(Connection newConn, string data)
		{
			try
			{
				if (State == ServerState.GameStarted)
				{
					Log.Write("server", "Rejected connection from {0}; game is already started.",
						newConn.Socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "The game has already started");
					DropClient(newConn);
					return;
				}

				var handshake = HandshakeResponse.Deserialize(data);

				if (!string.IsNullOrEmpty(Settings.Password) && handshake.Password != Settings.Password)
				{
					var message = string.IsNullOrEmpty(handshake.Password) ? "Server requires a password" : "Incorrect password";
					SendOrderTo(newConn, "AuthenticationError", message);
					DropClient(newConn);
					return;
				}

				var ipAddress = ((IPEndPoint)newConn.Socket.RemoteEndPoint).Address;
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
					State = Session.ClientState.Invalid,
				};

				if (ModData.Manifest.Id != handshake.Mod)
				{
					Log.Write("server", "Rejected connection from {0}; mods do not match.",
						newConn.Socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "Server is running an incompatible mod");
					DropClient(newConn);
					return;
				}

				if (ModData.Manifest.Metadata.Version != handshake.Version)
				{
					Log.Write("server", "Rejected connection from {0}; Not running the same version.",
						newConn.Socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "Server is running an incompatible version");
					DropClient(newConn);
					return;
				}

				if (handshake.OrdersProtocol != ProtocolVersion.Orders)
				{
					Log.Write("server", "Rejected connection from {0}; incompatible Orders protocol version {1}.",
						newConn.Socket.RemoteEndPoint, handshake.OrdersProtocol);

					SendOrderTo(newConn, "ServerError", "Server is running an incompatible protocol");
					DropClient(newConn);
					return;
				}

				// Check if IP is banned
				var bans = Settings.Ban.Union(TempBans);
				if (bans.Contains(client.IPAddress))
				{
					Log.Write("server", "Rejected connection from {0}; Banned.", newConn.Socket.RemoteEndPoint);
					SendOrderTo(newConn, "ServerError", "You have been {0} from the server".F(Settings.Ban.Contains(client.IPAddress) ? "banned" : "temporarily banned"));
					DropClient(newConn);
					return;
				}

				Action completeConnection = () =>
				{
					client.Slot = LobbyInfo.FirstEmptySlot();
					client.IsAdmin = !LobbyInfo.Clients.Any(c1 => c1.IsAdmin);

					if (client.IsObserver && !LobbyInfo.GlobalSettings.AllowSpectators)
					{
						SendOrderTo(newConn, "ServerError", "The game is full");
						DropClient(newConn);
						return;
					}

					if (client.Slot != null)
						SyncClientToPlayerReference(client, Map.Players.Players[client.Slot]);
					else
						client.Color = Color.White;

					// Promote connection to a valid client
					PreConns.Remove(newConn);
					Conns.Add(newConn);
					LobbyInfo.Clients.Add(client);
					newConn.Validated = true;

					var clientPing = new Session.ClientPing { Index = client.Index };
					LobbyInfo.ClientPings.Add(clientPing);

					Log.Write("server", "Client {0}: Accepted connection from {1}.",
						newConn.PlayerIndex, newConn.Socket.RemoteEndPoint);

					if (client.Fingerprint != null)
						Log.Write("server", "Client {0}: Player fingerprint is {1}.",
							newConn.PlayerIndex, client.Fingerprint);

					foreach (var t in serverTraits.WithInterface<IClientJoined>())
						t.ClientJoined(this, newConn);

					SyncLobbyInfo();

					Log.Write("server", "{0} ({1}) has joined the game.",
						client.Name, newConn.Socket.RemoteEndPoint);

					// Report to all other players
					SendMessage("{0} has joined the game.".F(client.Name), newConn);

					// Send initial ping
					SendOrderTo(newConn, "Ping", Game.RunTime.ToString(CultureInfo.InvariantCulture));

					if (Type == ServerType.Dedicated)
					{
						var motdFile = Platform.ResolvePath(Platform.SupportDirPrefix, "motd.txt");
						if (!File.Exists(motdFile))
							File.WriteAllText(motdFile, "Welcome, have fun and good luck!");

						var motd = File.ReadAllText(motdFile);
						if (!string.IsNullOrEmpty(motd))
							SendOrderTo(newConn, "Message", motd);
					}

					if (Map.DefinesUnsafeCustomRules)
						SendOrderTo(newConn, "Message", "This map contains custom rules. Game experience may change.");

					if (!LobbyInfo.GlobalSettings.EnableSingleplayer)
						SendOrderTo(newConn, "Message", TwoHumansRequiredText);
					else if (Map.Players.Players.Where(p => p.Value.Playable).All(p => !p.Value.AllowBots))
						SendOrderTo(newConn, "Message", "Bots have been disabled on this map.");
				};

				if (Type == ServerType.Local)
				{
					// Local servers can only be joined by the local client, so we can trust their identity without validation
					client.Fingerprint = handshake.Fingerprint;
					completeConnection();
				}
				else if (!string.IsNullOrEmpty(handshake.Fingerprint) && !string.IsNullOrEmpty(handshake.AuthSignature))
				{
					waitingForAuthenticationCallback++;

					Action<DownloadDataCompletedEventArgs> onQueryComplete = i =>
					{
						PlayerProfile profile = null;

						if (i.Error == null)
						{
							try
							{
								var yaml = MiniYaml.FromString(Encoding.UTF8.GetString(i.Result)).First();
								if (yaml.Key == "Player")
								{
									profile = FieldLoader.Load<PlayerProfile>(yaml.Value);

									var publicKey = Encoding.ASCII.GetString(Convert.FromBase64String(profile.PublicKey));
									var parameters = CryptoUtil.DecodePEMPublicKey(publicKey);
									if (!profile.KeyRevoked && CryptoUtil.VerifySignature(parameters, newConn.AuthToken, handshake.AuthSignature))
									{
										client.Fingerprint = handshake.Fingerprint;
										Log.Write("server", "{0} authenticated as {1} (UID {2})", newConn.Socket.RemoteEndPoint,
											profile.ProfileName, profile.ProfileID);
									}
									else if (profile.KeyRevoked)
									{
										profile = null;
										Log.Write("server", "{0} failed to authenticate as {1} (key revoked)", newConn.Socket.RemoteEndPoint, handshake.Fingerprint);
									}
									else
									{
										profile = null;
										Log.Write("server", "{0} failed to authenticate as {1} (signature verification failed)",
											newConn.Socket.RemoteEndPoint, handshake.Fingerprint);
									}
								}
								else
									Log.Write("server", "{0} failed to authenticate as {1} (invalid server response: `{2}` is not `Player`)",
										newConn.Socket.RemoteEndPoint, handshake.Fingerprint, yaml.Key);
							}
							catch (Exception ex)
							{
								Log.Write("server", "{0} failed to authenticate as {1} (exception occurred)",
									newConn.Socket.RemoteEndPoint, handshake.Fingerprint);
								Log.Write("server", ex.ToString());
							}
						}
						else
							Log.Write("server", "{0} failed to authenticate as {1} (server error: `{2}`)",
								newConn.Socket.RemoteEndPoint, handshake.Fingerprint, i.Error);

						delayedActions.Add(() =>
						{
							var notAuthenticated = Type == ServerType.Dedicated && profile == null && (Settings.RequireAuthentication || Settings.ProfileIDWhitelist.Any());
							var blacklisted = Type == ServerType.Dedicated && profile != null && Settings.ProfileIDBlacklist.Contains(profile.ProfileID);
							var notWhitelisted = Type == ServerType.Dedicated && Settings.ProfileIDWhitelist.Any() &&
								(profile == null || !Settings.ProfileIDWhitelist.Contains(profile.ProfileID));

							if (notAuthenticated)
							{
								Log.Write("server", "Rejected connection from {0}; Not authenticated.", newConn.Socket.RemoteEndPoint);
								SendOrderTo(newConn, "ServerError", "Server requires players to have an OpenRA forum account");
								DropClient(newConn);
							}
							else if (blacklisted || notWhitelisted)
							{
								if (blacklisted)
									Log.Write("server", "Rejected connection from {0}; In server blacklist.", newConn.Socket.RemoteEndPoint);
								else
									Log.Write("server", "Rejected connection from {0}; Not in server whitelist.", newConn.Socket.RemoteEndPoint);

								SendOrderTo(newConn, "ServerError", "You do not have permission to join this server");
								DropClient(newConn);
							}
							else
								completeConnection();

							waitingForAuthenticationCallback--;
						}, 0);
					};

					new Download(playerDatabase.Profile + handshake.Fingerprint, _ => { }, onQueryComplete);
				}
				else
				{
					if (Type == ServerType.Dedicated && (Settings.RequireAuthentication || Settings.ProfileIDWhitelist.Any()))
					{
						Log.Write("server", "Rejected connection from {0}; Not authenticated.", newConn.Socket.RemoteEndPoint);
						SendOrderTo(newConn, "ServerError", "Server requires players to have an OpenRA forum account");
						DropClient(newConn);
					}
					else
						completeConnection();
				}
			}
			catch (Exception ex)
			{
				Log.Write("server", "Dropping connection {0} because an error occurred:", newConn.Socket.RemoteEndPoint);
				Log.Write("server", ex.ToString());
				DropClient(newConn);
			}
		}

		void DispatchOrdersToClient(Connection c, int client, int frame, byte[] data)
		{
			try
			{
				var ms = new MemoryStream(data.Length + 12);
				ms.WriteArray(BitConverter.GetBytes(data.Length + 4));
				ms.WriteArray(BitConverter.GetBytes(client));
				ms.WriteArray(BitConverter.GetBytes(frame));
				ms.WriteArray(data);
				SendData(c.Socket, ms.ToArray());
			}
			catch (Exception e)
			{
				DropClient(c);
				Log.Write("server", "Dropping client {0} because dispatching orders failed: {1}",
					client.ToString(CultureInfo.InvariantCulture), e);
			}
		}

		public void DispatchOrdersToClients(Connection conn, int frame, byte[] data)
		{
			var from = conn != null ? conn.PlayerIndex : 0;
			foreach (var c in Conns.Except(conn).ToList())
				DispatchOrdersToClient(c, from, frame, data);
		}

		public void DispatchOrders(Connection conn, int frame, byte[] data)
		{
			if (frame == 0 && conn != null)
				InterpretServerOrders(conn, data);
			else
				DispatchOrdersToClients(conn, frame, data);

			if (GameSave != null && conn != null)
				GameSave.DispatchOrders(conn, frame, data);
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

		public void SendMessage(string text, Connection conn = null)
		{
			DispatchOrdersToClients(conn, 0, Order.FromTargetString("Message", text, true).Serialize());

			if (Type == ServerType.Dedicated)
				Console.WriteLine("[{0}] {1}".F(DateTime.Now.ToString(Settings.TimestampFormat), text));
		}

		void InterpretServerOrder(Connection conn, Order o)
		{
			// Only accept handshake responses from unvalidated clients
			// Anything else may be an attempt to exploit the server
			if (!conn.Validated)
			{
				if (o.OrderString == "HandshakeResponse")
					ValidateClient(conn, o.TargetString);
				else
				{
					Log.Write("server", "Rejected connection from {0}; Order `{1}` is not a `HandshakeResponse`.",
						conn.Socket.RemoteEndPoint, o.OrderString);

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
							Log.Write("server", "Unknown server command: {0}", o.TargetString);
							SendOrderTo(conn, "Message", "Unknown server command: {0}".F(o.TargetString));
						}

						break;
					}

				case "Chat":
					DispatchOrdersToClients(conn, 0, o.Serialize());
					break;
				case "Pong":
					{
						long pingSent;
						if (!OpenRA.Exts.TryParseInt64Invariant(o.TargetString, out pingSent))
						{
							Log.Write("server", "Invalid order pong payload: {0}", o.TargetString);
							break;
						}

						var client = GetClient(conn);
						if (client == null)
							return;

						var pingFromClient = LobbyInfo.PingFromClient(client);
						if (pingFromClient == null)
							return;

						var history = pingFromClient.LatencyHistory.ToList();
						history.Add(Game.RunTime - pingSent);

						// Cap ping history at 5 values (25 seconds)
						if (history.Count > 5)
							history.RemoveRange(0, history.Count - 5);

						pingFromClient.Latency = history.Sum() / history.Count;
						pingFromClient.LatencyJitter = (history.Max() - history.Min()) / 2;
						pingFromClient.LatencyHistory = history.ToArray();

						SyncClientPing();

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

							var baseSavePath = Platform.ResolvePath(
								Platform.SupportDirPrefix,
								"Saves",
								ModData.Manifest.Id,
								ModData.Manifest.Metadata.Version);

							if (!Directory.Exists(baseSavePath))
								Directory.CreateDirectory(baseSavePath);

							GameSave.Save(Path.Combine(baseSavePath, filename));
							DispatchOrdersToClients(null, 0, Order.FromTargetString("GameSaved", filename, true).Serialize());
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

						var savePath = Platform.ResolvePath(
							Platform.SupportDirPrefix,
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
							{
								LobbyInfo.Clients.Remove(c);
								var ping = LobbyInfo.PingFromClient(c);
								if (ping != null)
									LobbyInfo.ClientPings.Remove(ping);
							}
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
						SyncClientPing();

						break;
					}
			}
		}

		public Session.Client GetClient(Connection conn)
		{
			return LobbyInfo.ClientWithIndex(conn.PlayerIndex);
		}

		public void DropClient(Connection toDrop)
		{
			if (!PreConns.Remove(toDrop))
			{
				Conns.Remove(toDrop);

				var dropClient = LobbyInfo.Clients.FirstOrDefault(c1 => c1.Index == toDrop.PlayerIndex);
				if (dropClient == null)
					return;

				var suffix = "";
				if (State == ServerState.GameStarted)
					suffix = dropClient.IsObserver ? " (Spectator)" : dropClient.Team != 0 ? " (Team {0})".F(dropClient.Team) : "";
				SendMessage("{0}{1} has disconnected.".F(dropClient.Name, suffix));

				// Send disconnected order, even if still in the lobby
				DispatchOrdersToClients(toDrop, 0, Order.FromTargetString("Disconnected", "", true).Serialize());

				LobbyInfo.Clients.RemoveAll(c => c.Index == toDrop.PlayerIndex);
				LobbyInfo.ClientPings.RemoveAll(p => p.Index == toDrop.PlayerIndex);

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
						SendMessage("{0} is now the admin.".F(nextAdmin.Name));
					}
				}

				DispatchOrders(toDrop, toDrop.MostRecentFrame, new[] { (byte)OrderType.Disconnect });

				// All clients have left: clean up
				if (!Conns.Any())
					foreach (var t in serverTraits.WithInterface<INotifyServerEmpty>())
						t.ServerEmpty(this);

				if (Conns.Any() || Type == ServerType.Dedicated)
					SyncLobbyClients();

				if (Type != ServerType.Dedicated && dropClient.IsAdmin)
					Shutdown();
			}

			try
			{
				toDrop.Socket.Disconnect(false);
			}
			catch { }
		}

		public void SyncLobbyInfo()
		{
			if (State == ServerState.WaitingPlayers) // Don't do this while the game is running, it breaks things!
				DispatchOrders(null, 0, Order.FromTargetString("SyncInfo", LobbyInfo.Serialize(), true).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncLobbyClients()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			// TODO: Only need to sync the specific client that has changed to avoid conflicts!
			var clientData = LobbyInfo.Clients.Select(client => client.Serialize()).ToList();

			DispatchOrders(null, 0, Order.FromTargetString("SyncLobbyClients", clientData.WriteToString(), true).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncLobbySlots()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			// TODO: Don't sync all the slots if just one changed!
			var slotData = LobbyInfo.Slots.Select(slot => slot.Value.Serialize()).ToList();

			DispatchOrders(null, 0, Order.FromTargetString("SyncLobbySlots", slotData.WriteToString(), true).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncLobbyGlobalSettings()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			var sessionData = new List<MiniYamlNode> { LobbyInfo.GlobalSettings.Serialize() };

			DispatchOrders(null, 0, Order.FromTargetString("SyncLobbyGlobalSettings", sessionData.WriteToString(), true).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncClientPing()
		{
			// TODO: Split this further into per client ping orders
			var clientPings = LobbyInfo.ClientPings.Select(ping => ping.Serialize()).ToList();

			// Note that syncing pings doesn't trigger INotifySyncLobbyInfo
			DispatchOrders(null, 0, Order.FromTargetString("SyncClientPings", clientPings.WriteToString(), true).Serialize());
		}

		public void StartGame()
		{
			foreach (var listener in listeners)
				listener.Stop();

			Console.WriteLine("[{0}] Game started", DateTime.Now.ToString(Settings.TimestampFormat));

			// Drop any unvalidated clients
			foreach (var c in PreConns.ToArray())
				DropClient(c);

			// Drop any players who are not ready
			foreach (var c in Conns.Where(c => GetClient(c).IsInvalid).ToArray())
			{
				SendOrderTo(c, "ServerError", "You have been kicked from the server!");
				DropClient(c);
			}

			// HACK: Turn down the latency if there is only one real player
			if (LobbyInfo.NonBotClients.Count() == 1)
				LobbyInfo.GlobalSettings.OrderLatency = 1;

			// Enable game saves for singleplayer missions only
			// TODO: Enable for multiplayer (non-dedicated servers only) once the lobby UI has been created
			LobbyInfo.GlobalSettings.GameSavesEnabled = Type != ServerType.Dedicated && LobbyInfo.NonBotClients.Count() == 1;

			SyncLobbyInfo();
			State = ServerState.GameStarted;

			foreach (var c in Conns)
				foreach (var d in Conns)
					DispatchOrdersToClient(c, d.PlayerIndex, 0x7FFFFFFF, new[] { (byte)OrderType.Disconnect });

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

			DispatchOrders(null, 0,
				Order.FromTargetString("StartGame", startGameData, true).Serialize());

			foreach (var t in serverTraits.WithInterface<IStartGame>())
				t.GameStarted(this);

			if (GameSave != null && GameSave.LastOrdersFrame >= 0)
			{
				GameSave.ParseOrders(LobbyInfo, (frame, client, data) =>
				{
					foreach (var c in Conns)
						DispatchOrdersToClient(c, client, frame, data);
				});
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
	}
}
