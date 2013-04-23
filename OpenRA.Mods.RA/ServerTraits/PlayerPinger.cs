#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Server;
using OpenRA.Network;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.RA.Server
{
	public class PlayerPinger : ServerTrait, ITick
	{
		int PingInterval = 5000; // Ping every 5 seconds

		// TickTimeout is in microseconds
		public int TickTimeout { get { return PingInterval * 100; } }

		int lastPing = 0;
		bool isInitialPing = true;
		public void Tick(S server)
		{
			if ((Environment.TickCount - lastPing > PingInterval) || isInitialPing)
			{
				isInitialPing = false;
				lastPing = Environment.TickCount;
				foreach (var p in server.conns)
					server.SendOrderTo(p, "Ping", Environment.TickCount.ToString());
			}
		}
	}
}
