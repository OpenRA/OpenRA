using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using OpenRa;
using OpenRa.FileFormats;

namespace OpenRA.Server
{
	static class Server
	{
		static List<Connection> conns = new List<Connection>();
		static TcpListener listener = new TcpListener(IPAddress.Any, 1234);
		static Dictionary<int, List<Connection>> inFlightFrames
			= new Dictionary<int, List<Connection>>();
		static Session lobbyInfo = new Session();
		static bool GameStarted = false;

		const int DownloadChunkInterval = 20000;
		const int DownloadChunkSize = 16384;

		public static void Main(string[] args)
		{
			listener.Start();

			Console.WriteLine("Server started.");

			for (; ; )
			{
				var checkRead = new ArrayList();
				checkRead.Add(listener.Server);
				foreach (var c in conns) checkRead.Add(c.socket);

				var isSendingPackages = conns.Any(c => c.Stream != null);

				/* msdn lies, -1 doesnt work. this is ~1h instead. */
				Socket.Select(checkRead, null, null, isSendingPackages ? DownloadChunkInterval : -2 );

				foreach (Socket s in checkRead)
					if (s == listener.Server) AcceptConnection();
					else conns.Single(c => c.socket == s).ReadData();

				foreach (var c in conns.Where(a => a.Stream != null).ToArray())
					SendNextChunk(c);
			}
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
			
			/*
			for (var i = 0; i < 8; i++)
				if (lobbyInfo.Clients.All(c => c.Palette != i))
					return "player"+i;
			*/
			throw new InvalidOperationException("No free palettes");
		}

		static void AcceptConnection()
		{
			var newConn = new Connection { socket = listener.AcceptSocket() };
			try
			{
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
						Race = 1,
						State = Session.ClientState.NotReady
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
					DispatchOrders(null, conn.Frame, new byte[] { 0xef });
				}
			}
		}

		class Chunk { public int Index = 0; public int Count = 0; public string Data = ""; }

		static void SendNextChunk(Connection c)
		{
			try
			{
				var data = c.Stream.Read(Math.Min(DownloadChunkSize, c.RemainingBytes));
				if (data.Length != 0)
				{
					var chunk = new Chunk
					{
						Index = c.NextChunk++,
						Count = c.NumChunks,
						Data = Convert.ToBase64String(data)
					};

					DispatchOrdersToClient(c, 0, 0,
						new ServerOrder("FileChunk",
							FieldSaver.Save(chunk).Nodes.WriteToString()).Serialize());
				}

				c.RemainingBytes -= data.Length;
				if (c.RemainingBytes == 0)
				{
					GetClient(c).State = Session.ClientState.NotReady;
					c.Stream.Dispose();
					c.Stream = null;

					SyncLobbyInfo();
				}
			}
			catch (Exception e) { DropClient(c, e); }
		}

		static void DispatchOrdersToClient(Connection c, int client, int frame, byte[] data)
		{
			try
			{
				c.socket.Blocking = true;
				c.socket.Send(BitConverter.GetBytes(data.Length + 4));
				c.socket.Send(BitConverter.GetBytes(client));
				c.socket.Send(BitConverter.GetBytes(frame));
				c.socket.Send(data);
				c.socket.Blocking = false;
			}
			catch (Exception e) { DropClient(c, e); }
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
				{ "name", 
					s => 
					{
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

						int race;
						if (!int.TryParse(s, out race) || race < 0 || race > 1)
						{
							Console.WriteLine("Invalid race: {0}", s);
							return false;
						}

						GetClient(conn).Race = 1 + race;
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
						
						if (!int.TryParse(s, out pali) || pali < 0 || pali > 7)
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
				{ "addmod",
					s =>
					{
						if (GameStarted)
						{
							SendChatTo( conn, "You can't change mods after the game has started" );
							return true;
						}

						Console.WriteLine("** Added mod: `{0}`", s);
						try
						{
							lobbyInfo.GlobalSettings.Mods = 
								lobbyInfo.GlobalSettings.Mods.Concat( new[] { s } ).ToArray();
							SyncLobbyInfo();
							SendChatTo(conn, "Added mod: " + s );
							return true;
						}
						catch
						{
							Console.WriteLine("Adding the mod failed.");
							SendChatTo( conn, "Adding the mod failed.");
							return true;
						}
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
			DispatchOrders(null, 0, new ServerOrder("Chat", text).Serialize());
		}

		static void InterpretServerOrder(Connection conn, ServerOrder so)
		{
			switch (so.Name)
			{
				case "ToggleReady":
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
						if (conns.All(c => GetClient(c).State == Session.ClientState.Ready))
						{
							Console.WriteLine("All players are ready. Starting the game!");
							GameStarted = true;
							DispatchOrders(null, 0,
								new ServerOrder("StartGame", "").Serialize());
						}
					}
					break;

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
							DispatchOrdersToClient(c, 0, 0, so.Serialize());
					break;

				case "RequestFile":
					{
						Console.WriteLine("** Requesting file: `{0}`", so.Data);
						var client = GetClient(conn);
						client.State = Session.ClientState.Downloading;

						var filename = so.Data.Split(':')[0];

						if (conn.Stream != null)
							conn.Stream.Dispose();

						conn.Stream = File.OpenRead(filename);
						// todo: validate that the SHA1 they asked for matches what we've got.

						var length = (int) new FileInfo(filename).Length;
						conn.NextChunk = 0;
						conn.NumChunks = (length + DownloadChunkSize - 1) / DownloadChunkSize;
						conn.RemainingBytes = length;

						SyncLobbyInfo();
					}
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

			/* don't get stuck waiting for the dropped player, if they were the one holding up a frame */
			
			foreach( var f in inFlightFrames.ToArray() )
			if (conns.All(c => f.Value.Contains(c)))
			{
				inFlightFrames.Remove(f.Key);
				DispatchOrders(null, f.Key, new byte[] { 0xef });
			}

			if (conns.Count == 0) OnServerEmpty();
			else SyncLobbyInfo();
		}

		static void OnServerEmpty()
		{
			Console.WriteLine("Server emptied out; doing a bit of housekeeping to prepare for next game..");
			inFlightFrames.Clear();
			lobbyInfo = new Session();
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
		}
	}
}
