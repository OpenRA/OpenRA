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

		public TickTime(Func<int> timestep, long lastTickTime)
		{
			this.timestep = timestep;
			Value = lastTickTime;
		}

		public long Value { get; set; }

		public bool ShouldAdvance(long tick)
		{
			var i = timestep();

			if (i == 0)
				return false;

			var tickDelta = tick - Value;
			return tickDelta >= i;
		}

		public void AdvanceTickTime(long tick)
		{
			var tickDelta = tick - Value;

			var currentTimestep = timestep();

			var integralTickTimestep = tickDelta / currentTimestep * currentTimestep;
			Value += integralTickTimestep >= Game.TimestepJankThreshold
				? integralTickTimestep
				: currentTimestep;
		}
	}
}
