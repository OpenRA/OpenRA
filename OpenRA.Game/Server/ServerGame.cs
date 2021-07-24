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

using System.Diagnostics;

namespace OpenRA.Server
{
	public class ServerGame
	{
		const int JankThreshold = 250;

		readonly Stopwatch gameTimer;

		public readonly OrderBuffer OrderBuffer;
		public int CurrentNetFrame { get; protected set; }
		long nextFrameTick;
		int netTimestep;

		int slowdownHold;
		int slowdownAmount;
		int AdjustedTimestep => (netTimestep + slowdownAmount).Clamp(1, 1000);

		public ServerGame(int worldTimeStep)
		{
			CurrentNetFrame = 1;
			netTimestep = worldTimeStep * Game.NewNetcodeNetTickScale;
			nextFrameTick = netTimestep;
			gameTimer = Stopwatch.StartNew();
			OrderBuffer = new OrderBuffer();
		}

		public bool TryTick(IFrameOrderDispatcher dispatcher)
		{
			var now = gameTimer.ElapsedMilliseconds;
			if (now < nextFrameTick)
				return false;

			var timestep = AdjustedTimestep;

			OrderBuffer.DispatchOrders(dispatcher, timestep);

			CurrentNetFrame++;
			if (now - nextFrameTick > JankThreshold)
				nextFrameTick = now + timestep;
			else
				nextFrameTick += timestep;

			if (slowdownHold > 0)
				slowdownHold--;

			if (slowdownHold == 0 && slowdownAmount > 0)
				slowdownAmount = slowdownAmount - (slowdownAmount / 4) - 1;

			return true;
		}

		public void SlowDown(int amount)
		{
			if (slowdownAmount < amount)
			{
				slowdownAmount = amount;
				slowdownHold = 5;
			}
		}
	}
}
