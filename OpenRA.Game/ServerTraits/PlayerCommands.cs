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

namespace OpenRA.Server.Traits
{		
	public class PlayerCommands : IInterpretCommand
	{
		public bool InterpretCommand(Connection conn, Session.Client client, string cmd)
		{
			var dict = new Dictionary<string, Func<string, bool>>
			{
				{ "name", 
					s => 
					{
						Log.Write("server", "Player@{0} is now known as {1}", conn.socket.RemoteEndPoint, s);
						client.Name = s;
						Server.SyncLobbyInfo();
						return true;
					}},
				{ "race",
					s => 
					{	
						client.Country = s;
						Server.SyncLobbyInfo();
						return true;
					}},
				{ "team",
					s => 
					{
						int team;
						if (!int.TryParse(s, out team)) { Log.Write("server", "Invalid team: {0}", s ); return false; }

						client.Team = team;
						Server.SyncLobbyInfo();
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
						
						if (Server.lobbyInfo.Clients.Where( c => c != client ).Any( c => (c.SpawnPoint == spawnPoint) && (c.SpawnPoint != 0) ))
						{
							Server.SendChatTo( conn, "You can't be at the same spawn point as another player" );
							return true;
						}

						client.SpawnPoint = spawnPoint;
						Server.SyncLobbyInfo();
						return true;
					}},
				{ "color",
					s =>
					{
						var c = s.Split(',').Select(cc => int.Parse(cc)).ToArray();
						client.Color1 = Color.FromArgb(c[0],c[1],c[2]);
						client.Color2 = Color.FromArgb(c[3],c[4],c[5]);
						Server.SyncLobbyInfo();		
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
