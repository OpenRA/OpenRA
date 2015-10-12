#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public static class WorldUtils
	{
		public static Actor ClosestTo(this IEnumerable<Actor> actors, Actor a)
		{
			return actors.ClosestTo(a.CenterPosition);
		}

		public static Actor ClosestTo(this IEnumerable<Actor> actors, WPos pos)
		{
			return actors.MinByOrDefault(a => (a.CenterPosition - pos).LengthSquared);
		}

		public static IEnumerable<Actor> FindActorsInCircle(this World world, WPos origin, WDist r)
		{
			using (new PerfSample("FindUnitsInCircle"))
			{
				// Target ranges are calculated in 2D, so ignore height differences
				var vec = new WVec(r, r, WDist.Zero);
				return world.ActorMap.ActorsInBox(origin - vec, origin + vec).Where(
					a => (a.CenterPosition - origin).HorizontalLengthSquared <= r.LengthSquared);
			}
		}

		public static void DoTimed<T>(this IEnumerable<T> e, Action<T> a, string text)
		{
			// Note - manual enumeration here for performance due to high call volume.
			var longTickThresholdInStopwatchTicks = PerfTimer.LongTickThresholdInStopwatchTicks;
			using (var enumerator = e.GetEnumerator())
			{
				var start = Stopwatch.GetTimestamp();
				while (enumerator.MoveNext())
				{
					a(enumerator.Current);
					var current = Stopwatch.GetTimestamp();
					if (current - start > longTickThresholdInStopwatchTicks)
					{
						PerfTimer.LogLongTick(start, current, text, enumerator.Current);
						start = Stopwatch.GetTimestamp();
					}
					else
						start = current;
				}
			}
		}

		public static bool AreMutualAllies(Player a, Player b)
		{
			return a.Stances[b] == Stance.Ally &&
				b.Stances[a] == Stance.Ally;
		}
	}
}
