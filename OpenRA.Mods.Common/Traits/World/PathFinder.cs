#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Calculates routes for mobile units with locomotors based on the A* search algorithm.", " Attach this to the world actor.")]
	public class PathFinderInfo : TraitInfo, Requires<LocomotorInfo>
	{
		public override object Create(ActorInitializer init)
		{
			return new PathFinder(init.World);
		}
	}

	public class PathFinder : IPathFinder
	{
		public static readonly List<CPos> NoPath = new List<CPos>(0);

		readonly World world;

		public PathFinder(World world)
		{
			this.world = world;
		}

		/// <summary>
		/// Calculates a path for the actor from multiple possible sources to target.
		/// Returned path is *reversed* and given target to source.
		/// The shortest path between a source and the target is returned.
		/// </summary>
		/// <remarks>
		/// Searches that provide a multiple source cells are slower than those than provide only a single source cell,
		/// as optimizations are possible for the single source case. Use searches from multiple source cells
		/// sparingly.
		/// </remarks>
		public List<CPos> FindUnitPathToTargetCell(
			Actor self, IEnumerable<CPos> sources, CPos target, BlockedByActor check,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true)
		{
			var sourcesList = sources.ToList();
			if (sourcesList.Count == 0)
				return NoPath;

			var locomotor = GetLocomotor(self);

			// If the target cell is inaccessible, bail early.
			var inaccessible =
				!locomotor.CanMoveFreelyInto(self, target, check, ignoreActor) ||
				(!(customCost is null) && customCost(target) == PathGraph.PathCostForInvalidPath);
			if (inaccessible)
				return NoPath;

			// When searching from only one source cell, some optimizations are possible.
			if (sourcesList.Count == 1)
			{
				var source = sourcesList[0];

				// For adjacent cells on the same layer, we can return the path without invoking a full search.
				if (source.Layer == target.Layer && (source - target).LengthSquared < 3)
					return new List<CPos>(2) { target, source };

				// With one starting point, we can use a bidirectional search.
				using (var fromTarget = PathSearch.ToTargetCell(
					world, locomotor, self, new[] { target }, source, check, ignoreActor: ignoreActor))
				using (var fromSource = PathSearch.ToTargetCell(
					world, locomotor, self, new[] { source }, target, check, ignoreActor: ignoreActor, inReverse: true))
					return PathSearch.FindBidiPath(fromTarget, fromSource);
			}

			// With multiple starting points, we can only use a unidirectional search.
			using (var search = PathSearch.ToTargetCell(
				world, locomotor, self, sourcesList, target, check, customCost, ignoreActor, laneBias))
				return search.FindPath();
		}

		/// <summary>
		/// Calculates a path for the actor from multiple possible sources, whilst searching for an acceptable target.
		/// Returned path is *reversed* and given target to source.
		/// The shortest path between a source and a discovered target is returned.
		/// </summary>
		/// <remarks>
		/// Searches with this method are slower than <see cref="FindUnitPathToTargetCell"/> due to the need to search for
		/// and discover an acceptable target cell. Use this search sparingly.
		/// </remarks>
		public List<CPos> FindUnitPathToTargetCellByPredicate(
			Actor self, IEnumerable<CPos> sources, Func<CPos, bool> targetPredicate, BlockedByActor check,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true)
		{
			// With no pre-specified target location, we can only use a unidirectional search.
			using (var search = PathSearch.ToTargetCellByPredicate(
				world, GetLocomotor(self), self, sources, targetPredicate, check, customCost, ignoreActor, laneBias))
				return search.FindPath();
		}

		static Locomotor GetLocomotor(Actor self)
		{
			// PERF: This PathFinder trait requires the use of Mobile, so we can be sure that is in use.
			// We can save some performance by avoiding querying for the Locomotor trait and retrieving it from Mobile.
			return ((Mobile)self.OccupiesSpace).Locomotor;
		}
	}
}
