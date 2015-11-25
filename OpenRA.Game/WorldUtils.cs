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
using OpenRA.Graphics;
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

		public static IEnumerable<Actor> SelectActorsOnScreen(World world, Viewport viewport, IEnumerable<string> selectionClasses, Player player)
		{
			return SelectActorsByOwnerAndSelectionClass(world.ScreenMap.ActorsInBox(viewport.TopLeft, viewport.BottomRight), player, selectionClasses);
		}

		public static IEnumerable<Actor> SelectActorsInWorld(World world, IEnumerable<string> selectionClasses, Player player)
		{
			return SelectActorsByOwnerAndSelectionClass(world.ActorMap.ActorsInWorld(), player, selectionClasses);
		}

		public static IEnumerable<Actor> SelectActorsByOwnerAndSelectionClass(IEnumerable<Actor> actors, Player owner, IEnumerable<string> selectionClasses)
		{
			return actors.Where(a =>
			{
				if (a.Owner != owner)
					return false;

				var s = a.TraitOrDefault<Selectable>();

				// selectionClasses == null means that units, that meet all other criteria, get selected
				return s != null && (selectionClasses == null || selectionClasses.Contains(s.Class));
			});
		}

		public static IEnumerable<Actor> SelectActorsInBoxWithDeadzone(World world, int2 a, int2 b)
		{
			if ((a - b).Length <= Game.Settings.Game.SelectionDeadzone)
				return world.ScreenMap.ActorsAt(a).Where(x => IsSelectable(x)).SubsetWithHighestSelectionPriority();

			return world.ScreenMap.ActorsInBox(a, b).Where(x => IsSelectable(x)).SubsetWithHighestSelectionPriority();
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

		public static bool IsSelectable(Actor a)
		{
			return a.IsInWorld && a.Info.HasTraitInfo<SelectableInfo>() &&
				(a.Owner.IsAlliedWith(a.World.RenderPlayer) || !a.World.FogObscures(a));
		}
	}
}
