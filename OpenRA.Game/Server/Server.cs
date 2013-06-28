#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;

using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Network;

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
		// Valid player connections
		public List<Connection> conns = new List<Connection>();

		// Pre-verified player connections
		public List<Connection> preConns = new List<Connection>();

		TcpListener listener = null;
		Dictionary<int, List<Connection>> inFlightFrames
			= new Dictionary<int, List<Connection>>();

		TypeDictionary ServerTraits = new TypeDictionary();
		public Session lobbyInfo;

		public readonly IPAddress Ip;
		public readonly int Port;

		int randomSeed;
		public readonly Thirdparty.Random Random = new Thirdparty.Random();

		public ServerSettings Settings;
		public ModData ModData;
		public Map Map;
		XTimer gameTimeout;

		protected volatile ServerState pState = new ServerState();
		public ServerState State
		{
			get { return pState; }
			protected set { pState = value; }
		}

		public void Shutdown()
		{
			State = ServerState.ShuttingDown;
		}

		public void EndGame()
		{
			foreach (var t in ServerTraits.WithInterface<IEndGame>())
				t.GameEnded(this);
		}

		public Server(IPEndPoint endpoint, string[] mods, ServerSettings settings, ModData modData)
		{
			Log.AddChannel("server", "server.log");

			pState = ServerState.WaitingPlayers;
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
				ServerTraits.Add(modData.ObjectCreator.CreateObject<ServerTrait>(trait));

			lobbyInfo = new Session(mods);
			lobbyInfo.GlobalSettings.RandomSeed = randomSeed;
			lobbyInfo.GlobalSettings.Map = settings.Map;
			lobbyInfo.GlobalSettings.ServerName = settings.Name;
			lobbyInfo.GlobalSettings.Ban = settings.Ban;
			lobbyInfo.GlobalSettings.Dedicated = settings.Dedicated;

			foreach (var t in ServerTraits.WithInterface<INotifyServerStart>())
				t.ServerStarted(this);

			Log.Write("server", "Initial mods: ");
			foreach (var m in lobbyInfo.GlobalSettings.Mods)
				Log.Write("server","- {0}", m);

			Log.Write("server", "Initial map: {0}",lobbyInfo.GlobalSettings.Map);

			new Thread( _ =>
			{
				var timeout = ServerTraits.WithInterface<ITick>().Min(t => t.TickTimeout);
				for (;;)
				{
					var checkRead = new List<Socket>();
					checkRead.Add(listener.Server);
					foreach (var c in conns) checkRead.Add(c.socket);
					foreach (var c in preConns) checkRead.Add(c.socket);

					Socket.Select(checkRead, null, null, timeout);
					if (State == ServerState.ShuttingDown)
					{
						EndGame();
						break;
					}

					foreach (var s in checkRead)
						if (s == listener.Server) AcceptConnection();
						else if (preConns.Count > 0)
						{
							var p = preConns.SingleOrDefault(c => c.socket == s);
							if (p != null) p.ReadData(this);
						}
						else if (conns.Count > 0)
						{
							var conn = conns.SingleOrDefault(c => c.socket == s);
							if (conn != null) conn.ReadData(this);
						}

					foreach (var t in ServerTraits.WithInterface<ITick>())
						t.Tick(this);

					if (State == ServerState.ShuttingDown)
					{
						EndGame();
						if (Settings.AllowPortForward) UPnP.RemovePortforward();
						break;
					}
				}

				foreach (var t in ServerTraits.WithInterface<INotifyServerShutdown>())
					t.ServerShutdown(this);

				preConns.Clear();
				conns.Clear();
				try { listener.Stop(); }
				catch { }
			} ) { IsBackground = true }.Start();

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
				preConns.Add(newConn);

				// Dispatch a handshake order
				var request = new HandshakeRequest()
				{
					Map = lobbyInfo.GlobalSettings.Map,
					Mods = lobbyInfo.GlobalSettings.Mods.Select(m => "{0}@{1}".F(m, Mod.AllMods[m].Version)).ToArray()
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

				var client = new Session.Client()
				{
					Name = handshake.Client.Name,
					IpAddress = ((IPEndPoint)newConn.socket.RemoteEndPoint).Address.ToString(),
					Index = newConn.PlayerIndex,
					Slot = lobbyInfo.FirstEmptySlot(),
					PreferredColor = handshake.Client.Color,
					Color = handshake.Client.Color,
					Country = "random",
					SpawnPoint = 0,
					Team = 0,
					State = Session.ClientState.NotReady,
					IsAdmin = !lobbyInfo.Clients.Any(c1 => c1.IsAdmin)
				};

				if (client.Slot != null)
					SyncClientToPlayerReference(client, Map.Players[client.Slot]);

				var mods = handshake.Mods;

				// Check that the client has compatible mods
				var validMod = mods.All(m => m.Contains('@')) && //valid format
					mods.Count() == Game.CurrentMods.Count() && //same number
					mods.Select(m => Pair.New(m.Split('@')[0], m.Split('@')[1])).All(kv => Game.CurrentMods.ContainsKey(kv.First));

				if (!validMod)
				{
					Log.Write("server", "Rejected connection from {0}; mods do not match.",
						newConn.socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "Your mods don't match the server");
					DropClient(newConn);
					return;
				}

				var validVersion = mods.Select(m => Pair.New(m.Split('@')[0], m.Split('@')[1])).All(
					kv => kv.Second == Game.CurrentMods[kv.First].Version);

				if (!validVersion && !lobbyInfo.GlobalSettings.AllowVersionMismatch)
				{
					Log.Write("server", "Rejected connection from {0}; Not running the same version.",
						newConn.socket.RemoteEndPoint);

					SendOrderTo(newConn, "ServerError", "Not running the same version.");
					DropClient(newConn);
					return;
				}

				// Check if IP is banned
				if (lobbyInfo.GlobalSettings.Ban != null)
				{

					if (lobbyInfo.GlobalSettings.Ban.Contains(client.IpAddress))
					{
						Console.WriteLine("Rejected connection from "+client.Name+"("+newConn.socket.RemoteEndPoint+"); Banned.");
						Log.Write("server", "Rejected connection from {0}; Banned.",
							newConn.socket.RemoteEndPoint);
						SendOrderTo(newConn, "ServerError", "You are banned from the server!");
						DropClient(newConn);
						return;
					}
				}

				// Promote connection to a valid client
				preConns.Remove(newConn);
				conns.Add(newConn);
				lobbyInfo.Clients.Add(client);

				Log.Write("server", "Client {0}: Accepted connection from {1}.",
				          newConn.PlayerIndex, newConn.socket.RemoteEndPoint);

				foreach (var t in ServerTraits.WithInterface<IClientJoined>())
					t.ClientJoined(this, newConn);

				SyncLobbyInfo();
				SendMessage("{0} has joined the server.".F(client.Name));

				// Send initial ping
				SendOrderTo(newConn, "Ping", Environment.TickCount.ToString());

				if (File.Exists("{0}motd_{1}.txt".F(Platform.SupportDir, lobbyInfo.GlobalSettings.Mods[0])))
				{
					var motd = System.IO.File.ReadAllText("{0}motd_{1}.txt".F(Platform.SupportDir, lobbyInfo.GlobalSettings.Mods[0]));
					SendOrderTo(newConn, "Message", motd);
				}

				if (mods.Any(m => m.Contains("{DEV_VERSION}")))
					SendMessage("{0} is running an unversioned development build, ".F(client.Name) +
					"and may desynchronize the game state if they have incompatible rules.");

				SetOrderLag();
			}
			catch (Exception) { DropClient(newConn); }
		}

		void SetOrderLag()
		{
			if (lobbyInfo.IsSinglePlayer)
				lobbyInfo.GlobalSettings.OrderLatency = 1;
			else
				lobbyInfo.GlobalSettings.OrderLatency = 3;

			SyncLobbyInfo();
		}

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

		public void UpdateInFlightFrames(Connection conn)
		{
			if (conn.Frame == 0)
				return;

			if (!inFlightFrames.ContainsKey(conn.Frame))
				inFlightFrames[conn.Frame] = new List<Connection> { conn };
			else
				inFlightFrames[conn.Frame].Add(conn);

			if (conns.All(c => inFlightFrames[conn.Frame].Contains(c)))
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
			foreach (var c in conns.Except(conn).ToArray())
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
				for (; ; )
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
					bool handled = false;
					foreach (var t in ServerTraits.WithInterface<IInterpretCommand>())
						if ((handled = t.InterpretCommand(this, conn, GetClient(conn), so.Data)))
							break;

					if (!handled)
					{
						Log.Write("server", "Unknown server command: {0}", so.Data);
						SendOrderTo(conn, "Message", "Unknown server command: {0}".F(so.Data));
					}

					break;
				
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
					if (!int.TryParse(so.Data, out pingSent))
					{
						Log.Write("server", "Invalid order pong payload: {0}", so.Data);
						break;
					}

					var fromClient = GetClient(conn);
					var history = fromClient.LatencyHistory.ToList();
					history.Add(Environment.TickCount - pingSent);

					// Cap ping history at 5 values (25 seconds)
					if (history.Count > 5)
						history.RemoveRange(0, history.Count - 5);

					fromClient.Latency = history.Sum() / history.Count;
					fromClient.LatencyJitter = (history.Max() - history.Min())/2;
					fromClient.LatencyHistory = history.ToArray();

					if (State == ServerState.WaitingPlayers)
						SyncLobbyInfo();

					break;
				}
			}
		}

		public Session.Client GetClient(Connection conn)
		{
			return lobbyInfo.ClientWithIndex(conn.PlayerIndex);
		}

		public void DropClient(Connection toDrop)
		{
			if (preConns.Contains(toDrop))
				preConns.Remove(toDrop);
			else
			{
				conns.Remove(toDrop);

				var dropClient = lobbyInfo.Clients.Where(c1 => c1.Index == toDrop.PlayerIndex).FirstOrDefault();
				if (dropClient == null)
					return;

				// Send disconnected order, even if still in the lobby
				SendMessage("{0} has disconnected.".F(dropClient.Name));
				DispatchOrdersToClients(toDrop, 0, new ServerOrder("Disconnected", "").Serialize());

				lobbyInfo.Clients.RemoveAll(c => c.Index == toDrop.PlayerIndex);

				// Client was the server admin
				// TODO: Reassign admin for game in progress via an order
				if (lobbyInfo.GlobalSettings.Dedicated && dropClient.IsAdmin && State == ServerState.WaitingPlayers)
				{
					// Remove any bots controlled by the admin
					lobbyInfo.Clients.RemoveAll(c => c.Bot != null && c.BotControllerClientIndex == toDrop.PlayerIndex);

					var nextAdmin = lobbyInfo.Clients.Where(c1 => c1.Bot == null)
						.OrderBy(c => c.Index).FirstOrDefault();

					if (nextAdmin != null)
					{
						nextAdmin.IsAdmin = true;
						SendMessage("{0} is now the admin.".F(nextAdmin.Name));
					}
				}

				DispatchOrders(toDrop, toDrop.MostRecentFrame, new byte[] {0xbf});

				if (conns.Count != 0 || lobbyInfo.GlobalSettings.Dedicated)
					SyncLobbyInfo();

				if (!lobbyInfo.GlobalSettings.Dedicated && dropClient.IsAdmin)
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
			if (State != ServerState.GameStarted)	/* don't do this while the game is running, it breaks things. */
				DispatchOrders(null, 0,
					new ServerOrder("SyncInfo", lobbyInfo.Serialize()).Serialize());

			foreach (var t in ServerTraits.WithInterface<INotifySyncLobbyInfo>())
				t.LobbyInfoSynced(this);
		}

		public void StartGame()
		{
			State = ServerState.GameStarted;
			listener.Stop();

			Console.WriteLine("Game started");

			foreach (var c in conns)
				foreach (var d in conns)
					DispatchOrdersToClient(c, d.PlayerIndex, 0x7FFFFFFF, new byte[] { 0xBF });

			// Drop any unvalidated clients
			foreach (var c in preConns.ToArray())
				DropClient(c);

			DispatchOrders(null, 0,
				new ServerOrder("StartGame", "").Serialize());

			foreach (var t in ServerTraits.WithInterface<IStartGame>())
				t.GameStarted(this);
			
			// Check TimeOut
			if (Settings.TimeOut > 10000)
			{
				gameTimeout = new XTimer(Settings.TimeOut);
				gameTimeout.Elapsed += (_,e) =>
                                {
                                    Console.WriteLine("Timeout at {0}!!!", e.SignalTime);
                                    Environment.Exit(0);
                                };
				gameTimeout.Enabled = true;
			}
		}

		void SendData(Socket s, byte[] data)
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
