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

namespace OpenRA.Network
{
	public sealed class TickTime
	{
		readonly Func<int> timestep;

		long lastTickTime;

		public TickTime(Func<int> timestep, long lastTickTime)
		{
			this.timestep = timestep;
			this.lastTickTime = lastTickTime;
		}

		public long Value
		{
			get => lastTickTime;
			set => lastTickTime = value;
		}

		public bool ShouldAdvance(long tick)
		{
			var i = timestep();

			if (i == 0)
				return false;

			var tickDelta = tick - lastTickTime;
			return tickDelta >= i;
		}

		public void AdvanceTickTime(long tick)
		{
			var tickDelta = tick - lastTickTime;

			var currentTimestep = timestep();

			var integralTickTimestep = tickDelta / currentTimestep * currentTimestep;
			lastTickTime += integralTickTimestep >= Game.TimestepJankThreshold
				? integralTickTimestep
				: currentTimestep;
		}
	}
}
