#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
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

	public class Server
	{
		public readonly string TwoHumansRequiredText = "This server requires at least two human players to start a match.";

		public readonly IPAddress Ip;
		public readonly int Port;
		public readonly MersenneTwister Random = new MersenneTwister();
		public readonly bool Dedicated;

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

		readonly int randomSeed;
		readonly TcpListener listener;
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

		public Server(IPEndPoint endpoint, ServerSettings settings, ModData modData, bool dedicated)
		{
			Log.AddChannel("server", "server.log");

			listener = new TcpListener(endpoint);
			listener.Start();
			var localEndpoint = (IPEndPoint)listener.LocalEndpoint;
			Ip = localEndpoint.Address;
			Port = localEndpoint.Port;
			Dedicated = dedicated;
			Settings = settings;

			Settings.Name = OpenRA.Settings.SanitizedServerName(Settings.Name);

			ModData = modData;

			playerDatabase = modData.Manifest.Get<PlayerDatabase>();

			randomSeed = (int)DateTime.Now.ToBinary();

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
					EnableSingleplayer = settings.EnableSingleplayer || !dedicated,
					GameUid = Guid.NewGuid().ToString(),
					Dedicated = dedicated
				}
			};

			new Thread(_ =>
			{
				foreach (var t in serverTraits.WithInterface<INotifyServerStart>())
					t.ServerStarted(this);

				Log.Write("server", "Initial mod: {0}", ModData.Manifest.Id);
				Log.Write("server", "Initial map: {0}", LobbyInfo.GlobalSettings.Map);

				for (;;)
				{
					var checkRead = new List<Socket>();
					if (State == ServerState.WaitingPlayers)
						checkRead.Add(listener.Server);

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
						if (s == listener.Server)
						{
							AcceptConnection();
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
				try { listener.Stop(); }
				catch { }
			}) { IsBackground = true }.Start();
		}

		int nextPlayerIndex;
		public int ChooseFreePlayerIndex()
		{
			return nextPlayerIndex++;
		}

		void AcceptConnection()
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
				SendData(newConn.Socket, BitConverter.GetBytes(ProtocolVersion.Version));
				SendData(newConn.Socket, BitConverter.GetBytes(newConn.PlayerIndex));
				PreConns.Add(newConn);

				// Dispatch a handshake order
				var request = new HandshakeRequest
				{
					Mod = ModData.Manifest.Id,
					Version = ModData.Manifest.Metadata.Version,
					Map = LobbyInfo.GlobalSettings.Map,
					AuthToken = token
				};

				DispatchOrdersToClient(newConn, 0, 0, new ServerOrder("HandshakeRequest", request.Serialize()).Serialize());
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

				var client = new Session.Client
				{
					Name = OpenRA.Settings.SanitizedPlayerName(handshake.Client.Name),
					IpAddress = ((IPEndPoint)newConn.Socket.RemoteEndPoint).Address.ToString(),
					Index = newConn.PlayerIndex,
					PreferredColor = handshake.Client.PreferredColor,
					Color = handshake.Client.Color,
					Faction = "Random",
					SpawnPoint = 0,
					Team = 0,
					State = Session.ClientState.Invalid,
					IsAdmin = !LobbyInfo.Clients.Any(c1 => c1.IsAdmin)
				};

				if (client.IsObserver && !LobbyInfo.GlobalSettings.AllowSpectators)
				{
					SendOrderTo(newConn, "ServerError", "The game is full");
					DropClient(newConn);
					return;
				}

				if (ModData.Manifest.Id != handshake.Mod)
				{
					Log.Write("server", "Rejected connection from {0}; mods do not match.",
						newConn.Socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "Server is running an incompatible mod");
					DropClient(newConn);
					return;
				}

				if (ModData.Manifest.Metadata.Version != handshake.Version && !LobbyInfo.GlobalSettings.AllowVersionMismatch)
				{
					Log.Write("server", "Rejected connection from {0}; Not running the same version.",
						newConn.Socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "Server is running an incompatible version");
					DropClient(newConn);
					return;
				}

				// Check if IP is banned
				var bans = Settings.Ban.Union(TempBans);
				if (bans.Contains(client.IpAddress))
				{
					Log.Write("server", "Rejected connection from {0}; Banned.", newConn.Socket.RemoteEndPoint);
					SendOrderTo(newConn, "ServerError", "You have been {0} from the server".F(Settings.Ban.Contains(client.IpAddress) ? "banned" : "temporarily banned"));
					DropClient(newConn);
					return;
				}

				Action completeConnection = () =>
				{
					client.Slot = LobbyInfo.FirstEmptySlot();

					if (client.Slot != null)
						SyncClientToPlayerReference(client, Map.Players.Players[client.Slot]);
					else
						client.Color = HSLColor.FromRGB(255, 255, 255);

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

					if (Dedicated)
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

				if (!string.IsNullOrEmpty(handshake.Fingerprint) && !string.IsNullOrEmpty(handshake.AuthSignature))
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
										Log.Write("server", "{0} failed to authenticate as {1} (key revoked)", newConn.Socket.RemoteEndPoint, handshake.Fingerprint);
									else
										Log.Write("server", "{0} failed to authenticate as {1} (signature verification failed)",
											newConn.Socket.RemoteEndPoint, handshake.Fingerprint);
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
							var notAuthenticated = Dedicated && profile == null && (Settings.RequireAuthentication || Settings.ProfileIDWhitelist.Any());
							var blacklisted = Dedicated && profile != null && Settings.ProfileIDBlacklist.Contains(profile.ProfileID);
							var notWhitelisted = Dedicated && Settings.ProfileIDWhitelist.Any() &&
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
					if (Dedicated && (Settings.RequireAuthentication || Settings.ProfileIDWhitelist.Any()))
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
				SendData(c.Socket, BitConverter.GetBytes(data.Length + 4));
				SendData(c.Socket, BitConverter.GetBytes(client));
				SendData(c.Socket, BitConverter.GetBytes(frame));
				SendData(c.Socket, data);
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
		}

		void InterpretServerOrders(Connection conn, byte[] data)
		{
			var ms = new MemoryStream(data);
			var br = new BinaryReader(ms);

			try
			{
				while (ms.Position < ms.Length)
				{
					var so = ServerOrder.Deserialize(br);
					if (so == null) return;
					InterpretServerOrder(conn, so);
				}
			}
			catch (EndOfStreamException) { }
			catch (NotImplementedException) { }
		}

		public void SendOrderTo(Connection conn, string order, string data)
		{
			DispatchOrdersToClient(conn, 0, 0, new ServerOrder(order, data).Serialize());
		}

		public void SendMessage(string text, Connection conn = null)
		{
			DispatchOrdersToClients(conn, 0, new ServerOrder("Message", text).Serialize());

			if (Dedicated)
				Console.WriteLine("[{0}] {1}".F(DateTime.Now.ToString(Settings.TimestampFormat), text));
		}

		void InterpretServerOrder(Connection conn, ServerOrder so)
		{
			// Only accept handshake responses from unvalidated clients
			// Anything else may be an attempt to exploit the server
			if (!conn.Validated)
			{
				if (so.Name == "HandshakeResponse")
					ValidateClient(conn, so.Data);
				else
				{
					Log.Write("server", "Rejected connection from {0}; Order `{1}` is not a `HandshakeResponse`.",
						conn.Socket.RemoteEndPoint, so.Name);

					DropClient(conn);
				}

				return;
			}

			switch (so.Name)
			{
				case "Command":
					{
						var handledBy = serverTraits.WithInterface<IInterpretCommand>()
							.FirstOrDefault(t => t.InterpretCommand(this, conn, GetClient(conn), so.Data));

						if (handledBy == null)
						{
							Log.Write("server", "Unknown server command: {0}", so.Data);
							SendOrderTo(conn, "Message", "Unknown server command: {0}".F(so.Data));
						}

						break;
					}

				case "Chat":
				case "TeamChat":
				case "PauseGame":
					DispatchOrdersToClients(conn, 0, so.Serialize());
					break;
				case "Pong":
					{
						long pingSent;
						if (!OpenRA.Exts.TryParseInt64Invariant(so.Data, out pingSent))
						{
							Log.Write("server", "Invalid order pong payload: {0}", so.Data);
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
				DispatchOrdersToClients(toDrop, 0, new ServerOrder("Disconnected", "").Serialize());

				LobbyInfo.Clients.RemoveAll(c => c.Index == toDrop.PlayerIndex);
				LobbyInfo.ClientPings.RemoveAll(p => p.Index == toDrop.PlayerIndex);

				// Client was the server admin
				// TODO: Reassign admin for game in progress via an order
				if (Dedicated && dropClient.IsAdmin && State == ServerState.WaitingPlayers)
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

				DispatchOrders(toDrop, toDrop.MostRecentFrame, new byte[] { 0xbf });

				// All clients have left: clean up
				if (!Conns.Any())
					foreach (var t in serverTraits.WithInterface<INotifyServerEmpty>())
						t.ServerEmpty(this);

				if (Conns.Any() || Dedicated)
					SyncLobbyClients();

				if (!Dedicated && dropClient.IsAdmin)
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
				DispatchOrders(null, 0, new ServerOrder("SyncInfo", LobbyInfo.Serialize()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncLobbyClients()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			// TODO: Only need to sync the specific client that has changed to avoid conflicts!
			var clientData = LobbyInfo.Clients.Select(client => client.Serialize()).ToList();

			DispatchOrders(null, 0, new ServerOrder("SyncLobbyClients", clientData.WriteToString()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncLobbySlots()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			// TODO: Don't sync all the slots if just one changed!
			var slotData = LobbyInfo.Slots.Select(slot => slot.Value.Serialize()).ToList();

			DispatchOrders(null, 0, new ServerOrder("SyncLobbySlots", slotData.WriteToString()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncLobbyGlobalSettings()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			var sessionData = new List<MiniYamlNode> { LobbyInfo.GlobalSettings.Serialize() };

			DispatchOrders(null, 0, new ServerOrder("SyncLobbyGlobalSettings", sessionData.WriteToString()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncClientPing()
		{
			// TODO: Split this further into per client ping orders
			var clientPings = LobbyInfo.ClientPings.Select(ping => ping.Serialize()).ToList();

			// Note that syncing pings doesn't trigger INotifySyncLobbyInfo
			DispatchOrders(null, 0, new ServerOrder("SyncClientPings", clientPings.WriteToString()).Serialize());
		}

		public void StartGame()
		{
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

			SyncLobbyInfo();
			State = ServerState.GameStarted;

			foreach (var c in Conns)
				foreach (var d in Conns)
					DispatchOrdersToClient(c, d.PlayerIndex, 0x7FFFFFFF, new byte[] { 0xBF });

			DispatchOrders(null, 0,
				new ServerOrder("StartGame", "").Serialize());

			foreach (var t in serverTraits.WithInterface<IStartGame>())
				t.GameStarted(this);
		}
	}
}
