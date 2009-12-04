using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net;
using System.Collections;

namespace OpenRA.Server
{
	class ServerOrder
	{
		public readonly int PlayerId;
		public readonly string Name;
		public readonly string Data;

		public ServerOrder(int playerId, string name, string data)
		{
			PlayerId = playerId;
			Name = name;
			Data = data;
		}

		public static ServerOrder Deserialize(BinaryReader r)
		{
			byte b;
			switch (b = r.ReadByte())
			{
				case 0xff:
					Console.WriteLine("This isn't a server order.");
					return null;

				case 0xfe:
					{
						var playerID = r.ReadInt32();
						var name = r.ReadString();
						var data = r.ReadString();

						return new ServerOrder(playerID, name, data);
					}

				default:
					throw new NotImplementedException(b.ToString("x2"));
			}
		}

		public byte[] Serialize()
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);

			bw.Write((byte)0xfe);
			bw.Write(PlayerId);
			bw.Write(Name);
			bw.Write(Data);
			return ms.ToArray();
		}
	}

	static class Server
	{
		static List<Connection> conns = new List<Connection>();
		static TcpListener listener = new TcpListener(IPAddress.Any, 1234);
		static Dictionary<int, List<Connection>> inFlightFrames
			= new Dictionary<int, List<Connection>>();

		public static void Main(string[] args)
		{
			listener.Start();

			Console.WriteLine("Server started.");

			for (; ; )
			{
				var checkRead = new ArrayList();
				checkRead.Add(listener.Server);
				foreach (var c in conns) checkRead.Add(c.socket);

				/* msdn lies, -1 doesnt work. this is ~1h instead. */
				Socket.Select(checkRead, null, null, -2	);

				foreach (Socket s in checkRead)
					if (s == listener.Server) AcceptConnection();
					else ReadData(conns.Single(c => c.socket == s));
			}
		}

		static int ChooseFreePlayerIndex()
		{
			for (var i = 0; i < 8; i++)
				if (conns.All(c => c.PlayerIndex != i))
					return i;

			throw new InvalidOperationException("Already got 8 players");
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

				conns.Add(newConn);

				DispatchOrdersToClient(newConn, 0,
					new ServerOrder(newConn.PlayerIndex, "AssignPlayer", "").Serialize());

				// todo: tell this client about all the other conns.

				Console.WriteLine("Accepted connection from {0}.",
					newConn.socket.RemoteEndPoint);

				DispatchOrders(newConn, 0, new ServerOrder(newConn.PlayerIndex,
					"Chat", "has joined the game.").Serialize());
			}
			catch (Exception e) { DropClient(newConn, e); }
		}

		static bool ReadDataInner(Connection conn)
		{
			var rx = new byte[1024];
			var len = 0;

			for (; ; )
			{
				try
				{
					if (0 < (len = conn.socket.Receive(rx)))
					{
					//	Console.WriteLine("Read {0} bytes", len);
						conn.data.AddRange(rx.Take(len));
					}
					else
						break;
				}
				catch (SocketException e)
				{
					if (e.SocketErrorCode == SocketError.WouldBlock) break;
					DropClient(conn, e); 
					return false; 
				}
			}

			return true;
		}

		static void ReadData(Connection conn)
		{
			//Console.WriteLine("Start ReadData() for {0}",
			//    conn.socket.RemoteEndPoint);

			if (ReadDataInner(conn))
				while (conn.data.Count >= conn.ExpectLength)
				{
					var bytes = conn.PopBytes(conn.ExpectLength);
					switch (conn.State)
					{
						case ReceiveState.Header:
							{
								conn.ExpectLength = BitConverter.ToInt32(bytes, 0) - 4;
								conn.Frame = BitConverter.ToInt32(bytes, 4);
								conn.State = ReceiveState.Data;
							} break;

						case ReceiveState.Data:
							{
								if (bytes.Length > 0)
									Console.WriteLine("{0} bytes", bytes.Length);

								DispatchOrders(conn, conn.Frame, bytes);
								conn.ExpectLength = 8;
								conn.State = ReceiveState.Header;

								UpdateInFlightFrames(conn);
							} break;
					}
				}

			//Console.WriteLine("End ReadData() for {0}",
			//    conn.socket.RemoteEndPoint);
		}

		static void UpdateInFlightFrames(Connection conn)
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

		static void DispatchOrdersToClient(Connection c, int frame, byte[] data)
		{
			try
			{
				c.socket.Blocking = true;
				c.socket.Send(BitConverter.GetBytes(data.Length + 4));
				c.socket.Send(BitConverter.GetBytes(frame));
				c.socket.Send(data);
				c.socket.Blocking = false;
			}
			catch (Exception e) { DropClient(c, e); }
		}

		static void DispatchOrders(Connection conn, int frame, byte[] data)
		{
			foreach (var c in conns.Except(conn).ToArray())
				DispatchOrdersToClient(c, frame, data);

			if (frame == 0 && conn != null)
				InterpretServerOrders(conn, data);
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
						DispatchOrders( null, 0, new ServerOrder( conn.PlayerIndex, "SetName", s ).Serialize());
						return true;
					}},
				{ "lag",
					s =>
					{
						int lag;
						if (!int.TryParse(s, out lag)) { Console.WriteLine("Invalid order lag: {0}", s); return false; }

						Console.WriteLine("Order lag is now {0} frames.", lag);

						DispatchOrders(null, 0,
							new ServerOrder(0, "SetLag", lag.ToString()).Serialize());
						return true;
					}},
				{ "race",
					s => 
					{
						int race;
						if (!int.TryParse(s, out race) || race < 0 || race > 1)
						{
							Console.WriteLine("Invalid race: {0}", s);
							return false;
						}

						DispatchOrders(null, 0,
							new ServerOrder(conn.PlayerIndex, "SetRace", race.ToString()).Serialize());
						return true;
					}},
				{ "pal",
					s =>
					{
						int pal;
						if (!int.TryParse(s, out pal) || pal < 0 || pal > 7)
						{
							Console.WriteLine("Invalid palette: {0}", s);
							return false;
						}

						DispatchOrders(null, 0,
							new ServerOrder(conn.PlayerIndex, "SetPalette", pal.ToString()).Serialize());
						return true;
					}},
			};

			var cmdName = cmd.Split(' ').First();
			var cmdValue = string.Join(" ", cmd.Split(' ').Skip(1).ToArray());

			Func<string,bool> a;
			if (!dict.TryGetValue(cmdName, out a))
				return false;

			return a(cmdValue);
		}

		static void InterpretServerOrder(Connection conn, ServerOrder so)
		{
			switch (so.Name)
			{
				case "ToggleReady":
					conn.IsReady ^= true;

					Console.WriteLine("Player @{0} is {1}", 
						conn.socket.RemoteEndPoint, conn.IsReady ? "ready" : "not ready");

					// start the game if everyone is ready.
					if (conns.All(c => c.IsReady))
					{
						Console.WriteLine("All players are ready. Starting the game!");
						DispatchOrders(null, 0,
							new ServerOrder(0, "StartGame", "").Serialize());
					}
					break;

				case "Chat":
					if (so.Data.StartsWith("/"))
						if (!InterpretCommand(conn, so.Data.Substring(1)))
						{
							Console.WriteLine("Bad server command: {0}", so.Data.Substring(1));
							DispatchOrdersToClient(conn, 0, new ServerOrder(conn.PlayerIndex, "Chat", "Bad server command.").Serialize());
						}
					break;
			}
		}

		static void DropClient(Connection toDrop, Exception e)
		{
			Console.WriteLine("Client dropped: {0}.", toDrop.socket.RemoteEndPoint);
			Console.WriteLine(e.ToString());

			conns.Remove(toDrop);

			DispatchOrders(toDrop, 0, 
				new ServerOrder(toDrop.PlayerIndex, "Chat", "Connection Dropped").Serialize());

			/* don't get stuck waiting for the dropped player, if they were the one holding up a frame */
			
			foreach( var f in inFlightFrames.ToArray() )
			if (conns.All(c => f.Value.Contains(c)))
			{
				inFlightFrames.Remove(f.Key);
				DispatchOrders(null, f.Key, new byte[] { 0xef });
			}
		}

		public static void Write(this Stream s, byte[] data) { s.Write(data, 0, data.Length); }
		public static byte[] Read(this Stream s, int len) { var data = new byte[len]; s.Read(data, 0, len); return data; }
		public static IEnumerable<T> Except<T>(this IEnumerable<T> ts, T t)
		{
			return ts.Except(new[] { t });
		}
	}
}
