#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BeaconLib;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Support;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class MasterServerPinger : ServerTrait, ITick, INotifyServerStart, INotifyServerShutdown, INotifySyncLobbyInfo, IStartGame, IEndGame
	{
		// 3 minutes (in milliseconds). Server has a 5 minute TTL for games, so give ourselves a bit of leeway.
		const int MasterPingInterval = 60 * 3 * 1000;

		// 1 second (in milliseconds) minimum delay between pings
		const int RateLimitInterval = 1000;

		[TranslationReference]
		const string NoPortForward = "notification-no-port-forward";

		[TranslationReference]
		const string BlacklistedTitle = "notification-blacklisted-server-name";

		[TranslationReference]
		const string InvalidErrorCode = "notification-invalid-error-code";

		[TranslationReference]
		const string Connected = "notification-master-server-connected";

		[TranslationReference]
		const string Error = "notification-master-server-error";

		[TranslationReference]
		const string GameOffline = "notification-game-offline";

		static readonly Beacon LanGameBeacon;
		static readonly Dictionary<int, string> MasterServerErrors = new Dictionary<int, string>()
		{
			{ 1, NoPortForward },
			{ 2, BlacklistedTitle }
		};

		long lastPing = 0;
		long lastChanged = 0;
		bool isInitialPing = true;

		volatile bool isBusy;
		readonly Queue<string> masterServerMessages = new Queue<string>();

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
			// Force an update if the last one was too long ago so the advertisement doesn't time out
			if (Game.RunTime - lastChanged > MasterPingInterval)
				lastChanged = Game.RunTime;

			// Update the master server and LAN clients if something has changed
			// Note that isBusy is set while the master server ping is running on a
			// background thread, and limits LAN pings as well as master server pings for simplicity.
			if (!isBusy && ((lastChanged > lastPing && Game.RunTime - lastPing > RateLimitInterval) || isInitialPing))
			{
				var gs = new GameServer(server);
				if (server.Settings.AdvertiseOnline)
					UpdateMasterServer(server, gs.ToPOSTData(false));

				if (LanGameBeacon != null)
					LanGameBeacon.BeaconData = gs.ToPOSTData(true);

				lastPing = Game.RunTime;
			}

			lock (masterServerMessages)
				while (masterServerMessages.Count > 0)
					server.SendLocalizedMessage(masterServerMessages.Dequeue());
		}

		void INotifyServerStart.ServerStarted(S server)
		{
			if (server.Type != ServerType.Local && LanGameBeacon != null)
				LanGameBeacon.Start();
		}

		void INotifyServerShutdown.ServerShutdown(S server)
		{
			if (server.Settings.AdvertiseOnline)
			{
				// Announce that the game has ended to remove it from the list.
				var gameServer = new GameServer(server);
				UpdateMasterServer(server, gameServer.ToPOSTData(false));
			}

			LanGameBeacon?.Stop();
		}

		public void LobbyInfoSynced(S server)
		{
			lastChanged = Game.RunTime;
		}

		public void GameStarted(S server)
		{
			lastChanged = Game.RunTime;
		}

		public void GameEnded(S server)
		{
			LanGameBeacon?.Stop();

			lastChanged = Game.RunTime;
		}

		void UpdateMasterServer(S server, string postData)
		{
			isBusy = true;

			Task.Run(async () =>
			{
				try
				{
					var endpoint = server.ModData.Manifest.Get<WebServices>().ServerAdvertise;

					var client = HttpClientFactory.Create();
					var response = await client.PostAsync(endpoint, new StringContent(postData));

					var masterResponseText = await response.Content.ReadAsStringAsync();

					if (isInitialPing)
					{
						Log.Write("server", "Master server: " + masterResponseText);
						var errorCode = 0;
						var errorMessage = string.Empty;

						if (!string.IsNullOrWhiteSpace(masterResponseText))
						{
							var regex = new Regex(@"^\[(?<code>-?\d+)\](?<message>.*)");
							var match = regex.Match(masterResponseText);
							errorMessage = match.Success && int.TryParse(match.Groups["code"].Value, out errorCode) ?
								match.Groups["message"].Value.Trim() : InvalidErrorCode;
						}

						isInitialPing = false;
						lock (masterServerMessages)
						{
							masterServerMessages.Enqueue(Connected);
							if (errorCode != 0)
							{
								// Hardcoded error messages take precedence over the server-provided messages
								if (!MasterServerErrors.TryGetValue(errorCode, out var message))
									message = errorMessage;

								masterServerMessages.Enqueue(message);

								// Positive error codes indicate errors that prevent advertisement
								// Negative error codes are non-fatal warnings
								if (errorCode > 0)
									masterServerMessages.Enqueue(GameOffline);
							}
						}
					}
				}
				catch (Exception ex)
				{
					Log.Write("server", ex.ToString());
					lock (masterServerMessages)
						masterServerMessages.Enqueue(Error);
				}

				isBusy = false;
			});
		}
	}
}
