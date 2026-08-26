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

namespace OpenRA.Mods.Tcd.Formations
{
	// Spreads N units evenly along a drawn path. Both input gestures - dragging a
	// freehand shape and dropping a series of points - end up here, because both are
	// just a polyline with units to distribute along it.
	//
	// Pure geometry: no World, no Actor, so it is unit testable.
	public static class FormationPath
	{
		// Returns count positions spaced by equal arc length along the path.
		// A closed path wraps, so the last unit does not land on top of the first.
		public static WPos[] Distribute(IReadOnlyList<WPos> path, int count, bool closed)
		{
			ArgumentNullException.ThrowIfNull(path);
			if (count <= 0)
				return [];
			if (path.Count == 0)
				throw new ArgumentException("Path needs at least one point.", nameof(path));

			var slots = new WPos[count];
			if (path.Count == 1)
			{
				for (var i = 0; i < count; i++)
					slots[i] = path[0];

				return slots;
			}

			var points = new List<WPos>(path);
			if (closed && path.Count > 2)
				points.Add(path[0]);

			// Arc length up to each vertex.
			var cumulative = new double[points.Count];
			for (var i = 1; i < points.Count; i++)
				cumulative[i] = cumulative[i - 1] + Distance(points[i - 1], points[i]);

			var total = cumulative[^1];
			if (total <= 0)
			{
				for (var i = 0; i < count; i++)
					slots[i] = points[0];

				return slots;
			}

			// An open path puts a unit on each end; a closed one leaves a gap so the
			// ring stays evenly spaced all the way round.
			var divisor = closed && path.Count > 2 ? count : Math.Max(1, count - 1);

			var segment = 0;
			for (var i = 0; i < count; i++)
			{
				var target = total * i / divisor;
				while (segment < points.Count - 2 && cumulative[segment + 1] < target)
					segment++;

				var spanStart = cumulative[segment];
				var spanLength = cumulative[segment + 1] - spanStart;
				var fraction = spanLength <= 0 ? 0 : (target - spanStart) / spanLength;

				slots[i] = Lerp(points[segment], points[segment + 1], fraction);
			}

			return slots;
		}

		static double Distance(WPos a, WPos b)
		{
			double dx = b.X - a.X;
			double dy = b.Y - a.Y;
			return Math.Sqrt(dx * dx + dy * dy);
		}

		static WPos Lerp(WPos a, WPos b, double t)
		{
			return new WPos(
				a.X + (int)Math.Round((b.X - a.X) * t),
				a.Y + (int)Math.Round((b.Y - a.Y) * t),
				0);
		}
	}
}
