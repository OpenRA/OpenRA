#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using OpenRA.FileFormats;

namespace OpenRA.Server
{
	static class Server
	{
		static List<Connection> conns = new List<Connection>();
		static TcpListener listener;
		static Dictionary<int, List<Connection>> inFlightFrames
			= new Dictionary<int, List<Connection>>();
		static Session lobbyInfo;
		static bool GameStarted = false;
		static string[] initialMods;
		static string Name;
		static WebClient wc = new WebClient();
		static int ExternalPort;
		static int randomSeed;

		const int DownloadChunkInterval = 20000;
		const int DownloadChunkSize = 16384;

		const int MasterPingInterval = 60 * 3;	// 3 minutes. server has a 5 minute TTL for games, so give ourselves a bit
												// of leeway.
		static int lastPing = 0;
		static bool isInternetServer;
		static string masterServerUrl;
		static bool isInitialPing;

		public static void ServerMain(bool internetServer, string masterServerUrl, string name, int port, int extport, string[] mods, string map)
		{
			isInitialPing = true;
			Server.masterServerUrl = masterServerUrl;
			isInternetServer = internetServer;
			listener = new TcpListener(IPAddress.Any, port);
			initialMods = mods;
			Name = name;
			ExternalPort = extport;
			randomSeed = (int)DateTime.Now.ToBinary();

			lobbyInfo = new Session();
			lobbyInfo.GlobalSettings.Mods = mods;
			lobbyInfo.GlobalSettings.RandomSeed = randomSeed;
			lobbyInfo.GlobalSettings.Map = map;
			
			Console.WriteLine("Initial mods: ");
			foreach( var m in lobbyInfo.GlobalSettings.Mods )
				Console.WriteLine("- {0}", m);
			
			Console.WriteLine("Initial map: {0}",lobbyInfo.GlobalSettings.Map);
			
			try
			{
				listener.Start();
			}
			catch (Exception)
			{
				throw new InvalidOperationException( "Unable to start server: port is already in use" );
			}

			new Thread( _ =>
			{
				for( ; ; )
				{
					var checkRead = new ArrayList();
					checkRead.Add( listener.Server );
					foreach( var c in conns ) checkRead.Add( c.socket );

					Socket.Select( checkRead, null, null, MasterPingInterval * 1000000 );

					foreach( Socket s in checkRead )
						if( s == listener.Server ) AcceptConnection();
						else conns.Single( c => c.socket == s ).ReadData();

					if (Environment.TickCount - lastPing > MasterPingInterval * 1000)
						PingMasterServer();
				}
			} ) { IsBackground = true }.Start();
		}

		static int ChooseFreePlayerIndex()
		{
			for (var i = 0; i < 8; i++)
				if (conns.All(c => c.PlayerIndex != i))
					return i;

			throw new InvalidOperationException("Already got 8 players");
		}

		static int ChooseFreePalette()
		{
			// TODO: Query the list of palettes from somewhere, and pick one
			return 0;
		}

		static void AcceptConnection()
		{
			var newConn = new Connection { socket = listener.AcceptSocket() };
			try
			{
				if (GameStarted)
				{
					Console.WriteLine("Rejected connection from {0}; game is already started.", 
						newConn.socket.RemoteEndPoint);
					newConn.socket.Close();
					return;
				}

				newConn.socket.Blocking = false;
				newConn.socket.NoDelay = true;

				// assign the player number.
				newConn.PlayerIndex = ChooseFreePlayerIndex();
				newConn.socket.Send(BitConverter.GetBytes(ProtocolVersion.Version));
				newConn.socket.Send(BitConverter.GetBytes(newConn.PlayerIndex));
				conns.Add(newConn);

				lobbyInfo.Clients.Add(
					new Session.Client()
					{
						Index = newConn.PlayerIndex,
						PaletteIndex = ChooseFreePalette(),
						Name = "Player {0}".F(1 + newConn.PlayerIndex),
						Country = "Random",
						State = Session.ClientState.NotReady,
						SpawnPoint = 0,
						Team = 0,
					});

				Console.WriteLine("Client {0}: Accepted connection from {1}",
					newConn.PlayerIndex, newConn.socket.RemoteEndPoint);

				SendChat(newConn, "has joined the game.");

				SyncLobbyInfo();
			}
			catch (Exception e) { DropClient(newConn, e); }
		}

		public static void UpdateInFlightFrames(Connection conn)
		{
			if (conn.Frame != 0)
			{
				if (!inFlightFrames.ContainsKey(conn.Frame))
					inFlightFrames[conn.Frame] = new List<Connection> { conn };
				else
					inFlightFrames[conn.Frame].Add(conn);

				if (conns.All(c => inFlightFrames[conn.Frame].Contains(c)))
				{
					inFlightFrames.Remove(conn.Frame);
				}
			}
		}

		static void DispatchOrdersToClient(Connection c, int client, int frame, byte[] data)
		{
			try
			{
				var ms = new MemoryStream();
				ms.Write( BitConverter.GetBytes( data.Length + 4 ) );
				ms.Write( BitConverter.GetBytes( client ) );
				ms.Write( BitConverter.GetBytes( frame ) );
				ms.Write( data );
				c.socket.Send( ms.ToArray() );
			}
			catch( Exception e ) { DropClient( c, e ); }
		}

