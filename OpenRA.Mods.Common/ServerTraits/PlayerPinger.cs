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
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
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
			if ((Game.RunTime - lastPing > PingInterval) || isInitialPing)
			{
				isInitialPing = false;
				lastPing = Game.RunTime;
				foreach (var p in server.Conns)
					server.SendOrderTo(p, "Ping", Game.RunTime.ToString());
			}
		}
	}
}
