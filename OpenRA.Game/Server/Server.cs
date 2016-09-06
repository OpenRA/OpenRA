#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using System.Threading;
using OpenRA.Graphics;
using OpenRA.Network;
using OpenRA.Primitives;
using OpenRA.Support;

using XTimer = System.Timers.Timer;

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

		XTimer gameTimeout;

		protected volatile ServerState internalState = ServerState.WaitingPlayers;

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

			randomSeed = (int)DateTime.Now.ToBinary();

			if (Settings.AllowPortForward)
				UPnP.ForwardPort(Settings.ListenPort, Settings.ExternalPort).Wait();

			foreach (var trait in modData.Manifest.ServerTraits)
				serverTraits.Add(modData.ObjectCreator.CreateObject<ServerTrait>(trait));

			LobbyInfo = new Session
			{
				GlobalSettings =
				{
					RandomSeed = randomSeed,
					Map = settings.Map,
					ServerName = settings.Name,
					EnableSingleplayer = settings.EnableSingleplayer || !dedicated,
				}
			};

			new Thread(_ =>
			{
				foreach (var t in serverTraits.WithInterface<INotifyServerStart>())
					t.ServerStarted(this);

				Log.Write("server", "Initial mod: {0}", ModData.Manifest.Mod.Id);
				Log.Write("server", "Initial map: {0}", LobbyInfo.GlobalSettings.Map);

				var timeout = serverTraits.WithInterface<ITick>().Min(t => t.TickTimeout);
				for (;;)
				{
					var checkRead = new List<Socket>();
					if (State == ServerState.WaitingPlayers)
						checkRead.Add(listener.Server);

					checkRead.AddRange(Conns.Select(c => c.Socket));
					checkRead.AddRange(PreConns.Select(c => c.Socket));

					if (checkRead.Count > 0)
						Socket.Select(checkRead, null, null, timeout);

					if (State == ServerState.ShuttingDown)
					{
						EndGame();
						break;
					}

					foreach (var s in checkRead)
					{
						if (s == listener.Server)
							AcceptConnection();
						else if (PreConns.Count > 0)
						{
							var p = PreConns.SingleOrDefault(c => c.Socket == s);
							if (p != null) p.ReadData(this);
						}
						else if (Conns.Count > 0)
						{
							var conn = Conns.SingleOrDefault(c => c.Socket == s);
							if (conn != null) conn.ReadData(this);
						}
					}

					foreach (var t in serverTraits.WithInterface<ITick>())
						t.Tick(this);

					if (State == ServerState.ShuttingDown)
					{
						EndGame();
						if (Settings.AllowPortForward)
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

		/* lobby rework TODO:
		 *	- "teams together" option for team games -- will eliminate most need
		 *		for manual spawnpoint choosing.
		 */
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

				// assign the player number.
				newConn.PlayerIndex = ChooseFreePlayerIndex();
				SendData(newConn.Socket, BitConverter.GetBytes(ProtocolVersion.Version));
				SendData(newConn.Socket, BitConverter.GetBytes(newConn.PlayerIndex));
				PreConns.Add(newConn);

				// Dispatch a handshake order
				var request = new HandshakeRequest
				{
					Mod = ModData.Manifest.Mod.Id,
					Version = ModData.Manifest.Mod.Version,
					Map = LobbyInfo.GlobalSettings.Map
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
					Slot = LobbyInfo.FirstEmptySlot(),
					PreferredColor = handshake.Client.Color,
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

				if (client.Slot != null)
					SyncClientToPlayerReference(client, Map.Players.Players[client.Slot]);
				else
					client.Color = HSLColor.FromRGB(255, 255, 255);

				if (ModData.Manifest.Mod.Id != handshake.Mod)
				{
					Log.Write("server", "Rejected connection from {0}; mods do not match.",
						newConn.Socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "Server is running an incompatible mod");
					DropClient(newConn);
					return;
				}

				if (ModData.Manifest.Mod.Version != handshake.Version && !LobbyInfo.GlobalSettings.AllowVersionMismatch)
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

				// Promote connection to a valid client
				PreConns.Remove(newConn);
				Conns.Add(newConn);
				LobbyInfo.Clients.Add(client);
				var clientPing = new Session.ClientPing { Index = client.Index };
				LobbyInfo.ClientPings.Add(clientPing);

				Log.Write("server", "Client {0}: Accepted connection from {1}.",
					newConn.PlayerIndex, newConn.Socket.RemoteEndPoint);

				foreach (var t in serverTraits.WithInterface<IClientJoined>())
					t.ClientJoined(this, newConn);

				SyncLobbyInfo();

				Log.Write("server", "{0} ({1}) has joined the game.",
					client.Name, newConn.Socket.RemoteEndPoint);

				if (Dedicated || !LobbyInfo.IsSinglePlayer)
					SendMessage("{0} has joined the game.".F(client.Name));

				// Send initial ping
				SendOrderTo(newConn, "Ping", Game.RunTime.ToString(CultureInfo.InvariantCulture));

				if (Dedicated)
				{
					var motdFile = Platform.ResolvePath("^", "motd.txt");
					if (!File.Exists(motdFile))
						File.WriteAllText(motdFile, "Welcome, have fun and good luck!");

					var motd = File.ReadAllText(motdFile);
					if (!string.IsNullOrEmpty(motd))
						SendOrderTo(newConn, "Message", motd);
				}

				if (!LobbyInfo.IsSinglePlayer && Map.DefinesUnsafeCustomRules)
					SendOrderTo(newConn, "Message", "This map contains custom rules. Game experience may change.");

				if (!LobbyInfo.GlobalSettings.EnableSingleplayer)
					SendOrderTo(newConn, "Message", "This server requires at least two human players to start a match.");
				else if (Map.Players.Players.Where(p => p.Value.Playable).All(p => !p.Value.AllowBots))
					SendOrderTo(newConn, "Message", "Bots have been disabled on this map.");

				if (handshake.Mod == "{DEV_VERSION}")
					SendMessage("{0} is running an unversioned development build, ".F(client.Name) +
						"and may desynchronize the game state if they have incompatible rules.");
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

		public void SendMessage(string text)
		{
			DispatchOrdersToClients(null, 0, new ServerOrder("Message", text).Serialize());

			if (Dedicated)
				Console.WriteLine("[{0}] {1}".F(DateTime.Now.ToString(Settings.TimestampFormat), text));
		}

		void InterpretServerOrder(Connection conn, ServerOrder so)
		{
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

				case "HandshakeResponse":
					ValidateClient(conn, so.Data);
					break;
				case "Chat":
				case "TeamChat":
				case "PauseGame":
					DispatchOrdersToClients(conn, 0, so.Serialize());
					break;
				case "Pong":
					{
						int pingSent;
						if (!OpenRA.Exts.TryParseIntegerInvariant(so.Data, out pingSent))
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
			DropClient(toDrop, toDrop.MostRecentFrame);
		}

		public void DropClient(Connection toDrop, int frame)
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

				DispatchOrders(toDrop, frame, new byte[] { 0xbf });

				if (!Conns.Any())
					TempBans.Clear();

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

			DispatchOrders(null, 0, new ServerOrder("SyncClientPings", clientPings.WriteToString()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
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
			if (LobbyInfo.IsSinglePlayer)
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

			// Check TimeOut
			if (Settings.TimeOut > 10000)
			{
				gameTimeout = new XTimer(Settings.TimeOut);
				gameTimeout.Elapsed += (_, e) =>
				{
					Console.WriteLine("Timeout at {0}!!!", e.SignalTime);
					Environment.Exit(0);
				};
				gameTimeout.Enabled = true;
			}
		}
	}
}