		public static void DispatchOrders(Connection conn, int frame, byte[] data)
		{
			if (frame == 0 && conn != null)
				InterpretServerOrders(conn, data);
			else
			{
				var from = conn != null ? conn.PlayerIndex : 0;
				foreach (var c in conns.Except(conn).ToArray())
					DispatchOrdersToClient(c, from, frame, data);
			}
		}

		static void InterpretServerOrders(Connection conn, byte[] data)
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
		}

		static bool InterpretCommand(Connection conn, string cmd)
		{
			var dict = new Dictionary<string, Func<string, bool>>
			{
				{ "ready",
					s =>
					{
						// if we're downloading, we can't ready up.

						var client = GetClient(conn);
						if (client.State == Session.ClientState.NotReady)
							client.State = Session.ClientState.Ready;
						else if (client.State == Session.ClientState.Ready)
							client.State = Session.ClientState.NotReady;

						Console.WriteLine("Player @{0} is {1}",
							conn.socket.RemoteEndPoint, client.State);

						SyncLobbyInfo();

						// start the game if everyone is ready.
						if (conns.Count > 0 && conns.All(c => GetClient(c).State == Session.ClientState.Ready))
						{
							Console.WriteLine("All players are ready. Starting the game!");
							GameStarted = true;
							foreach( var c in conns )
								foreach( var d in conns )
									DispatchOrdersToClient( c, d.PlayerIndex, 0x7FFFFFFF, new byte[] { 0xBF } );

							DispatchOrders(null, 0,
								new ServerOrder("StartGame", "").Serialize());

							PingMasterServer();
						}

						return true;
					}},
				{ "name", 
					s => 
					{
						if (GameStarted)
						{
							SendChatTo( conn, "You can't change your name after the game has started" );
							return true;
						}

						if (s.Trim() == "")
						{
							SendChatTo( conn, "Blank names are not permitted." );
							return true;
						}

						Console.WriteLine("Player@{0} is now known as {1}", conn.socket.RemoteEndPoint, s);
						GetClient(conn).Name = s;
						SyncLobbyInfo();
						return true;
					}},
				{ "lag",
					s =>
					{
						int lag;
						if (!int.TryParse(s, out lag)) { Console.WriteLine("Invalid order lag: {0}", s); return false; }

						Console.WriteLine("Order lag is now {0} frames.", lag);

						lobbyInfo.GlobalSettings.OrderLatency = lag;
						SyncLobbyInfo();
						return true;
					}},
				{ "race",
					s => 
					{
						if (GameStarted) 
						{
							SendChatTo( conn, "You can't change your race after the game has started" );
							return true;
						}

						GetClient(conn).Country = s;
						SyncLobbyInfo();
						return true;
					}},	
				{ "team",
					s => 
					{
						if (GameStarted) 
						{
							SendChatTo( conn, "You can't change your team after the game has started" );
							return true;
						}

						int team;
						if (!int.TryParse(s, out team)) { Console.WriteLine("Invalid team: {0}", s ); return false; }

						GetClient(conn).Team = team;
						SyncLobbyInfo();
						return true;
					}},	
				{ "spawn",
					s => 
					{
						if (GameStarted) 
						{
							SendChatTo( conn, "You can't change your spawn point after the game has started" );
							return true;
						}

						int spawnPoint;
						if (!int.TryParse(s, out spawnPoint) || spawnPoint < 0 || spawnPoint > 8) //TODO: SET properly!
						{
							Console.WriteLine("Invalid spawn point: {0}", s);
							return false;
						}
						
						if (lobbyInfo.Clients.Where( c => c != GetClient(conn) ).Any( c => (c.SpawnPoint == spawnPoint) && (c.SpawnPoint != 0) ))
						{
							SendChatTo( conn, "You can't be at the same spawn point as another player" );
							return true;
						}

						GetClient(conn).SpawnPoint = spawnPoint;
						SyncLobbyInfo();
						return true;
					}},
				{ "pal",
					s =>
					{
						if (GameStarted) 
						{
							SendChatTo( conn, "You can't change your color after the game has started" );
							return true;
						}
						int pali;
						
						if (!int.TryParse(s, out pali))
						{
							Console.WriteLine("Invalid palette: {0}", s);
							return false;
						}

						if (lobbyInfo.Clients.Where( c => c != GetClient(conn) ).Any( c => c.PaletteIndex == pali ))
						{
							SendChatTo( conn, "You can't be the same color as another player" );
							return true;
						}

						GetClient(conn).PaletteIndex = pali;
						SyncLobbyInfo();
						return true;
					}},
				{ "map",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							SendChatTo( conn, "Only the host can change the map" );
							return true;
						}

						if (GameStarted)
						{
							SendChatTo( conn, "You can't change the map after the game has started" );
							return true;
						}

						lobbyInfo.GlobalSettings.Map = s;			
						foreach(var client in lobbyInfo.Clients)
							client.SpawnPoint = 0;
						
						SyncLobbyInfo();
						return true;
					}},
				{ "addpkg",
					s => 
					{
						if (GameStarted)
						{
							SendChatTo( conn, "You can't change packages after the game has started" );
							return true;
						}

						Console.WriteLine("** Added package: `{0}`", s);
						try
						{
							lobbyInfo.GlobalSettings.Packages = 
								lobbyInfo.GlobalSettings.Packages.Concat( new string[] {
									MakePackageString(s)}).ToArray();
							SyncLobbyInfo();
							return true;
						}
						catch
						{
							Console.WriteLine("Adding the package failed.");
							SendChatTo( conn, "Adding the package failed." );
							return true;
						}
					}},
				{ "mods",
					s =>
					{
						if (GameStarted)
						{
							SendChatTo( conn, "You can't change mods after the game has started" );
							return true;
						}
						var args = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
						lobbyInfo.GlobalSettings.Mods = args.GetRange(0,args.Count - 1).ToArray();
						lobbyInfo.GlobalSettings.Map = args.Last();
						SyncLobbyInfo();
						return true;
					}},
			};

			var cmdName = cmd.Split(' ').First();
			var cmdValue = string.Join(" ", cmd.Split(' ').Skip(1).ToArray());

			Func<string,bool> a;
			if (!dict.TryGetValue(cmdName, out a))
				return false;

			Console.WriteLine( "Client {0} sent server command: {1}", conn.PlayerIndex, cmd );
			return a(cmdValue);
		}

		static void SendChatTo(Connection conn, string text)
		{
			DispatchOrdersToClient(conn, 0, 0,
				new ServerOrder("Chat", text).Serialize());
		}

		static void SendChat(Connection asConn, string text)
		{
			DispatchOrders(asConn, 0, new ServerOrder("Chat", text).Serialize());
		}

		static void InterpretServerOrder(Connection conn, ServerOrder so)
		{
			switch (so.Name)
			{
				case "Chat":
					if (so.Data.StartsWith("/"))
					{
						if (!InterpretCommand(conn, so.Data.Substring(1)))
						{
							Console.WriteLine("Bad server command: {0}", so.Data.Substring(1));
							SendChatTo(conn, "Bad server command.");
						}
					}
					else
						foreach (var c in conns.Except(conn).ToArray())
							DispatchOrdersToClient(c, GetClient(conn).Index, 0, so.Serialize());
					break;
			}
		}

		static Session.Client GetClient(Connection conn)
		{
			return lobbyInfo.Clients.First(c => c.Index == conn.PlayerIndex);
		}

		public static void DropClient(Connection toDrop, Exception e)
		{
			Console.WriteLine("Client dropped: {0}.", toDrop.socket.RemoteEndPoint);

			conns.Remove(toDrop);
			SendChat(toDrop, "Connection Dropped");

			lobbyInfo.Clients.RemoveAll(c => c.Index == toDrop.PlayerIndex);

			DispatchOrders( toDrop, toDrop.MostRecentFrame, new byte[] { 0xbf } );
			
			if (conns.Count == 0) OnServerEmpty();
			else SyncLobbyInfo();
		}

		static void OnServerEmpty()
		{
			Console.WriteLine("Server emptied out; doing a bit of housekeeping to prepare for next game..");
			inFlightFrames.Clear();
			lobbyInfo = new Session();
			lobbyInfo.GlobalSettings.Mods = initialMods;
			lobbyInfo.GlobalSettings.RandomSeed = randomSeed;
			GameStarted = false;
		}

		static string MakePackageString(string a)
		{
			return "{0}:{1}".F(a, CalculateSHA1(a));
		}

		static string CalculateSHA1(string filename)
		{
			using (var csp = SHA1.Create())
				return new string(csp.ComputeHash(File.ReadAllBytes(filename))
					.SelectMany(a => a.ToString("x2")).ToArray());
		}

		static void SyncLobbyInfo()
		{
			var clientData = lobbyInfo.Clients.ToDictionary(
				a => a.Index.ToString(),
				a => FieldSaver.Save(a));

			clientData["GlobalSettings"] = FieldSaver.Save(lobbyInfo.GlobalSettings);

			DispatchOrders(null, 0,
				new ServerOrder("SyncInfo", clientData.WriteToString()).Serialize());

			PingMasterServer();
		}

		static void PingMasterServer()
		{
			if (wc.IsBusy || !isInternetServer) return;

			var url = "ping.php?port={0}&name={1}&state={2}&players={3}&mods={4}&map={5}";
			if (isInitialPing)
			{
				url += "&new=1";
				isInitialPing = false;
			}

			wc.DownloadDataAsync(new Uri(
				masterServerUrl + url.F(
				ExternalPort, Uri.EscapeUriString(Name),
				GameStarted ? 2 : 1,	// todo: post-game states, etc.
				lobbyInfo.Clients.Count,
				string.Join(",", lobbyInfo.GlobalSettings.Mods),
				lobbyInfo.GlobalSettings.Map)));

			lastPing = Environment.TickCount;
		}
	}
}
