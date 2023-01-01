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

using System.Diagnostics;

namespace OpenRA.Support
{
	public sealed class PerfTickLogger
	{
		readonly DebugSettings settings = Game.Settings.Debug;
		readonly long threshold = PerfTimer.LongTickThresholdInStopwatchTicks;
		long start;
		long current;
		bool enabled;

		long CurrentTimestamp => enabled ? Stopwatch.GetTimestamp() : 0L;

		public void Start()
		{
			enabled = settings.EnableSimulationPerfLogging;
			start = CurrentTimestamp;
		}

		public void LogTickAndRestartTimer(string name, object item)
		{
			if (!enabled)
				return;

			current = CurrentTimestamp;
			if (current - start > threshold)
			{
				PerfTimer.LogLongTick(start, current, name, item);
				start = CurrentTimestamp;
				return;
			}

			start = current;
		}
	}
}
