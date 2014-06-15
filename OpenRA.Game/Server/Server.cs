#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
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
	public enum ServerState : int
	{
		WaitingPlayers = 1,
		GameStarted = 2,
		ShuttingDown = 3
	}

	public class Server
	{
		public readonly IPAddress Ip;
		public readonly int Port;

		int randomSeed;
		public readonly MersenneTwister Random = new MersenneTwister();

		// Valid player connections
		public List<Connection> Conns = new List<Connection>();

		// Pre-verified player connections
		public List<Connection> PreConns = new List<Connection>();

		TcpListener listener = null;
		Dictionary<int, List<Connection>> inFlightFrames
			= new Dictionary<int, List<Connection>>();

		TypeDictionary serverTraits = new TypeDictionary();
		public Session LobbyInfo;

		public ServerSettings Settings;
		public ModData ModData;
		public Map Map;
		XTimer gameTimeout;

		public static void SyncClientToPlayerReference(Session.Client c, PlayerReference pr)
		{
			if (pr == null)
				return;
			if (pr.LockColor)
				c.Color = pr.Color;
			else
				c.Color = c.PreferredColor;
			if (pr.LockRace)
				c.Country = pr.Race;
			if (pr.LockSpawn)
				c.SpawnPoint = pr.Spawn;
			if (pr.LockTeam)
				c.Team = pr.Team;
		}

		protected volatile ServerState internalState = new ServerState();
		public ServerState State
		{
			get { return internalState; }
			protected set { internalState = value; }
		}

		public List<string> TempBans = new List<string>();

		public void Shutdown()
		{
			State = ServerState.ShuttingDown;
		}

		public void EndGame()
		{
			foreach (var t in serverTraits.WithInterface<IEndGame>())
				t.GameEnded(this);
		}

		public Server(IPEndPoint endpoint, ServerSettings settings, ModData modData)
		{
			Log.AddChannel("server", "server.log");

			internalState = ServerState.WaitingPlayers;
			listener = new TcpListener(endpoint);
			listener.Start();
			var localEndpoint = (IPEndPoint)listener.LocalEndpoint;
			Ip = localEndpoint.Address;
			Port = localEndpoint.Port;

			Settings = settings;
			ModData = modData;

			randomSeed = (int)DateTime.Now.ToBinary();

			if (Settings.AllowPortForward)
				UPnP.ForwardPort();

			foreach (var trait in modData.Manifest.ServerTraits)
				serverTraits.Add(modData.ObjectCreator.CreateObject<ServerTrait>(trait));

			LobbyInfo = new Session();
			LobbyInfo.GlobalSettings.RandomSeed = randomSeed;
			LobbyInfo.GlobalSettings.Map = settings.Map;
			LobbyInfo.GlobalSettings.ServerName = settings.Name;
			LobbyInfo.GlobalSettings.Dedicated = settings.Dedicated;
			FieldLoader.Load(LobbyInfo.GlobalSettings, modData.Manifest.LobbyDefaults);

			foreach (var t in serverTraits.WithInterface<INotifyServerStart>())
				t.ServerStarted(this);

			Log.Write("server", "Initial mod: {0}", ModData.Manifest.Mod.Id);
			Log.Write("server", "Initial map: {0}", LobbyInfo.GlobalSettings.Map);

			new Thread(_ =>
			{
				var timeout = serverTraits.WithInterface<ITick>().Min(t => t.TickTimeout);
				for (;;)
				{
					var checkRead = new List<Socket>();
					checkRead.Add(listener.Server);
					foreach (var c in Conns) checkRead.Add(c.socket);
					foreach (var c in PreConns) checkRead.Add(c.socket);

					Socket.Select(checkRead, null, null, timeout);
					if (State == ServerState.ShuttingDown)
					{
						EndGame();
						break;
					}

					foreach (var s in checkRead)
						if (s == listener.Server) AcceptConnection();
						else if (PreConns.Count > 0)
						{
							var p = PreConns.SingleOrDefault(c => c.socket == s);
							if (p != null) p.ReadData(this);
						}
						else if (Conns.Count > 0)
						{
							var conn = Conns.SingleOrDefault(c => c.socket == s);
							if (conn != null) conn.ReadData(this);
						}

					foreach (var t in serverTraits.WithInterface<ITick>())
						t.Tick(this);

					if (State == ServerState.ShuttingDown)
					{
						EndGame();
						if (Settings.AllowPortForward) UPnP.RemovePortforward();
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
		int nextPlayerIndex = 0;
		public int ChooseFreePlayerIndex()
		{
			return nextPlayerIndex++;
		}

		void AcceptConnection()
		{
			Socket newSocket = null;

			try
			{
				if (!listener.Server.IsBound) return;
				newSocket = listener.AcceptSocket();
			}
			catch (Exception e)
			{
				/* TODO: Could have an exception here when listener 'goes away' when calling AcceptConnection! */
				/* Alternative would be to use locking but the listener doesnt go away without a reason. */
				Log.Write("server", "Accepting the connection failed.", e);
				return;
			}

			var newConn = new Connection { socket = newSocket };
			try
			{
				newConn.socket.Blocking = false;
				newConn.socket.NoDelay = true;

				// assign the player number.
				newConn.PlayerIndex = ChooseFreePlayerIndex();
				SendData(newConn.socket, BitConverter.GetBytes(ProtocolVersion.Version));
				SendData(newConn.socket, BitConverter.GetBytes(newConn.PlayerIndex));
				PreConns.Add(newConn);

				// Dispatch a handshake order
				var request = new HandshakeRequest()
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
				Log.Write("server", "Dropping client {0} because handshake failed: {1}", newConn.PlayerIndex.ToString(), e);
			}
		}

		void ValidateClient(Connection newConn, string data)
		{
			try
			{
				if (State == ServerState.GameStarted)
				{
					Log.Write("server", "Rejected connection from {0}; game is already started.",
						newConn.socket.RemoteEndPoint);

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

				var client = new Session.Client()
				{
					Name = handshake.Client.Name,
					IpAddress = ((IPEndPoint)newConn.socket.RemoteEndPoint).Address.ToString(),
					Index = newConn.PlayerIndex,
					Slot = LobbyInfo.FirstEmptySlot(),
					PreferredColor = handshake.Client.Color,
					Color = handshake.Client.Color,
					Country = "random",
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
					SyncClientToPlayerReference(client, Map.Players[client.Slot]);
				else
					client.Color = HSLColor.FromRGB(255, 255, 255);

				if (ModData.Manifest.Mod.Id != handshake.Mod)
				{
					Log.Write("server", "Rejected connection from {0}; mods do not match.",
						newConn.socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "Server is running an incompatible mod");
					DropClient(newConn);
					return;
				}

				if (ModData.Manifest.Mod.Version != handshake.Version && !LobbyInfo.GlobalSettings.AllowVersionMismatch)
				{
					Log.Write("server", "Rejected connection from {0}; Not running the same version.",
						newConn.socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "Server is running an incompatible version");
					DropClient(newConn);
					return;
				}

				// Check if IP is banned
				var bans = Settings.Ban.Union(TempBans);
				if (bans.Contains(client.IpAddress))
				{
					Log.Write("server", "Rejected connection from {0}; Banned.", newConn.socket.RemoteEndPoint);
					SendOrderTo(newConn, "ServerError", "You have been {0} from the server".F(Settings.Ban.Contains(client.IpAddress) ? "banned" : "temporarily banned"));
					DropClient(newConn);
					return;
				}

				// Promote connection to a valid client
				PreConns.Remove(newConn);
				Conns.Add(newConn);
				LobbyInfo.Clients.Add(client);
				var clientPing = new Session.ClientPing();
				clientPing.Index = client.Index;
				LobbyInfo.ClientPings.Add(clientPing);

				Log.Write("server", "Client {0}: Accepted connection from {1}.",
				          newConn.PlayerIndex, newConn.socket.RemoteEndPoint);

				foreach (var t in serverTraits.WithInterface<IClientJoined>())
					t.ClientJoined(this, newConn);

				SyncLobbyInfo();
				SendMessage("{0} has joined the server.".F(client.Name));

				// Send initial ping
				SendOrderTo(newConn, "Ping", Environment.TickCount.ToString());

				if (Settings.Dedicated)
				{
					var motdFile = Path.Combine(Platform.SupportDir, "motd.txt");
					if (!File.Exists(motdFile))
						System.IO.File.WriteAllText(motdFile, "Welcome, have fun and good luck!");
					var motd = System.IO.File.ReadAllText(motdFile);
					if (!string.IsNullOrEmpty(motd))
						SendOrderTo(newConn, "Message", motd);
				}

				if (handshake.Mod == "{DEV_VERSION}")
					SendMessage("{0} is running an unversioned development build, ".F(client.Name) +
					"and may desynchronize the game state if they have incompatible rules.");

				SetOrderLag();
			}
			catch (Exception) { DropClient(newConn); }
		}

		void SetOrderLag()
		{
			if (LobbyInfo.IsSinglePlayer)
				LobbyInfo.GlobalSettings.OrderLatency = 1;
			else
				LobbyInfo.GlobalSettings.OrderLatency = 3;

			SyncLobbyGlobalSettings();
		}

		public void UpdateInFlightFrames(Connection conn)
		{
			if (conn.Frame == 0)
				return;

			if (!inFlightFrames.ContainsKey(conn.Frame))
				inFlightFrames[conn.Frame] = new List<Connection> { conn };
			else
				inFlightFrames[conn.Frame].Add(conn);

			if (Conns.All(c => inFlightFrames[conn.Frame].Contains(c)))
				inFlightFrames.Remove(conn.Frame);
		}

		void DispatchOrdersToClient(Connection c, int client, int frame, byte[] data)
		{
			try
			{
				SendData(c.socket, BitConverter.GetBytes(data.Length + 4));
				SendData(c.socket, BitConverter.GetBytes(client));
				SendData(c.socket, BitConverter.GetBytes(frame));
				SendData(c.socket, data);
			}
			catch (Exception e)
			{
				DropClient(c);
				Log.Write("server", "Dropping client {0} because dispatching orders failed: {1}", client.ToString(), e);
			}
		}

		public void DispatchOrdersToClients(Connection conn, int frame, byte[] data)
		{
			var from = conn != null ? conn.PlayerIndex : 0;
			foreach (var c in Conns.Except(conn).ToArray())
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
				for (;;)
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
		}

		void InterpretServerOrder(Connection conn, ServerOrder so)
		{
			switch (so.Name)
			{
				case "Command":
				{
					bool handled = false;
					foreach (var t in serverTraits.WithInterface<IInterpretCommand>())
						if (handled = t.InterpretCommand(this, conn, GetClient(conn), so.Data))
							break;

					if (!handled)
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

					var pingFromClient = LobbyInfo.PingFromClient(GetClient(conn));
					if (pingFromClient == null)
						return;

					var history = pingFromClient.LatencyHistory.ToList();
					history.Add(Environment.TickCount - pingSent);

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
			if (PreConns.Contains(toDrop))
				PreConns.Remove(toDrop);
			else
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
				if (LobbyInfo.GlobalSettings.Dedicated && dropClient.IsAdmin && State == ServerState.WaitingPlayers)
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

				if (!Conns.Any())
				{
					FieldLoader.Load(LobbyInfo.GlobalSettings, ModData.Manifest.LobbyDefaults);
					TempBans.Clear();
				}

				if (Conns.Any() || LobbyInfo.GlobalSettings.Dedicated)
					SyncLobbyClients();

				if (!LobbyInfo.GlobalSettings.Dedicated && dropClient.IsAdmin)
					Shutdown();
			}

			try
			{
				toDrop.socket.Disconnect(false);
			}
			catch { }

			SetOrderLag();
		}

		public void SyncLobbyInfo()
		{
			if (State == ServerState.WaitingPlayers) // Don't do this while the game is running, it breaks things!
				DispatchOrders(null, 0,
					new ServerOrder("SyncInfo", LobbyInfo.Serialize()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncLobbyClients()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			// TODO: only need to sync the specific client that has changed to avoid conflicts
			var clientData = new List<MiniYamlNode>();
			foreach (var client in LobbyInfo.Clients)
				clientData.Add(client.Serialize());

			DispatchOrders(null, 0,
				new ServerOrder("SyncLobbyClients", clientData.WriteToString()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncLobbySlots()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			// TODO: don't sync all the slots if just one changed
			var slotData = new List<MiniYamlNode>();
			foreach (var slot in LobbyInfo.Slots)
				slotData.Add(slot.Value.Serialize());

			DispatchOrders(null, 0,
				new ServerOrder("SyncLobbySlots", slotData.WriteToString()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncLobbyGlobalSettings()
		{
			if (State != ServerState.WaitingPlayers)
				return;

			var sessionData = new List<MiniYamlNode>();
			sessionData.Add(LobbyInfo.GlobalSettings.Serialize());

			DispatchOrders(null, 0,
				new ServerOrder("SyncLobbyGlobalSettings", sessionData.WriteToString()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void SyncClientPing()
		{
			// TODO: split this further into per client ping orders
			var clientPings = new List<MiniYamlNode>();
			foreach (var ping in LobbyInfo.ClientPings)
				clientPings.Add(ping.Serialize());

			DispatchOrders(null, 0,
				new ServerOrder("SyncClientPings", clientPings.WriteToString()).Serialize());

			foreach (var t in serverTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void StartGame()
		{
			listener.Stop();

			Console.WriteLine("Game started");

			// Drop any unvalidated clients
			foreach (var c in PreConns.ToArray())
				DropClient(c);

			// Drop any players who are not ready
			foreach (var c in Conns.ToArray())
			{
				if (GetClient(c).IsInvalid)
				{
					SendOrderTo(c, "ServerError", "You have been kicked from the server");
					DropClient(c);
				}
			}

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

		static void SendData(Socket s, byte[] data)
		{
			var start = 0;
			var length = data.Length;
			SocketError error;

			// Non-blocking sends are free to send only part of the data
			while (start < length)
			{
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
	}
}
