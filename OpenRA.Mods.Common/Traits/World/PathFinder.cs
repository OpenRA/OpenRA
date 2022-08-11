#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
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
			return new PathFinder(init.Self);
		}
	}

	public class PathFinder : IPathFinder, IWorldLoaded
	{
		public static readonly List<CPos> NoPath = new List<CPos>(0);

		/// <summary>
		/// When searching for paths, use a default weight of 125% to reduce
		/// computation effort - even if this means paths may be sub-optimal.
		/// </summary>
		const int DefaultHeuristicWeightPercentage = 125;

		readonly World world;
		PathFinderOverlay pathFinderOverlay;
		Dictionary<Locomotor, HierarchicalPathFinder> hierarchicalPathFindersByLocomotor;

		public PathFinder(Actor self)
		{
			world = self.World;
		}

		public (
			IReadOnlyDictionary<CPos, List<GraphConnection>> AbstractGraph,
			IReadOnlyDictionary<CPos, uint> AbstractDomains) GetOverlayDataForLocomotor(Locomotor locomotor)
		{
			return hierarchicalPathFindersByLocomotor[locomotor].GetOverlayData();
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			pathFinderOverlay = world.WorldActor.TraitOrDefault<PathFinderOverlay>();

			// Requires<LocomotorInfo> ensures all Locomotors have been initialized.
			hierarchicalPathFindersByLocomotor = w.WorldActor.TraitsImplementing<Locomotor>().ToDictionary(
				locomotor => locomotor,
				locomotor => new HierarchicalPathFinder(world, locomotor));
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
		public List<CPos> FindPathToTargetCell(
			Actor self, IEnumerable<CPos> sources, CPos target, BlockedByActor check,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true)
		{
			var sourcesList = sources.ToList();
			if (sourcesList.Count == 0)
				return NoPath;

			var locomotor = GetActorLocomotor(self);

			// If the target cell is inaccessible, bail early.
			var inaccessible =
				!world.Map.Contains(target) ||
				!locomotor.CanMoveFreelyInto(self, target, check, ignoreActor) ||
				(customCost != null && customCost(target) == PathGraph.PathCostForInvalidPath);
			if (inaccessible)
				return NoPath;

			// When searching from only one source cell, some optimizations are possible.
			if (sourcesList.Count == 1)
			{
				var source = sourcesList[0];

				// For adjacent cells on the same layer, we can return the path without invoking a full search.
				if (source.Layer == target.Layer && (source - target).LengthSquared < 3)
				{
					// If the source cell is inaccessible, there is no path.
					if (!world.Map.Contains(source) ||
						(customCost != null && customCost(source) == PathGraph.PathCostForInvalidPath))
						return NoPath;
					return new List<CPos>(2) { target, source };
				}

				// Use a hierarchical path search, which performs a guided bidirectional search.
				return hierarchicalPathFindersByLocomotor[locomotor].FindPath(
					self, source, target, check, DefaultHeuristicWeightPercentage, customCost, ignoreActor, laneBias, pathFinderOverlay);
			}

			// Use a hierarchical path search, which performs a guided unidirectional search.
			return hierarchicalPathFindersByLocomotor[locomotor].FindPath(
				self, sourcesList, target, check, DefaultHeuristicWeightPercentage, customCost, ignoreActor, laneBias, pathFinderOverlay);
		}

		/// <summary>
		/// Calculates a path for the actor from multiple possible sources, whilst searching for an acceptable target.
		/// Returned path is *reversed* and given target to source.
		/// The shortest path between a source and a discovered target is returned.
		/// </summary>
		/// <remarks>
		/// Searches with this method are slower than <see cref="FindPathToTargetCell"/> due to the need to search for
		/// and discover an acceptable target cell. Use this search sparingly.
		/// </remarks>
		public List<CPos> FindPathToTargetCellByPredicate(
			Actor self, IEnumerable<CPos> sources, Func<CPos, bool> targetPredicate, BlockedByActor check,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true)
		{
			pathFinderOverlay?.NewRecording(self, sources, null);

			// With no pre-specified target location, we can only use a unidirectional search.
			using (var search = PathSearch.ToTargetCellByPredicate(
				world, GetActorLocomotor(self), self, sources, targetPredicate, check, customCost, ignoreActor, laneBias, pathFinderOverlay?.RecordLocalEdges(self)))
				return search.FindPath();
		}

		/// <summary>
		/// Determines if a path exists between source and target.
		/// Only terrain is taken into account, i.e. as if <see cref="BlockedByActor.None"/> was given.
		/// This would apply for any actor using the given <see cref="Locomotor"/>.
		/// </summary>
		public bool PathExistsForLocomotor(Locomotor locomotor, CPos source, CPos target)
		{
			return hierarchicalPathFindersByLocomotor[locomotor].PathExists(source, target);
		}

		static Locomotor GetActorLocomotor(Actor self)
		{
			// PERF: This PathFinder trait requires the use of Mobile, so we can be sure that is in use.
			// We can save some performance by avoiding querying for the Locomotor trait and retrieving it from Mobile.
			return ((Mobile)self.OccupiesSpace).Locomotor;
		}
	}
}
