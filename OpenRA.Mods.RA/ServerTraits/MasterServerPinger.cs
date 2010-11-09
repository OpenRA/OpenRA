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
using System.Net;
using OpenRA.Server;
using server = OpenRA.Server.Server;

namespace OpenRA.Mods.RA.Server
{
	public class MasterServerPinger : ServerTrait, ITick, INotifySyncLobbyInfo, IStartGame
	{
		const int MasterPingInterval = 60 * 3;	// 3 minutes. server has a 5 minute TTL for games, so give ourselves a bit
												// of leeway.
		public int TickTimeout { get { return MasterPingInterval * 10000; } }
		public void Tick()
		{
			if (Environment.TickCount - lastPing > MasterPingInterval * 1000)
				PingMasterServer();
			else
				lock (masterServerMessages)
					while (masterServerMessages.Count > 0)
						server.SendChat(null, masterServerMessages.Dequeue());
			
		}
		
		
		public void LobbyInfoSynced() { PingMasterServer(); }
		public void GameStarted() { PingMasterServer(); }

		static int lastPing = 0;
		// Todo: use the settings passed to the server instead
		static bool isInternetServer = Game.Settings.Server.AdvertiseOnline;
		static string masterServerUrl = Game.Settings.Server.MasterServer;
		static int externalPort = Game.Settings.Server.ExternalPort;
		static bool isInitialPing = true;
		
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
								externalPort, Uri.EscapeUriString(server.Name),
								server.GameStarted ? 2 : 1,	// todo: post-game states, etc.
								server.lobbyInfo.Clients.Count,
								string.Join(",", server.lobbyInfo.GlobalSettings.Mods),
								server.lobbyInfo.GlobalSettings.Map));

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
