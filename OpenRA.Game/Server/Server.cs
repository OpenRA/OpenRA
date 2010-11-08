#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Network;
using OpenRA.Server.Traits;

namespace OpenRA.Server
{
	static class Server
	{
		public static List<Connection> conns = new List<Connection>();
		static TcpListener listener = null;
		static Dictionary<int, List<Connection>> inFlightFrames
			= new Dictionary<int, List<Connection>>();
		
		static TypeDictionary ServerTraits = new TypeDictionary();
		public static Session lobbyInfo;
		public static bool GameStarted = false;
		static string Name;
		static int ExternalPort;
		static int randomSeed;

		const int DownloadChunkInterval = 20000;
		const int DownloadChunkSize = 16384;

		const int MasterPingInterval = 60 * 3;	// 3 minutes. server has a 5 minute TTL for games, so give ourselves a bit
												// of leeway.

		public static int MaxSpectators = 4; // How many spectators to allow // @todo Expose this as an option

		static int lastPing = 0;
		static bool isInternetServer;
		static string masterServerUrl;
		static bool isInitialPing;
		public static ModData ModData;
		public static Map Map;

		public static void StopListening()
		{
			conns.Clear();
			GameStarted = false;
			try { listener.Stop(); }
			catch { }
		}
		
		public static void ServerMain(ModData modData, Settings settings, string map)
		{
			Log.AddChannel("server", "server.log");

			ServerTraits.Add( new DebugServerTrait() );
			ServerTraits.Add( new PlayerCommands() );
			ServerTraits.Add( new LobbyCommands() );
			isInitialPing = true;
			Server.masterServerUrl = settings.Server.MasterServer;
			isInternetServer = settings.Server.AdvertiseOnline;
			listener = new TcpListener(IPAddress.Any, settings.Server.ListenPort);
			Name = settings.Server.Name;
			ExternalPort = settings.Server.ExternalPort;
			randomSeed = (int)DateTime.Now.ToBinary();
			ModData = modData;

			lobbyInfo = new Session( settings.Game.Mods );
			lobbyInfo.GlobalSettings.RandomSeed = randomSeed;
			lobbyInfo.GlobalSettings.Map = map;
			lobbyInfo.GlobalSettings.AllowCheats = settings.Server.AllowCheats;
			lobbyInfo.GlobalSettings.ServerName = settings.Server.Name;
			
			foreach (var t in ServerTraits.WithInterface<IStartServer>())
				t.ServerStarted();
						
			Log.Write("server", "Initial mods: ");
			foreach( var m in lobbyInfo.GlobalSettings.Mods )
				Log.Write("server","- {0}", m);
			
			Log.Write("server", "Initial map: {0}",lobbyInfo.GlobalSettings.Map);
			
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

					Socket.Select( checkRead, null, null, MasterPingInterval * 10000 );

					foreach( Socket s in checkRead )
						if( s == listener.Server ) AcceptConnection();
						else if (conns.Count > 0) conns.Single( c => c.socket == s ).ReadData();

					if (Environment.TickCount - lastPing > MasterPingInterval * 1000)
						PingMasterServer();
					else
						lock (masterServerMessages)
							while (masterServerMessages.Count > 0)
								SendChat(null, masterServerMessages.Dequeue());
					
					if (conns.Count() == 0)
					{
						listener.Stop();
						GameStarted = false;
						break;
					}
				}
			} ) { IsBackground = true }.Start();


		}

		/* lobby rework todo: 
		 *	- "teams together" option for team games -- will eliminate most need
		 *		for manual spawnpoint choosing.
		 *	- 256 max players is a dirty hack
		 */
		static int ChooseFreePlayerIndex()
		{
			for (var i = 0; i < 256; i++)
				if (conns.All(c => c.PlayerIndex != i))
					return i;

			throw new InvalidOperationException("Already got 256 players");
		}

		static int ChooseFreeSlot()
		{
			return lobbyInfo.Slots.First(s => !s.Closed && s.Bot == null 
				&& !lobbyInfo.Clients.Any( c => c.Slot == s.Index )).Index;
		}

		static void AcceptConnection()
		{
			Socket newSocket = null;

			try
			{
				if (!listener.Server.IsBound) return;
				newSocket = listener.AcceptSocket();
			}catch
			{
				/* could have an exception here when listener 'goes away' when calling AcceptConnection! */
				/* alternative would be to use locking but the listener doesnt go away without a reason */
				return; 
			}

			var newConn = new Connection { socket = newSocket };
			try
			{
				if (GameStarted)
				{
					Log.Write("server", "Rejected connection from {0}; game is already started.", 
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

				var defaults = new GameRules.PlayerSettings();
				
				var client = new Session.Client()
				{
					Index = newConn.PlayerIndex,
					Color1 = defaults.Color1,
					Color2 = defaults.Color2,
					Name = defaults.Name,
					Country = "random",
					State = Session.ClientState.NotReady,
					SpawnPoint = 0,
					Team = 0,
					Slot = ChooseFreeSlot(),
				};
				
				var slotData = lobbyInfo.Slots.FirstOrDefault( x => x.Index == client.Slot );
				if (slotData != null)
					SyncClientToPlayerReference(client, Map.Players[slotData.MapPlayer]);
				
				lobbyInfo.Clients.Add(client);

				Log.Write("server", "Client {0}: Accepted connection from {1}",
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

		public static void DispatchOrdersToClient(Connection c, int client, int frame, byte[] data)
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
			catch (NotImplementedException) { }
		}

		public static void SyncClientToPlayerReference(Session.Client c, PlayerReference pr)
		{
			if (pr == null)
				return;
			if (pr.LockColor)
			{
				c.Color1 = pr.Color;
				c.Color2 = pr.Color2;
			}
			if (pr.LockRace)
				c.Country = pr.Race;
		}
		
		public static void SendChatTo(Connection conn, string text)
		{
			DispatchOrdersToClient(conn, 0, 0,
				new ServerOrder("Chat", text).Serialize());
		}

        static void SendChat(Connection asConn, string text)
        {
            DispatchOrders(asConn, 0, new ServerOrder("Chat", text).Serialize());
        }

        static void SendDisconnected(Connection asConn)
        {
            DispatchOrders(asConn, 0, new ServerOrder("Disconnected", "").Serialize());
        }

		static void InterpretServerOrder(Connection conn, ServerOrder so)
		{
			switch (so.Name)
			{
				case "Command":
					{
						if (GameStarted)
							SendChatTo(conn, "Cannot change state when game started. ({0})".F(so.Data));
						else if (GetClient(conn).State == Session.ClientState.Ready && !(so.Data == "ready" || so.Data == "startgame"))
							SendChatTo(conn, "Cannot change state when marked as ready.");
						else
						{
							bool handled = false;
							foreach (var t in ServerTraits.WithInterface<IInterpretCommand>())
								if ((handled = t.InterpretCommand(conn, so.Data)))
									break;
							
							if (!handled)
							{
								Log.Write("server", "Unknown server command: {0}", so.Data);
								SendChatTo(conn, "Unknown server command: {0}".F(so.Data));
							}
						}
					}
					break;

				case "Chat":
				case "TeamChat":
					foreach (var c in conns.Except(conn).ToArray())
						DispatchOrdersToClient(c, GetClient(conn).Index, 0, so.Serialize());
				break;
			}
		}

		public static Session.Client GetClient(Connection conn)
		{
			return lobbyInfo.Clients.First(c => c.Index == conn.PlayerIndex);
		}

		public static void DropClient(Connection toDrop, Exception e)
		{
			conns.Remove(toDrop);
			SendChat(toDrop, "Connection Dropped");

            if (GameStarted)
                SendDisconnected(toDrop); /* Report disconnection */

			lobbyInfo.Clients.RemoveAll(c => c.Index == toDrop.PlayerIndex);

			DispatchOrders( toDrop, toDrop.MostRecentFrame, new byte[] { 0xbf } );
			
			if (conns.Count != 0)
				SyncLobbyInfo();
		}

		public static void SyncLobbyInfo()
		{
			if (!GameStarted)	/* don't do this while the game is running, it breaks things. */
				DispatchOrders(null, 0,
					new ServerOrder("SyncInfo", lobbyInfo.Serialize()).Serialize());

			PingMasterServer();
		}

		static volatile bool isBusy;
		static Queue<string> masterServerMessages = new Queue<string>();
		public static void PingMasterServer()
		{
			if (isBusy || !isInternetServer) return;

			lastPing = Environment.TickCount;
			isBusy = true;

			Action a = () =>
				{
					try
					{
						var url = "ping.php?port={0}&name={1}&state={2}&players={3}&mods={4}&map={5}";
						if (isInitialPing) url += "&new=1";

						using (var wc = new WebClient())
						{
							 wc.DownloadData(
								masterServerUrl + url.F(
								ExternalPort, Uri.EscapeUriString(Name),
								GameStarted ? 2 : 1,	// todo: post-game states, etc.
								lobbyInfo.Clients.Count,
								string.Join(",", lobbyInfo.GlobalSettings.Mods),
								lobbyInfo.GlobalSettings.Map));

							if (isInitialPing)
							{
								isInitialPing = false;
								lock (masterServerMessages)
									masterServerMessages.Enqueue("Master server communication established.");
							}
						}
					}
					catch(Exception ex)
					{
						Log.Write("server", ex.ToString());
						lock( masterServerMessages )
							masterServerMessages.Enqueue( "Master server communication failed." );
					}

					isBusy = false;
				};

			a.BeginInvoke(null, null);
		}
	}
}
