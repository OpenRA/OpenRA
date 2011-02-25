#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Network;
using OpenRA.FileFormats;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.RA.Server
{
	public class LobbyCommands : ServerTrait, IInterpretCommand, INotifyServerStart
	{
		public static int MaxSpectators = 4; // How many spectators to allow // @todo Expose this as an option

		public bool InterpretCommand(S server, Connection conn, Session.Client client, string cmd)
		{
			if (server.GameStarted)
			{
				server.SendChatTo(conn, "Cannot change state when game started. ({0})".F(cmd));
				return false;
			}
			else if (client.State == Session.ClientState.Ready && !(cmd == "ready" || cmd == "startgame"))
			{
				server.SendChatTo(conn, "Cannot change state when marked as ready.");
				return false;
			}
			
			var dict = new Dictionary<string, Func<string, bool>>
			{
				{ "ready",
					s =>
					{
						// if we're downloading, we can't ready up.
						if (client.State == Session.ClientState.NotReady)
							client.State = Session.ClientState.Ready;
						else if (client.State == Session.ClientState.Ready)
							client.State = Session.ClientState.NotReady;

						Log.Write("server", "Player @{0} is {1}",
							conn.socket.RemoteEndPoint, client.State);

						server.SyncLobbyInfo();
						
						if (server.conns.Count > 0 && server.conns.All(c => server.GetClient(c).State == Session.ClientState.Ready))
							InterpretCommand(server, conn, client, "startgame");
						
						return true;
					}},
				{ "startgame", 
					s => 
					{
						server.StartGame();
						return true;
					}},
				{ "lag",
					s =>
					{
						int lag;
						if (!int.TryParse(s, out lag)) { Log.Write("server", "Invalid order lag: {0}", s); return false; }

						Log.Write("server", "Order lag is now {0} frames.", lag);

						server.lobbyInfo.GlobalSettings.OrderLatency = lag;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "spectator",
					s =>
						{
							var slotData = server.lobbyInfo.Slots.Where(ax => ax.Spectator && !server.lobbyInfo.Clients.Any(l => l.Slot == ax.Index)).FirstOrDefault();
							if (slotData == null)
								return true;
	
							client.Slot = slotData.Index;
							S.SyncClientToPlayerReference(client, slotData.MapPlayer != null ? server.Map.Players[slotData.MapPlayer] : null);

						server.SyncLobbyInfo();
						return true;
					}},	
				{ "slot",
					s =>
					{
						int slot;
						if (!int.TryParse(s, out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null || slotData.Closed || slotData.Bot != null 
							|| server.lobbyInfo.Clients.Any( c => c.Slot == slot ))
							return false;

						client.Slot = slot;
						S.SyncClientToPlayerReference(client, slotData.MapPlayer != null ? server.Map.Players[slotData.MapPlayer] : null);
						
						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_close",
					s =>
					{
						int slot;
						if (!int.TryParse(s, out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null)
							return false;

						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						slotData.Closed = true;
						slotData.Bot = null;
						
						/* kick any player that's in the slot */
						var occupant = server.lobbyInfo.Clients.FirstOrDefault( c => c.Slot == slotData.Index );
						if (occupant != null)
						{
							var occupantConn = server.conns.FirstOrDefault( c => c.PlayerIndex == occupant.Index );
							if (occupantConn != null)
							{
								server.SendOrderTo(occupantConn, "ServerError", "Your slot was closed by the host");
								server.DropClient(occupantConn);
							}
						}

						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_open",
					s =>
					{
						int slot;
						if (!int.TryParse(s, out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null)
							return false;

						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						slotData.Closed = false;
						slotData.Bot = null;

						server.SyncLobbyInfo();
						return true;
					}},
				{ "slot_bot",
					s =>
					{
						var parts = s.Split(' ');

						if (parts.Length < 2)
						{
							server.SendChatTo( conn, "Malformed slot_bot command" );
							return true;
						}

						int slot;
						if (!int.TryParse(parts[0], out slot)) { Log.Write("server", "Invalid slot: {0}", s ); return false; }

						var slotData = server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == slot );
						if (slotData == null)
							return false;

						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can alter slots" );
							return true;
						}

						slotData.Closed = false;
						slotData.Bot = string.Join(" ", parts.Skip(1).ToArray() );

						server.SyncLobbyInfo();
						return true;
					}},
				{ "map",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can change the map" );
							return true;
						}
						server.lobbyInfo.GlobalSettings.Map = s;			
						LoadMap(server);

						foreach(var c in server.lobbyInfo.Clients)
						{
							c.SpawnPoint = 0;
							var slotData = server.lobbyInfo.Slots.FirstOrDefault( x => x.Index == c.Slot );
							if (slotData != null && slotData.MapPlayer != null)
								S.SyncClientToPlayerReference(c, server.Map.Players[slotData.MapPlayer]);
				
							c.State = Session.ClientState.NotReady;
						}
						
						server.SyncLobbyInfo();
						return true;
					}},
				{ "lockteams",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can set that option" );
							return true;
						}
						
						bool.TryParse(s, out server.lobbyInfo.GlobalSettings.LockTeams);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "allowcheats",
					s =>
					{
						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can set that option" );
							return true;
						}
						
						bool.TryParse(s, out server.lobbyInfo.GlobalSettings.AllowCheats);
						server.SyncLobbyInfo();
						return true;
					}},
				{ "kick",
					s => 
					{
						if (conn.PlayerIndex != 0)
						{
							server.SendChatTo( conn, "Only the host can kick players" );
							return true;
						}

						int slot;
						int.TryParse( s, out slot );

						var connToKick = server.conns.SingleOrDefault( c => server.GetClient(c) != null && server.GetClient(c).Slot == slot);
						if (connToKick == null) 
						{
							server.SendChatTo( conn, "Noone in that slot." );
							return true;
						}
						
						server.SendOrderTo(connToKick, "ServerError", "You have been kicked from the server");
						server.DropClient(connToKick);

						server.SyncLobbyInfo();

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
		
		public void ServerStarted(S server) { LoadMap(server); }
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

		public static void LoadMap(S server)
		{
			server.Map = new Map(server.ModData.AvailableMaps[server.lobbyInfo.GlobalSettings.Map].Path);
			server.lobbyInfo.Slots = server.Map.Players
				.Select(p => MakeSlotFromPlayerReference(p.Value))
				.Where(s => s != null)
				.Select((s, i) => { s.Index = i; return s; })
				.ToList();

			// Generate slots for spectators
			for (int i = 0; i < MaxSpectators; i++)
				server.lobbyInfo.Slots.Add(new Session.Slot
				{
					Spectator = true,
					Index = server.lobbyInfo.Slots.Count(),
					MapPlayer = null,
					Bot = null
				});
		}
	}
}
