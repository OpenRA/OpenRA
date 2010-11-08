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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.FileFormats;

namespace OpenRA.Server.Traits
{
	public class LobbyCommands : IInterpretCommand, IStartServer, IClientJoined
	{
		public bool InterpretCommand(Connection conn, string cmd)
		{
			var dict = new Dictionary<string, Func<string, bool>>
			{
				{ "ready",
					s =>
					{
						// if we're downloading, we can't ready up.

						var client = Server.GetClient(conn);
						if (client.State == Session.ClientState.NotReady)
							client.State = Session.ClientState.Ready;
						else if (client.State == Session.ClientState.Ready)
							client.State = Session.ClientState.NotReady;

						Log.Write("server", "Player @{0} is {1}",
							conn.socket.RemoteEndPoint, client.State);

						Server.SyncLobbyInfo();
						
						if (Server.conns.Count > 0 && Server.conns.All(c => Server.GetClient(c).State == Session.ClientState.Ready))
							InterpretCommand(conn, "startgame");
						
						return true;
					}},
				{ "startgame", 
					s => 
					{
						Server.StartGame();
						return true;
					}},
				{ "lag",
					s =>
					{
						int lag;
						if (!int.TryParse(s, out lag)) { Log.Write("server", "Invalid order lag: {0}", s); return false; }

						Log.Write("server", "Order lag is now {0} frames.", lag);

						Server.lobbyInfo.GlobalSettings.OrderLatency = lag;
						Server.SyncLobbyInfo();
						return true;
					}},
				{ "spectator",
					s =>
						{
							var slotData = Server.lobbyInfo.Slots.Where(ax => ax.Spectator && !Server.lobbyInfo.Clients.Any(l => l.Slot == ax.Index)).FirstOrDefault();
							if (slotData == null)
								return true;
	
							var cl = Server.GetClient(conn);
							cl.Slot = slotData.Index;
							SyncClientToPlayerReference(cl, slotData.MapPlayer != null ? Server.Map.Players[slotData.MapPlayer] : null);

						Server.SyncLobbyInfo();
						return true;
					}},	
				{ "slot",
					s =>
					{
						int slot;
						if (!int.TryParse(s, out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = Server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null || slotData.Closed || slotData.Bot != null 
							|| Server.lobbyInfo.Clients.Any( c => c.Slot == slot ))
							return false;

						var cl = Server.GetClient(conn);
						cl.Slot = slot;
						SyncClientToPlayerReference(cl, slotData.MapPlayer != null ? Server.Map.Players[slotData.MapPlayer] : null);
						
						Server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_close",
					s =>
					{
						int slot;
						if (!int.TryParse(s, out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = Server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null)
							return false;

						if (conn.PlayerIndex != 0)
						{
							Server.SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						slotData.Closed = true;
						slotData.Bot = null;
						
						/* kick any player that's in the slot */
						var occupant = Server.lobbyInfo.Clients.FirstOrDefault( c => c.Slot == slotData.Index );
						if (occupant != null)
						{
							var occupantConn = Server.conns.FirstOrDefault( c => c.PlayerIndex == occupant.Index );
							if (occupantConn != null)
								Server.DropClient( occupantConn, new Exception() );
						}

						Server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_open",
					s =>
					{
						int slot;
						if (!int.TryParse(s, out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = Server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null)
							return false;

						if (conn.PlayerIndex != 0)
						{
							Server.SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						slotData.Closed = false;
						slotData.Bot = null;

						Server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_bot",
					s =>
					{
						var parts = s.Split(' ');

						if (parts.Length != 2)
						{
							Server.SendChatTo( conn, "Malformed slot_bot command" );
							return true;
						}

						int slot;
						if (!int.TryParse(parts[0], out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = Server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null)
							return false;

						if (conn.PlayerIndex != 0)
						{
							Server.SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						slotData.Bot = parts[1];

						Server.SyncLobbyInfo();
						return true;
					}},
				{ "map",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							Server.SendChatTo( conn, "Only the host can change the map" );
							return true;
						}
						Server.lobbyInfo.GlobalSettings.Map = s;			
						LoadMap();

						foreach(var client in Server.lobbyInfo.Clients)
						{
							client.SpawnPoint = 0;
							var slotData = Server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == client.Slot );
							if (slotData != null && slotData.MapPlayer != null)
								SyncClientToPlayerReference(client, Server.Map.Players[slotData.MapPlayer]);
				
							client.State = Session.ClientState.NotReady;
						}
						
						Server.SyncLobbyInfo();
						return true;
					}},
				{ "lockteams",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							Server.SendChatTo( conn, "Only the host can set that option" );
							return true;
						}
						
						bool.TryParse(s, out Server.lobbyInfo.GlobalSettings.LockTeams);
						Server.SyncLobbyInfo();
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
		
		public void ServerStarted() { LoadMap(); }
		static Session.Slot MakeSlotFromPlayerReference(PlayerReference pr)
		{
			if (!pr.Playable) return null;
			return new Session.Slot
			{
				MapPlayer = pr.Name,
				Bot = null,	/* todo: allow the map to specify a bot class? */
				Closed = false,
			};
		}

		public static void LoadMap()
		{
			Server.Map = new Map(Server.ModData.AvailableMaps[Server.lobbyInfo.GlobalSettings.Map]);
			Server.lobbyInfo.Slots = Server.Map.Players
				.Select(p => MakeSlotFromPlayerReference(p.Value))
				.Where(s => s != null)
				.Select((s, i) => { s.Index = i; return s; })
				.ToList();

			// Generate slots for spectators
			for (int i = 0; i < Server.MaxSpectators; i++)
				Server.lobbyInfo.Slots.Add(new Session.Slot
				{
					Spectator = true,
					Index = Server.lobbyInfo.Slots.Count(),
					MapPlayer = null,
					Bot = null
				});
		}
		
		public void ClientJoined(Connection newConn)
		{
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
			
			var slotData = Server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == client.Slot );
			if (slotData != null)
				SyncClientToPlayerReference(client, Server.Map.Players[slotData.MapPlayer]);
			
			Server.lobbyInfo.Clients.Add(client);

			Log.Write("server", "Client {0}: Accepted connection from {1}",
				newConn.PlayerIndex, newConn.socket.RemoteEndPoint);

			Server.SendChat(newConn, "has joined the game.");
			Server.SyncLobbyInfo();
		}
		
		static int ChooseFreeSlot()
		{
			return Server.lobbyInfo.Slots.First(s => !s.Closed && s.Bot == null 
				&& !Server.lobbyInfo.Clients.Any( c => c.Slot == s.Index )).Index;
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
	}
}
