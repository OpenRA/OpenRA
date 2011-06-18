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
using System.Drawing;
using System.Linq;
using OpenRA.Network;
using OpenRA.Server;
using S = OpenRA.Server.Server;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.Server
{		
	public class PlayerCommands : ServerTrait, IInterpretCommand
	{
		public bool InterpretCommand( S server, Connection conn, Session.Client client, string cmd)
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
				{ "name", 
					s => 
					{
						Log.Write("server", "Player@{0} is now known as {1}", conn.socket.RemoteEndPoint, s);
						client.Name = s;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "race",
					s => 
					{	
						var parts = s.Split(' ');
						var targetClient = server.lobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && conn.PlayerIndex != 0)
							return true;

						// Map has disabled race changes
						if (server.lobbyInfo.Slots[targetClient.Slot].LockRace)
							return true;

						targetClient.Country = parts[1];
						server.SyncLobbyInfo();
						return true;
					}},
				{ "team",
					s => 
					{
						var parts = s.Split(' ');
						var targetClient = server.lobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && conn.PlayerIndex != 0)
							return true;

						// Map has disabled team changes
						if (server.lobbyInfo.Slots[targetClient.Slot].LockTeam)
							return true;

						int team;
						if (!int.TryParse(parts[1], out team)) { Log.Write("server", "Invalid team: {0}", s ); return false; }

						targetClient.Team = team;
						server.SyncLobbyInfo();
						return true;
					}},	
				{ "spawn",
					s => 
					{
						var parts = s.Split(' ');
						var targetClient = server.lobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && conn.PlayerIndex != 0)
							return true;

						// Spectators don't need a spawnpoint
						if (client.Slot == null)
							return true;

						int spawnPoint;
						if (!int.TryParse(parts[1], out spawnPoint) || spawnPoint < 0 || spawnPoint > server.Map.SpawnPoints.Count())
						{
							Log.Write("server", "Invalid spawn point: {0}", parts[1]);
							return true;
						}

						if (server.lobbyInfo.Clients.Where( cc => cc != client ).Any( cc => (cc.SpawnPoint == spawnPoint) && (cc.SpawnPoint != 0) ))
						{
							server.SendChatTo( conn, "You can't be at the same spawn point as another player" );
							return true;
						}

						targetClient.SpawnPoint = spawnPoint;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "color",
					s =>
					{
						var parts = s.Split(' ');
						var targetClient = server.lobbyInfo.ClientWithIndex(int.Parse(parts[0]));

						// Only the host can change other client's info
						if (targetClient.Index != client.Index && conn.PlayerIndex != 0)
							return true;

						// Map has disabled color changes
						if (targetClient.Slot != null && server.lobbyInfo.Slots[targetClient.Slot].LockColor)
							return true;

						var ci = parts[1].Split(',').Select(cc => int.Parse(cc)).ToArray();
						targetClient.ColorRamp = new ColorRamp((byte)ci[0], (byte)ci[1], (byte)ci[2], (byte)ci[3]);
						server.SyncLobbyInfo();		
						return true;
					}}
			};
			
			var cmdName = cmd.Split(' ').First();
			var cmdValue = string.Join(" ", cmd.Split(' ').Skip(1).ToArray());

			Func<string,bool> a;
			if (!dict.TryGetValue(cmdName, out a))
				return false;
			
			return a(cmdValue);
		}
	}
}
