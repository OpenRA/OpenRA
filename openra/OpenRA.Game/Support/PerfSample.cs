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
using System.Diagnostics;

namespace OpenRA.Support
{
	public readonly struct PerfSample : IDisposable
	{
		readonly string item;
		readonly long ticks;

		public PerfSample(string item)
		{
			this.item = item;
			ticks = Stopwatch.GetTimestamp();
		}

		public void Dispose()
		{
			PerfHistory.Increment(item, 1000.0 * Math.Max(0, Stopwatch.GetTimestamp() - ticks) / Stopwatch.Frequency);
		}
	}
}
