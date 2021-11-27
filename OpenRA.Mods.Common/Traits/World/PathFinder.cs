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

using System.Collections.Generic;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Calculates routes for mobile units based on the A* search algorithm.", " Attach this to the world actor.")]
	public class PathFinderInfo : TraitInfo, Requires<LocomotorInfo>
	{
		public override object Create(ActorInitializer init)
		{
			return new PathFinder(init.World);
		}
	}

	public interface IPathFinder
	{
		/// <summary>
		/// Calculates a path for the actor from source to target.
		/// Returned path is *reversed* and given target to source.
		/// </summary>
		List<CPos> FindUnitPath(CPos source, CPos target, Actor self, Actor ignoreActor, BlockedByActor check);

		/// <summary>
		/// Expands the path search until a path is found, and returns that path.
		/// Returned path is *reversed* and given target to source.
		/// </summary>
		List<CPos> FindPath(PathSearch search);

		/// <summary>
		/// Expands both path searches until they intersect, and returns the path.
		/// Returned path is from the source of the first search to the source of the second search.
		/// </summary>
		List<CPos> FindBidiPath(PathSearch fromSrc, PathSearch fromDest);
	}

	public class PathFinder : IPathFinder
	{
		public static readonly List<CPos> NoPath = new List<CPos>(0);

		readonly World world;
		DomainIndex domainIndex;
		bool cached;

		public PathFinder(World world)
		{
			this.world = world;
		}

		/// <summary>
		/// Calculates a path for the actor from source to target.
		/// Returned path is *reversed* and given target to source.
		/// </summary>
		public List<CPos> FindUnitPath(CPos source, CPos target, Actor self, Actor ignoreActor, BlockedByActor check)
		{
			// PERF: Because we can be sure that OccupiesSpace is Mobile here, we can save some performance by avoiding querying for the trait.
			var locomotor = ((Mobile)self.OccupiesSpace).Locomotor;

			if (!cached)
			{
				domainIndex = world.WorldActor.TraitOrDefault<DomainIndex>();
				cached = true;
			}

			// If a water-land transition is required, bail early
			if (domainIndex != null && !domainIndex.IsPassable(source, target, locomotor))
				return NoPath;

			var distance = source - target;
			var canMoveFreely = locomotor.CanMoveFreelyInto(self, target, check, null);
			if (distance.LengthSquared < 3 && !canMoveFreely)
				return NoPath;

			if (source.Layer == target.Layer && distance.LengthSquared < 3 && canMoveFreely)
				return new List<CPos> { target };

			List<CPos> pb;
			using (var fromSrc = PathSearch.ToTargetCell(world, locomotor, self, target, source, check, ignoreActor: ignoreActor))
			using (var fromDest = PathSearch.ToTargetCell(world, locomotor, self, source, target, check, ignoreActor: ignoreActor, inReverse: true))
				pb = FindBidiPath(fromSrc, fromDest);

			return pb;
		}

		/// <summary>
		/// Expands the path search until a path is found, and returns that path.
		/// Returned path is *reversed* and given target to source.
		/// </summary>
		public List<CPos> FindPath(PathSearch search)
		{
			while (search.CanExpand)
			{
				var p = search.Expand();
				if (search.IsTarget(p))
					return MakePath(search.Graph, p);
			}

			return NoPath;
		}

		// Build the path from the destination.
		// When we find a node that has the same previous position than itself, that node is the source node.
		static List<CPos> MakePath(IPathGraph graph, CPos destination)
		{
			var ret = new List<CPos>();
			var currentNode = destination;

			while (graph[currentNode].PreviousNode != currentNode)
			{
				ret.Add(currentNode);
				currentNode = graph[currentNode].PreviousNode;
			}

			ret.Add(currentNode);
			return ret;
		}

		/// <summary>
		/// Expands both path searches until they intersect, and returns the path.
		/// Returned path is from the source of the first search to the source of the second search.
		/// </summary>
		public List<CPos> FindBidiPath(PathSearch first, PathSearch second)
		{
			while (first.CanExpand && second.CanExpand)
			{
				// make some progress on the first search
				var p = first.Expand();
				var pInfo = second.Graph[p];
				if (pInfo.Status == CellStatus.Closed &&
					pInfo.CostSoFar != PathGraph.PathCostForInvalidPath)
					return MakeBidiPath(first, second, p);

				// make some progress on the second search
				var q = second.Expand();
				var qInfo = first.Graph[q];
				if (qInfo.Status == CellStatus.Closed &&
					qInfo.CostSoFar != PathGraph.PathCostForInvalidPath)
					return MakeBidiPath(first, second, q);
			}

			return NoPath;
		}

		// Build the path from the destination of each search.
		// When we find a node that has the same previous position than itself, that is the source of that search.
		static List<CPos> MakeBidiPath(PathSearch first, PathSearch second, CPos confluenceNode)
		{
			var ca = first.Graph;
			var cb = second.Graph;

			var ret = new List<CPos>();

			var q = confluenceNode;
			var previous = ca[q].PreviousNode;
			while (previous != q)
			{
				ret.Add(q);
				q = previous;
				previous = ca[q].PreviousNode;
			}

			ret.Add(q);

			ret.Reverse();

			q = confluenceNode;
			previous = cb[q].PreviousNode;
			while (previous != q)
			{
				q = previous;
				previous = cb[q].PreviousNode;
				ret.Add(q);
			}

			return ret;
		}
	}
}
