#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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

		// TickTimeout is in microseconds
		public int TickTimeout { get { return PingInterval * 100; } }

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
				if (server.LobbyInfo.IsSinglePlayer && !server.Dedicated)
					foreach (var c in server.Conns.ToList())
						server.SendOrderTo(c, "Ping", Game.RunTime.ToString());
				else
				{
					foreach (var c in server.Conns.ToList())
					{
						if (c == null || c.Socket == null)
							continue;

						var client = server.GetClient(c);
						if (client == null)
						{
							server.DropClient(c, -1);
							server.SendMessage("A player has been dropped after timing out.");
							continue;
						}

						if (c.TimeSinceLastResponse < ConnTimeout)
						{
							server.SendOrderTo(c, "Ping", Game.RunTime.ToString());
							if (!c.TimeoutMessageShown && c.TimeSinceLastResponse > PingInterval * 2)
							{
								server.SendMessage(client.Name + " is experiencing connection problems.");
								c.TimeoutMessageShown = true;
							}
						}
						else
						{
							server.SendMessage(client.Name + " has been dropped after timing out.");
							server.DropClient(c, -1);
						}
					}

					if (Game.RunTime - lastConnReport > ConnReportInterval)
					{
						lastConnReport = Game.RunTime;

						var timeouts = server.Conns
							.Where(c => c.TimeSinceLastResponse > ConnReportInterval && c.TimeSinceLastResponse < ConnTimeout)
							.OrderBy(c => c.TimeSinceLastResponse);

						foreach (var c in timeouts)
						{
							if (c == null || c.Socket == null)
								continue;

							var client = server.GetClient(c);
							if (client != null)
								server.SendMessage("{0} will be dropped in {1} seconds.".F(client.Name, (ConnTimeout - c.TimeSinceLastResponse) / 1000));
						}
					}
				}
			}
		}
	}
}
