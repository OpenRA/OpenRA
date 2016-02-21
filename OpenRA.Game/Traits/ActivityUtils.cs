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

using System.Diagnostics;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Support;

namespace OpenRA.Traits
{
	public static class ActivityUtils
	{
		public static Activity RunActivity(Actor self, Activity act)
		{
			// PERF: If there are no activities we can bail straight away and save ourselves a call to
			// Stopwatch.GetTimestamp.
			if (act == null)
				return act;

			// PERF: This is a hot path and must run with minimal added overhead.
			// Calling Stopwatch.GetTimestamp is a bit expensive, so we enumerate manually to allow us to call it only
			// once per iteration in the normal case.
			// See also: DoTimed
			var longTickThresholdInStopwatchTicks = PerfTimer.LongTickThresholdInStopwatchTicks;
			var start = Stopwatch.GetTimestamp();
			while (act != null)
			{
				var prev = act;
				act = act.Tick(self);
				var current = Stopwatch.GetTimestamp();
				if (current - start > longTickThresholdInStopwatchTicks)
				{
					PerfTimer.LogLongTick(start, current, "Activity", prev);
					start = Stopwatch.GetTimestamp();
				}
				else
					start = current;

				if (prev == act)
					break;
			}

			return act;
		}

		public static Activity SequenceActivities(params Activity[] acts)
		{
			return acts.Reverse().Aggregate(
				(next, a) => { a.Queue(next); return a; });
		}
	}
}
