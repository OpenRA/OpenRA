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
using System.Linq;
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
			return new PathFinderUnitPathCacheDecorator(new PathFinder(init.World), new PathCacheStorage(init.World));
		}
	}

	public interface IPathFinder
	{
		/// <summary>
		/// Calculates a path for the actor from source to destination
		/// </summary>
		/// <returns>A path from start to target</returns>
		List<CPos> FindUnitPath(CPos source, CPos target, Actor self, Actor ignoreActor, BlockedByActor check);

		List<CPos> FindUnitPathToRange(CPos source, SubCell srcSub, WPos target, WDist range, Actor self, BlockedByActor check);

		/// <summary>
		/// Calculates a path given a search specification
		/// </summary>
		List<CPos> FindPath(IPathSearch search);

		/// <summary>
		/// Calculates a path given two search specifications, and
		/// then returns a path when both search intersect each other
		/// TODO: This should eventually disappear
		/// </summary>
		List<CPos> FindBidiPath(IPathSearch fromSrc, IPathSearch fromDest);
	}

	public class PathFinder : IPathFinder
	{
		static readonly List<CPos> EmptyPath = new List<CPos>(0);
		readonly World world;
		DomainIndex domainIndex;
		bool cached;

		public PathFinder(World world)
		{
			this.world = world;
		}

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
				return EmptyPath;

			var distance = source - target;
			var canMoveFreely = locomotor.CanMoveFreelyInto(self, target, check, null);
			if (distance.LengthSquared < 3 && !canMoveFreely)
				return new List<CPos> { };

			if (source.Layer == target.Layer && distance.LengthSquared < 3 && canMoveFreely)
				return new List<CPos> { target };

			List<CPos> pb;

			using (var fromSrc = PathSearch.FromPoint(world, locomotor, self, target, source, check).WithIgnoredActor(ignoreActor))
			using (var fromDest = PathSearch.FromPoint(world, locomotor, self, source, target, check).WithIgnoredActor(ignoreActor).Reverse())
				pb = FindBidiPath(fromSrc, fromDest);

			return pb;
		}

		public List<CPos> FindUnitPathToRange(CPos source, SubCell srcSub, WPos target, WDist range, Actor self, BlockedByActor check)
		{
			if (!cached)
			{
				domainIndex = world.WorldActor.TraitOrDefault<DomainIndex>();
				cached = true;
			}

			// PERF: Because we can be sure that OccupiesSpace is Mobile here, we can save some performance by avoiding querying for the trait.
			var mobile = (Mobile)self.OccupiesSpace;
			var locomotor = mobile.Locomotor;

			var targetCell = world.Map.CellContaining(target);

			// Correct for SubCell offset
			target -= world.Map.Grid.OffsetOfSubCell(srcSub);

			// Select only the tiles that are within range from the requested SubCell
			// This assumes that the SubCell does not change during the path traversal
			var tilesInRange = world.Map.FindTilesInCircle(targetCell, range.Length / 1024 + 1)
				.Where(t => (world.Map.CenterOfCell(t) - target).LengthSquared <= range.LengthSquared
							&& mobile.Info.CanEnterCell(self.World, self, t));

			// See if there is any cell within range that does not involve a cross-domain request
			// Really, we only need to check the circle perimeter, but it's not clear that would be a performance win
			if (domainIndex != null)
			{
				tilesInRange = new List<CPos>(tilesInRange.Where(t => domainIndex.IsPassable(source, t, locomotor)));
				if (!tilesInRange.Any())
					return EmptyPath;
			}

			using (var fromSrc = PathSearch.FromPoints(world, locomotor, self, tilesInRange, source, check))
			using (var fromDest = PathSearch.FromPoint(world, locomotor, self, source, targetCell, check).Reverse())
				return FindBidiPath(fromSrc, fromDest);
		}

		public List<CPos> FindPath(IPathSearch search)
		{
			List<CPos> path = EmptyPath;

			while (search.TryExpand(out var p))
			{
				if (search.IsTarget(p))
				{
					path = MakePath(search.Graph, p);
					break;
				}
			}

			search.Graph.Dispose();

			return path;
		}

		// Searches from both ends toward each other. This is used to prevent blockings in case we find
		// units in the middle of the path that prevent us to continue.
		public List<CPos> FindBidiPath(IPathSearch fromSrc, IPathSearch fromDest)
		{
			List<CPos> path = EmptyPath;

			while (fromSrc.TryExpand(out var p) && fromDest.TryExpand(out var q))
			{
				// make some progress on the first search
				var pci = fromDest.Graph[p];
				if (pci.Status == CellStatus.Closed && pci.CostSoFar < int.MaxValue)
				{
					path = MakeBidiPath(fromSrc, fromDest, p);
					break;
				}

				// make some progress on the second search
				var qci = fromSrc.Graph[q];
				if (qci.Status == CellStatus.Closed && qci.CostSoFar < int.MaxValue)
				{
					path = MakeBidiPath(fromSrc, fromDest, q);
					break;
				}
			}

			fromSrc.Graph.Dispose();
			fromDest.Graph.Dispose();

			return path;
		}

		// Build the path from the destination. When we find a node that has the same previous
		// position than itself, that node is the source node.
		static List<CPos> MakePath(IGraph<CellInfo> cellInfo, CPos destination)
		{
			var ret = new List<CPos>();
			var currentNode = destination;
			var prevNode = cellInfo[currentNode].PreviousPos;
			while (prevNode != currentNode)
			{
				ret.Add(currentNode);
				currentNode = prevNode;
				prevNode = cellInfo[currentNode].PreviousPos;
			}

			ret.Add(currentNode);
			return ret;
		}

		static List<CPos> MakeBidiPath(IPathSearch a, IPathSearch b, CPos confluenceNode)
		{
			var ca = a.Graph;
			var cb = b.Graph;

			var ret = new List<CPos>();

			var q = confluenceNode;
			var prevQ = ca[q].PreviousPos;
			while (prevQ != q)
			{
				ret.Add(q);
				q = prevQ;
				prevQ = ca[q].PreviousPos;
			}

			ret.Add(q);

			ret.Reverse();

			q = confluenceNode;
			prevQ = cb[q].PreviousPos;
			while (prevQ != q)
			{
				q = prevQ;
				ret.Add(q);
				prevQ = cb[q].PreviousPos;
			}

			return ret;
		}
	}
}
