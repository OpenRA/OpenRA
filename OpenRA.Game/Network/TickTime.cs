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

namespace OpenRA.Network
{
	public sealed class TickTime
	{
		public int Timestep;

		public TickTime(int timestep, long lastTickTime)
		{
			Timestep = timestep;
			Value = lastTickTime;
		}

		public long Value { get; set; }

		public bool ShouldAdvance(long tick)
		{
			if (Timestep == 0)
				return false;

			var tickDelta = tick - Value;
			return tickDelta >= Timestep;
		}

		public void AdvanceTickTime(long tick)
		{
			var tickDelta = tick - Value;

			var integralTickTimestep = tickDelta / Timestep * Timestep;
			Value += integralTickTimestep >= Game.TimestepJankThreshold
				? integralTickTimestep
				: Timestep;
		}
	}
}
