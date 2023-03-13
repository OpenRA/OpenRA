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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Calculates routes for mobile actors with locomotors based on the A* search algorithm.", " Attach this to the world actor.")]
	public class PathFinderInfo : TraitInfo, Requires<LocomotorInfo>, Requires<ActorMapInfo>
	{
		public override object Create(ActorInitializer init)
		{
			return new PathFinder(init.Self);
		}
	}

	public class PathFinder : IPathFinder, IWorldLoaded
	{
		public static readonly List<CPos> NoPath = new(0);

		/// <summary>
		/// When searching for paths, use a default weight of 125% to reduce
		/// computation effort - even if this means paths may be sub-optimal.
		/// </summary>
		const int DefaultHeuristicWeightPercentage = 125;

		readonly World world;
		PathFinderOverlay pathFinderOverlay;
		Dictionary<Locomotor, HierarchicalPathFinder> hierarchicalPathFindersBlockedByNoneByLocomotor;
		Dictionary<Locomotor, HierarchicalPathFinder> hierarchicalPathFindersBlockedByImmovableByLocomotor;

		public PathFinder(Actor self)
		{
			world = self.World;
		}

		public (
			IReadOnlyDictionary<CPos, List<GraphConnection>> AbstractGraph,
			IReadOnlyDictionary<CPos, uint> AbstractDomains) GetOverlayDataForLocomotor(
			Locomotor locomotor, BlockedByActor check)
		{
			return GetHierarchicalPathFinder(locomotor, check, null).GetOverlayData();
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			pathFinderOverlay = world.WorldActor.TraitOrDefault<PathFinderOverlay>();

			// Requires<LocomotorInfo> ensures all Locomotors have been initialized.
			var locomotors = w.WorldActor.TraitsImplementing<Locomotor>().ToList();
			hierarchicalPathFindersBlockedByNoneByLocomotor = locomotors.ToDictionary(
				locomotor => locomotor,
				locomotor => new HierarchicalPathFinder(world, locomotor, w.ActorMap, BlockedByActor.None));
			hierarchicalPathFindersBlockedByImmovableByLocomotor = locomotors.ToDictionary(
				locomotor => locomotor,
				locomotor => new HierarchicalPathFinder(world, locomotor, w.ActorMap, BlockedByActor.Immovable));
		}

		/// <summary>
		/// Calculates a path for the actor from multiple possible sources to target.
		/// Returned path is *reversed* and given target to source.
		/// The shortest path between a source and the target is returned.
		/// </summary>
		/// <remarks>
		/// <para>
		/// It is allowed for an actor to occupy an inaccessible space and move out of it if another adjacent cell is
		/// accessible, but it is not allowed to move into an inaccessible target space. Therefore it is vitally
		/// important to not mix up the source and target locations. A path can exist from an inaccessible source space
		/// to an accessible target space, but if those parameters as swapped then no path can exist.
		/// </para>
		/// <para>
		/// Searches that provide multiple source cells are slower than those than provide only a single source cell,
		/// as optimizations are possible for the single source case. Use searches from multiple source cells
		/// sparingly.
		/// </para>
		/// </remarks>
		public List<CPos> FindPathToTargetCell(
			Actor self, IEnumerable<CPos> sources, CPos target, BlockedByActor check,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true)
		{
			return FindPathToTarget(self, sources, target, check, customCost, ignoreActor, laneBias);
		}

		/// <summary>
		/// Calculates a path for the actor from source to multiple possible targets.
		/// Returned path is *reversed* and given target to source.
		/// The shortest path between the source and a target is returned.
		/// </summary>
		/// <remarks>
		/// <para>
		/// It is allowed for an actor to occupy an inaccessible space and move out of it if another adjacent cell is
		/// accessible, but it is not allowed to move into an inaccessible target space. Therefore it is vitally
		/// important to not mix up the source and target locations. A path can exist from an inaccessible source space
		/// to an accessible target space, but if those parameters as swapped then no path can exist.
		/// </para>
		/// <para>
		/// Searches that provide multiple target cells are slower than those than provide only a single target cell,
		/// as optimizations are possible for the single target case. Use searches to multiple target cells
		/// sparingly.
		/// </para>
		/// </remarks>
		public List<CPos> FindPathToTargetCells(
			Actor self, CPos source, IEnumerable<CPos> targets, BlockedByActor check,
			Func<CPos, int> customCost = null,
			Actor ignoreActor = null,
			bool laneBias = true)
		{
			// We can reuse existing search infrastructure by swapping the single source and multiple targets,
			// and calling the existing methods that allow multiple sources and one target.
			// However there is a case of asymmetry we must handle, an actor may move out of a inaccessible source,
			// but may not move onto a inaccessible target. We must account for this when performing the swap.

			// As targets must be accessible, determine accessible targets in advance so when they becomes the sources
			// we don't accidentally allow an inaccessible position to become viable.
			var locomotor = GetActorLocomotor(self);
			var accessibleTargets = targets
				.Where(target =>
					PathSearch.CellAllowsMovement(self.World, locomotor, target, customCost)
					&& locomotor.MovementCostToEnterCell(self, target, check, ignoreActor, true) != PathGraph.MovementCostForUnreachableCell)
				.ToList();
			if (accessibleTargets.Count == 0)
				return NoPath;

			// When checking if the source location is accessible, we must also ignore self.
			// So that when it becomes a target, we don't consider the location blocked by ourselves!
			List<CPos> path;
			var sourceIsAccessible =
				PathSearch.CellAllowsMovement(self.World, locomotor, source, customCost)
				&& locomotor.MovementCostToEnterCell(self, source, check, ignoreActor, true) != PathGraph.MovementCostForUnreachableCell;
			if (sourceIsAccessible)
			{
				// As both ends are accessible, we can freely swap them.
				path = FindPathToTarget(self, targets, source, check, customCost, ignoreActor, laneBias);
			}
			else
			{
				// When we treat the source as a target, we need to be able to path to it.
				// We know this would fail as it is inaccessible but we need an exception to be made.
				// A hierarchical path search doesn't support this ability,
				// but the local pathfinder can deal with it when doing reverse searches.
				pathFinderOverlay?.NewRecording(self, accessibleTargets, source);
				using (var search = PathSearch.ToTargetCell(
					world, locomotor, self, accessibleTargets, source, check, DefaultHeuristicWeightPercentage,
					customCost, ignoreActor, laneBias, inReverse: true, recorder: pathFinderOverlay?.RecordLocalEdges(self)))
					path = search.FindPath();
			}

			// Since we swapped the positions, we need to reverse the path to swap it back.
			path.Reverse();
			return path;
		}

		List<CPos> FindPathToTarget(
			Actor self, IEnumerable<CPos> sources, CPos target, BlockedByActor check,
			Func<CPos, int> customCost, Actor ignoreActor, bool laneBias)
		{
			var sourcesList = sources.ToList();
			if (sourcesList.Count == 0)
				return NoPath;

			var locomotor = GetActorLocomotor(self);

			// If the target cell is inaccessible, bail early.
			// The destination cell must allow movement and also have a reachable movement cost.
			if (!PathSearch.CellAllowsMovement(self.World, locomotor, target, customCost)
				|| locomotor.MovementCostToEnterCell(self, target, check, ignoreActor) == PathGraph.MovementCostForUnreachableCell)
				return NoPath;

			// When searching from only one source cell, some optimizations are possible.
			if (sourcesList.Count == 1)
			{
				var source = sourcesList[0];

				// For adjacent cells on the same layer, we can return the path without invoking a full search.
				if (source.Layer == target.Layer && (source - target).LengthSquared < 3)
				{
					// If the source cell is inaccessible, there is no path.
					// Unlike the destination cell, the source cell is allowed to have an unreachable movement cost.
					if (!PathSearch.CellAllowsMovement(self.World, locomotor, source, customCost))
						return NoPath;
					return new List<CPos>(2) { target, source };
				}

				// Use a hierarchical path search, which performs a guided bidirectional search.
				return GetHierarchicalPathFinder(locomotor, check, ignoreActor).FindPath(
					self, source, target, check, DefaultHeuristicWeightPercentage, customCost, ignoreActor, laneBias, pathFinderOverlay);
			}

			// Use a hierarchical path search, which performs a guided unidirectional search.
			return GetHierarchicalPathFinder(locomotor, check, ignoreActor).FindPath(
				self, sourcesList, target, check, DefaultHeuristicWeightPercentage, customCost, ignoreActor, laneBias, pathFinderOverlay);
		}

		HierarchicalPathFinder GetHierarchicalPathFinder(Locomotor locomotor, BlockedByActor check, Actor ignoreActor)
		{
			// If there is an actor to ignore, we cannot use an HPF that accounts for any blocking actors.
			// One of the blocking actors might be the one we need to ignore!
			var hpfs = check == BlockedByActor.None || ignoreActor != null
				? hierarchicalPathFindersBlockedByNoneByLocomotor
				: hierarchicalPathFindersBlockedByImmovableByLocomotor;
			return hpfs[locomotor];
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
		/// <remarks>
		/// It is allowed for an actor to occupy an inaccessible space and move out of it if another adjacent cell is
		/// accessible, but it is not allowed to move into an inaccessible target space. Therefore it is vitally
		/// important to not mix up the source and target locations. A path can exist from an inaccessible source space
		/// to an accessible target space, but if those parameters as swapped then no path can exist.
		/// </remarks>
		public bool PathExistsForLocomotor(Locomotor locomotor, CPos source, CPos target)
		{
			return hierarchicalPathFindersBlockedByNoneByLocomotor[locomotor].PathExists(source, target);
		}

		static Locomotor GetActorLocomotor(Actor self)
		{
			// PERF: This PathFinder trait requires the use of Mobile, so we can be sure that is in use.
			// We can save some performance by avoiding querying for the Locomotor trait and retrieving it from Mobile.
			return ((Mobile)self.OccupiesSpace).Locomotor;
		}
	}
}
