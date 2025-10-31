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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public static class WorldUtils
	{
		/// <summary>
		/// From the given <paramref name="actors"/>, select the one nearest the given <paramref name="actor"/> by
		/// comparing their <see cref="Actor.CenterPosition"/>. No check is done to see if a path exists.
		/// </summary>
		public static Actor ClosestToIgnoringPath(this IEnumerable<Actor> actors, Actor actor)
		{
			return actors.ClosestToIgnoringPath(actor.CenterPosition);
		}

		/// <summary>
		/// From the given <paramref name="actors"/>, select the one nearest the given <paramref name="position"/> by
		/// comparing the <see cref="Actor.CenterPosition"/>. No check is done to see if a path exists.
		/// </summary>
		public static Actor ClosestToIgnoringPath(this IEnumerable<Actor> actors, WPos position)
		{
			return actors.MinByOrDefault(a => (a.CenterPosition - position).LengthSquared);
		}

		/// <summary>
		/// From the given <paramref name="items"/> that can be projected to <see cref="Actor"/>,
		/// select the one nearest the given <paramref name="actor"/> by
		/// comparing their <see cref="Actor.CenterPosition"/>. No check is done to see if a path exists.
		/// </summary>
		public static T ClosestToIgnoringPath<T>(IEnumerable<T> items, Func<T, Actor> selector, Actor actor)
		{
			return ClosestToIgnoringPath(items, selector, actor.CenterPosition);
		}

		/// <summary>
		/// From the given <paramref name="items"/> that can be projected to <see cref="Actor"/>,
		/// select the one nearest the given <paramref name="position"/> by
		/// comparing the <see cref="Actor.CenterPosition"/>. No check is done to see if a path exists.
		/// </summary>
		public static T ClosestToIgnoringPath<T>(IEnumerable<T> items, Func<T, Actor> selector, WPos position)
		{
			return items.MinByOrDefault(x => (selector(x).CenterPosition - position).LengthSquared);
		}

		/// <summary>
		/// From the given <paramref name="positions"/>, select the one nearest the given <paramref name="position"/>.
		/// No check is done to see if a path exists, as an actor is required for that.
		/// </summary>
		public static WPos ClosestToIgnoringPath(this IEnumerable<WPos> positions, WPos position)
		{
			return positions.MinByOrDefault(p => (p - position).LengthSquared);
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
			// PERF: This is a hot path and must run with minimal added overhead, so we enumerate manually
			// to allow us to call PerfTickLogger only once per iteration in the normal case.
			using (var enumerator = e.GetEnumerator())
			{
				var start = PerfTickLogger.GetTimestamp();
				while (enumerator.MoveNext())
				{
					a(enumerator.Current);
					start = PerfTickLogger.LogLongTick(start, text, enumerator.Current);
				}
			}
		}
	}
}
