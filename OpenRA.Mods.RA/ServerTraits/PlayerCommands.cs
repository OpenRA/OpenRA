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
						client.Country = s;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "team",
					s => 
					{
						int team;
						if (!int.TryParse(s, out team)) { Log.Write("server", "Invalid team: {0}", s ); return false; }

						client.Team = team;
						server.SyncLobbyInfo();
						return true;
					}},	
				{ "spawn",
					s => 
					{
						int spawnPoint;
						if (!int.TryParse(s, out spawnPoint) || spawnPoint < 0 || spawnPoint > 8) //TODO: SET properly!
						{
							Log.Write("server", "Invalid spawn point: {0}", s);
							return false;
						}
						
						if (server.lobbyInfo.Clients.Where( c => c != client ).Any( c => (c.SpawnPoint == spawnPoint) && (c.SpawnPoint != 0) ))
						{
							server.SendChatTo( conn, "You can't be at the same spawn point as another player" );
							return true;
						}

						client.SpawnPoint = spawnPoint;
						server.SyncLobbyInfo();
						return true;
					}},
				{ "color",
					s =>
					{
						var c = s.Split(',').Select(cc => int.Parse(cc)).ToArray();
						client.ColorRamp = new ColorRamp((byte)c[0], (byte)c[1], (byte)c[2], (byte)c[3]);
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
