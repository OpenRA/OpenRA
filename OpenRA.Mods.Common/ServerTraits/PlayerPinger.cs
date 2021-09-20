#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class PlayerPinger : ServerTrait, ITick
	{
		static readonly int PingInterval = 5000; // Ping every 5 seconds
		static readonly int ConnReportInterval = 20000; // Report every 20 seconds
		static readonly int ConnTimeout = 60000; // Drop unresponsive clients after 60 seconds

		long lastPing = 0;
		long lastConnReport = 0;
		bool isInitialPing = true;

		public void Tick(S server)
		{
			if ((Game.RunTime - lastPing > PingInterval) || isInitialPing)
			{
				isInitialPing = false;
				lastPing = Game.RunTime;

				// Ignore client timeout in singleplayer games to make debugging easier
				var nonBotClientCount = 0;
				lock (server.LobbyInfo)
					nonBotClientCount = server.LobbyInfo.NonBotClients.Count();

				if (nonBotClientCount >= 2 || server.Type == ServerType.Dedicated)
				{
					foreach (var c in server.Conns.ToList())
					{
						if (!c.Validated)
							continue;

						var client = server.GetClient(c);
						if (client == null)
						{
							server.DropClient(c);
							server.SendMessage("A player has been dropped after timing out.");
							continue;
						}

						if (c.TimeSinceLastResponse < ConnTimeout)
						{
							if (!c.TimeoutMessageShown && c.TimeSinceLastResponse > PingInterval * 2)
							{
								server.SendMessage(client.Name + " is experiencing connection problems.");
								c.TimeoutMessageShown = true;
							}
						}
						else
						{
							server.SendMessage(client.Name + " has been dropped after timing out.");
							server.DropClient(c);
						}
					}

					if (Game.RunTime - lastConnReport > ConnReportInterval)
					{
						lastConnReport = Game.RunTime;

						var timeouts = server.Conns
							.Where(c => c.Validated && c.TimeSinceLastResponse > ConnReportInterval && c.TimeSinceLastResponse < ConnTimeout)
							.OrderBy(c => c.TimeSinceLastResponse);

						foreach (var c in timeouts)
						{
							var client = server.GetClient(c);
							if (client != null)
								server.SendMessage($"{client.Name} will be dropped in {(ConnTimeout - c.TimeSinceLastResponse) / 1000} seconds.");
						}
					}
				}
			}
		}
	}
}
