#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using BeaconLib;
using OpenRA.Network;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class MasterServerPinger : ServerTrait, ITick, INotifyServerStart, INotifySyncLobbyInfo, IStartGame, IEndGame
	{
		// 3 minutes. Server has a 5 minute TTL for games, so give ourselves a bit of leeway.
		const int MasterPingInterval = 60 * 3;
		static readonly Beacon LanGameBeacon;
		static readonly Dictionary<int, string> MasterServerErrors = new Dictionary<int, string>()
		{
			{ 1, "Server port is not accessible from the internet." },
			{ 2, "Server name contains a blacklisted word." }
		};

		public int TickTimeout { get { return MasterPingInterval * 10000; } }

		long lastPing = 0;
		bool isInitialPing = true;

		volatile bool isBusy;
		Queue<string> masterServerMessages = new Queue<string>();

		static MasterServerPinger()
		{
			try
			{
				LanGameBeacon = new Beacon("OpenRALANGame", (ushort)new Random(DateTime.Now.Millisecond).Next(2048, 60000));
			}
			catch (Exception ex)
			{
				Log.Write("server", "BeaconLib.Beacon: " + ex.Message);
			}
		}

		public void Tick(S server)
		{
			if ((Game.RunTime - lastPing > MasterPingInterval * 1000) || isInitialPing)
				PublishGame(server);
			else
				lock (masterServerMessages)
					while (masterServerMessages.Count > 0)
						server.SendMessage(masterServerMessages.Dequeue());
		}

		public void ServerStarted(S server)
		{
			if (!server.Ip.Equals(IPAddress.Loopback) && LanGameBeacon != null)
				LanGameBeacon.Start();
		}

		public void LobbyInfoSynced(S server)
		{
			PublishGame(server);
		}

		public void GameStarted(S server)
		{
			PublishGame(server);
		}

		public void GameEnded(S server)
		{
			if (LanGameBeacon != null)
				LanGameBeacon.Stop();

			PublishGame(server);
		}

		void PublishGame(S server)
		{
			// Cache the server info on the main thread to ensure data consistency
			var gs = new GameServer(server);

			if (!isBusy && server.Settings.AdvertiseOnline)
				UpdateMasterServer(server, gs.ToPOSTData(false));

			if (LanGameBeacon != null)
				LanGameBeacon.BeaconData = gs.ToPOSTData(true);
		}

		void UpdateMasterServer(S server, string postData)
		{
			lastPing = Game.RunTime;
			isBusy = true;

			Action a = () =>
			{
				try
				{
					var endpoint = server.ModData.Manifest.Get<WebServices>().ServerAdvertise;
					using (var wc = new WebClient())
					{
						wc.Proxy = null;
						var masterResponseText = wc.UploadString(endpoint, postData);

						if (isInitialPing)
						{
							Log.Write("server", "Master server: " + masterResponseText);
							var errorCode = 0;
							var errorMessage = string.Empty;

							if (masterResponseText.Length > 0)
							{
								var regex = new Regex(@"^\[(?<code>\d+)\](?<message>.*)");
								var match = regex.Match(masterResponseText);
								errorMessage = match.Success && int.TryParse(match.Groups["code"].Value, out errorCode) ?
									match.Groups["message"].Value.Trim() : "Failed to parse error message";
							}

							isInitialPing = false;
							lock (masterServerMessages)
							{
								masterServerMessages.Enqueue("Master server communication established.");
								if (errorCode != 0)
								{
									// Hardcoded error messages take precedence over the server-provided messages
									string message;
									if (!MasterServerErrors.TryGetValue(errorCode, out message))
										message = errorMessage;

									masterServerMessages.Enqueue("Warning: " + message);
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
