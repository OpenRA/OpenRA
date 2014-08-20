#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class PlayerPinger : ServerTrait, ITick
	{
		int PingInterval = 5000; // Ping every 5 seconds
		int ConnReportInterval = 20000; // Report every 20 seconds
		int ConnTimeout = 90000; // Drop unresponsive clients after 90 seconds

		// TickTimeout is in microseconds
		public int TickTimeout { get { return PingInterval * 100; } }

		int lastPing = 0;
		int lastConnReport = 0;
		bool isInitialPing = true;

		public void Tick(S server)
		{
			if ((Game.RunTime - lastPing > PingInterval) || isInitialPing)
			{
				isInitialPing = false;
				lastPing = Game.RunTime;
				foreach (var c in server.Conns.ToList())
				{
					if (!c.CanTimeout)
						continue;
					if (c.TimeSinceLastResponse < ConnTimeout)
					{
						server.SendOrderTo(c, "Ping", Game.RunTime.ToString());
						if (!c.TimeoutMessageShown && c.TimeSinceLastResponse > PingInterval * 2)
						{
							server.SendMessage(server.GetClient(c).Name + " is experiencing connection problems.");
							c.TimeoutMessageShown = true;
						}
					}
					else
					{
						server.SendMessage(server.GetClient(c).Name + " has been dropped after timing out.");
						server.DropClient(c, -1);
					}
				}
			}

			if (Game.RunTime - lastConnReport > ConnReportInterval)
			{
				lastConnReport = Game.RunTime;

				var timeouts = server.Conns
					.Where(c => c.CanTimeout && c.TimeSinceLastResponse > ConnReportInterval && c.TimeSinceLastResponse < ConnTimeout)
					.OrderBy(c => c.TimeSinceLastResponse);

				foreach (var c in timeouts)
					server.SendMessage("{0} will be dropped in {1} seconds.".F(
						server.GetClient(c).Name, (ConnTimeout - c.TimeSinceLastResponse) / 1000));
			}
		}
	}
}
