#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

		public static WPos PositionClosestTo(this IEnumerable<WPos> positions, WPos pos)
		{
			return positions.MinByOrDefault(p => (p - pos).LengthSquared);
		}

		public static IEnumerable<Actor> FindActorsInCircle(this World world, WPos origin, WDist r)
		{
			// Target ranges are calculated in 2D, so ignore height differences
			var vec = new WVec(r, r, WDist.Zero);
			return world.ActorMap.ActorsInBox(origin - vec, origin + vec).Where(
				a => (a.CenterPosition - origin).HorizontalLengthSquared <= r.LengthSquared);
		}

		public static bool ContainsTemporaryBlocker(this World world, CPos cell, Actor ignoreActor = null)
		{
			if (!world.RulesContainTemporaryBlocker)
				return false;

			var temporaryBlockers = world.ActorMap.GetActorsAt(cell);
			foreach (var temporaryBlocker in temporaryBlockers)
			{
				if (temporaryBlocker == ignoreActor)
					continue;

				var temporaryBlockerTraits = temporaryBlocker.TraitsImplementing<ITemporaryBlocker>();
				foreach (var temporaryBlockerTrait in temporaryBlockerTraits)
					if (temporaryBlockerTrait.IsBlocking(temporaryBlocker, cell))
						return true;
			}

			return false;
		}

		public static void DoTimed<T>(this IEnumerable<T> e, Action<T> a, string text)
		{
			// PERF: This is a hot path and must run with minimal added overhead.
			// Calling Stopwatch.GetTimestamp is a bit expensive, so we enumerate manually to allow us to call it only
			// once per iteration in the normal case.
			// See also: RunActivity
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
	}
}
