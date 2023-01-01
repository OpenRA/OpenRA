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

using OpenRA.Support;
using Activity = OpenRA.Activities.Activity;

namespace OpenRA.Traits
{
	public static class ActivityUtils
	{
		public static Activity RunActivity(Actor self, Activity act)
		{
			// PERF: This is a hot path and must run with minimal added overhead.
			// If there are no activities we can bail straight away and save ourselves the overhead of setting up the perf logging.
			if (act == null)
				return act;

			var perfLogger = new PerfTickLogger();
			perfLogger.Start();
			while (act != null)
			{
				var prev = act;
				act = act.TickOuter(self);
				perfLogger.LogTickAndRestartTimer("Activity", prev);

				if (act == prev)
					break;
			}

			return act;
		}
	}
}
