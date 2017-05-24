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
			var mod = server.ModData.Manifest;

			// important to grab these on the main server thread, not in the worker we're about to spawn -- they may be modified
			// by the main thread as clients join and leave.
			var numPlayers = server.LobbyInfo.Clients.Where(c1 => c1.Bot == null && c1.Slot != null).Count();
			var numBots = server.LobbyInfo.Clients.Where(c1 => c1.Bot != null).Count();
			var numSpectators = server.LobbyInfo.Clients.Where(c1 => c1.Bot == null && c1.Slot == null).Count();
			var numSlots = server.LobbyInfo.Slots.Where(s => !s.Value.Closed).Count() - numBots;
			var passwordProtected = !string.IsNullOrEmpty(server.Settings.Password);
			var clients = server.LobbyInfo.Clients.Where(c1 => c1.Bot == null).Select(c => Convert.ToBase64String(Encoding.UTF8.GetBytes(c.Name))).ToArray();

			UpdateMasterServer(server, numPlayers, numSlots, numBots, numSpectators, mod, passwordProtected, clients);
			if (LanGameBeacon != null)
				UpdateLANGameBeacon(server, numPlayers, numSlots, numBots, numSpectators, mod, passwordProtected);
		}

		void UpdateMasterServer(S server, int numPlayers, int numSlots, int numBots, int numSpectators, Manifest mod, bool passwordProtected, string[] clients)
		{
			if (isBusy || !server.Settings.AdvertiseOnline)
				return;

			lastPing = Game.RunTime;
			isBusy = true;

			Action a = () =>
			{
				try
				{
					var url = "ping?port={0}&name={1}&state={2}&players={3}&bots={4}&mods={5}&map={6}&maxplayers={7}&spectators={8}&protected={9}&clients={10}";
					if (isInitialPing) url += "&new=1";

					var serverList = server.ModData.Manifest.Get<WebServices>().ServerList;
					using (var wc = new WebClient())
					{
						wc.Proxy = null;
						var masterResponse = wc.DownloadData(
							serverList + url.F(
							server.Settings.ExternalPort, Uri.EscapeUriString(server.Settings.Name),
							(int)server.State,
							numPlayers,
							numBots,
							"{0}@{1}".F(mod.Id, mod.Metadata.Version),
							server.LobbyInfo.GlobalSettings.Map,
							numSlots,
							numSpectators,
							passwordProtected ? 1 : 0,
							string.Join(",", clients)));

						if (isInitialPing)
						{
							var masterResponseText = Encoding.UTF8.GetString(masterResponse);
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

		void UpdateLANGameBeacon(S server, int numPlayers, int numSlots, int numBots, int numSpectators, Manifest mod, bool passwordProtected)
		{
			var settings = server.Settings;

			// TODO: Serialize and send client names
			var lanGameYaml =
@"Game:
	Id: {0}
	Name: {1}
	Address: {2}:{3}
	State: {4}
	Players: {5}
	MaxPlayers: {6}
	Bots: {7}
	Spectators: {8}
	Map: {9}
	Mods: {10}@{11}
	Protected: {12}".F(Platform.SessionGUID, settings.Name, server.Ip, settings.ListenPort, (int)server.State, numPlayers, numSlots, numBots, numSpectators,
				server.Map.Uid, mod.Id, mod.Metadata.Version, passwordProtected);

			LanGameBeacon.BeaconData = lanGameYaml;
		}
	}
}
