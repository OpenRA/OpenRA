#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class MasterServerPinger : ServerTrait, ITick, INotifySyncLobbyInfo, IStartGame, IEndGame
	{
		// 3 minutes. Server has a 5 minute TTL for games, so give ourselves a bit of leeway.
		const int MasterPingInterval = 60 * 3;
		public int TickTimeout { get { return MasterPingInterval * 10000; } }

		public void Tick(S server)
		{
			if ((Game.RunTime - lastPing > MasterPingInterval * 1000) || isInitialPing)
				PingMasterServer(server);
			else
				lock (masterServerMessages)
					while (masterServerMessages.Count > 0)
						server.SendMessage(masterServerMessages.Dequeue());
		}

		public void LobbyInfoSynced(S server) { PingMasterServer(server); }
		public void GameStarted(S server) { PingMasterServer(server); }
		public void GameEnded(S server) { PingMasterServer(server); }

		int lastPing = 0;
		bool isInitialPing = true;

		volatile bool isBusy;
		Queue<string> masterServerMessages = new Queue<string>();

		public void PingMasterServer(S server)
		{
			if (isBusy || !server.Settings.AdvertiseOnline) return;

			lastPing = Game.RunTime;
			isBusy = true;

			var mod = server.ModData.Manifest.Mod;

			// important to grab these on the main server thread, not in the worker we're about to spawn -- they may be modified
			// by the main thread as clients join and leave.
			var numPlayers = server.LobbyInfo.Clients.Where(c1 => c1.Bot == null && c1.Slot != null).Count();
			var numBots = server.LobbyInfo.Clients.Where(c1 => c1.Bot != null).Count();
			var numSpectators = server.LobbyInfo.Clients.Where(c1 => c1.Bot == null && c1.Slot == null).Count();
			var numSlots = server.LobbyInfo.Slots.Where(s => !s.Value.Closed).Count() - numBots;
			var passwordProtected = string.IsNullOrEmpty(server.Settings.Password) ? 0 : 1;
			var clients = server.LobbyInfo.Clients.Where(c1 => c1.Bot == null).Select(c => Convert.ToBase64String(Encoding.UTF8.GetBytes(c.Name))).ToArray();

			Action a = () =>
			{
				try
				{
					var url = "ping?port={0}&name={1}&state={2}&players={3}&bots={4}&mods={5}&map={6}&maxplayers={7}&spectators={8}&protected={9}&clients={10}";
					if (isInitialPing) url += "&new=1";

					using (var wc = new WebClient())
					{
						wc.Proxy = null;
						var masterResponse = wc.DownloadData(
							server.Settings.MasterServer + url.F(
							server.Settings.ExternalPort, Uri.EscapeUriString(server.Settings.Name),
							(int)server.State,
							numPlayers,
							numBots,
							"{0}@{1}".F(mod.Id, mod.Version),
							server.LobbyInfo.GlobalSettings.Map,
							numSlots,
							numSpectators,
							passwordProtected,
							string.Join(",", clients)));

						if (isInitialPing)
						{
							var masterResponseText = Encoding.UTF8.GetString(masterResponse);
							isInitialPing = false;
							lock (masterServerMessages)
							{
								masterServerMessages.Enqueue("Master server communication established.");
								if (masterResponseText.Contains("[001]"))  // Server does not respond code
								{
									Log.Write("server", masterResponseText);
									masterServerMessages.Enqueue("Warning: Server ports are not forwarded.");
									masterServerMessages.Enqueue("Game has not been advertised online.");
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Log.Write("server", ex.ToString());
					lock (masterServerMessages)
						masterServerMessages.Enqueue("Master server communication failed.");
				}

				isBusy = false;
			};

			a.BeginInvoke(null, null);
		}
	}
}
