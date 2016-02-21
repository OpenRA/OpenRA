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

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common
{
	public static class WorldExtensions
	{
		/// <summary>
		/// Finds all the actors of which their health radius is intersected by a line (with a definable width) between two points.
		/// </summary>
		/// <param name="world">The engine world the line intersection is to be done in.</param>
		/// <param name="lineStart">The position the line should start at</param>
		/// <param name="lineEnd">The position the line should end at</param>
		/// <param name="lineWidth">How close an actor's health radius needs to be to the line to be considered 'intersected' by the line</param>
		/// <returns>A list of all the actors intersected by the line</returns>
		public static IEnumerable<Actor> FindActorsOnLine(this World world, WPos lineStart, WPos lineEnd, WDist lineWidth, WDist targetExtraSearchRadius)
		{
			// This line intersection check is done by first just finding all actors within a square that starts at the source, and ends at the target.
			// Then we iterate over this list, and find all actors for which their health radius is at least within lineWidth of the line.
			// For actors without a health radius, we simply check their center point.
			// The square in which we select all actors must be large enough to encompass the entire line's width.
			var xDir = Math.Sign(lineEnd.X - lineStart.X);
			var yDir = Math.Sign(lineEnd.Y - lineStart.Y);

			var dir = new WVec(xDir, yDir, 0);
			var overselect = dir * (1024 + lineWidth.Length + targetExtraSearchRadius.Length);
			var finalTarget = lineEnd + overselect;
			var finalSource = lineStart - overselect;

			var actorsInSquare = world.ActorMap.ActorsInBox(finalTarget, finalSource);
			var intersectedActors = new List<Actor>();

			foreach (var currActor in actorsInSquare)
			{
				var actorWidth = 0;
				var healthInfo = currActor.Info.TraitInfoOrDefault<HealthInfo>();
				if (healthInfo != null)
					actorWidth = healthInfo.Shape.OuterRadius.Length;

				var projection = MinimumPointLineProjection(lineStart, lineEnd, currActor.CenterPosition);
				var distance = (currActor.CenterPosition - projection).HorizontalLength;
				var maxReach = actorWidth + lineWidth.Length;

				if (distance <= maxReach)
					intersectedActors.Add(currActor);
			}

			return intersectedActors;
		}

		/// <summary>
		/// Find the point (D) on a line (A-B) that is closest to the target point (C).
		/// </summary>
		/// <param name="lineStart">The source point (tail) of the line</param>
		/// <param name="lineEnd">The target point (head) of the line</param>
		/// <param name="point">The target point that the minimum distance should be found to</param>
		/// <returns>The WPos that is the point on the line that is closest to the target point</returns>
		public static WPos MinimumPointLineProjection(WPos lineStart, WPos lineEnd, WPos point)
		{
			var squaredLength = (lineEnd - lineStart).HorizontalLengthSquared;

			// Line has zero length, so just use the lineEnd position as the closest position.
			if (squaredLength == 0)
				return lineEnd;

			// Consider the line extending the segment, parameterized as target + t (source - target).
			// We find projection of point onto the line.
			// It falls where t = [(point - target) . (source - target)] / |source - target|^2
			// The normal DotProduct math would be (xDiff + yDiff) / dist, where dist = (target - source).LengthSquared;
			// But in order to avoid floating points, we do not divide here, but rather work with the large numbers as far as possible.
			// We then later divide by dist, only AFTER we have multiplied by the dotproduct.
			var xDiff = ((long)point.X - lineEnd.X) * (lineStart.X - lineEnd.X);
			var yDiff = ((long)point.Y - lineEnd.Y) * (lineStart.Y - lineEnd.Y);
			var t = xDiff + yDiff;

			// Beyond the 'target' end of the segment
			if (t < 0)
				return lineEnd;

			// Beyond the 'source' end of the segment
			if (t > squaredLength)
				return lineStart;

			// Projection falls on the segment
			return WPos.Lerp(lineEnd, lineStart, t, squaredLength);
		}
	}
}
