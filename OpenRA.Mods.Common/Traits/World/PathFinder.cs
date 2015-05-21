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
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Calculates routes for mobile units based on the A* search algorithm.", " Attach this to the world actor.")]
	public class PathFinderInfo : ITraitInfo
	{
		public object Create(ActorInitializer init)
		{
			return new PathFinderCacheDecorator(new PathFinder(init.World), new PathCacheStorage(init.World));
		}
	}

	public interface IPathFinder
	{
		/// <summary>
		/// Calculates a path for the actor from source to destination
		/// </summary>
		/// <returns>A path from start to target</returns>
		List<CPos> FindUnitPath(CPos source, CPos target, Actor self);

		List<CPos> FindUnitPathToRange(CPos source, SubCell srcSub, WPos target, WRange range, Actor self);

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

		public PathFinder(World world)
		{
			this.world = world;
		}

		public List<CPos> FindUnitPath(CPos source, CPos target, Actor self)
		{
			var mi = self.Info.Traits.Get<MobileInfo>();

			// If a water-land transition is required, bail early
			var domainIndex = world.WorldActor.TraitOrDefault<DomainIndex>();
			if (domainIndex != null)
			{
				var passable = mi.GetMovementClass(world.TileSet);
				if (!domainIndex.IsPassable(source, target, (uint)passable))
					return EmptyPath;
			}

			var pb = FindBidiPath(
				PathSearch.FromPoint(world, mi, self, target, source, true),
				PathSearch.FromPoint(world, mi, self, source, target, true).Reverse());

			CheckSanePath2(pb, source, target);

			return pb;
		}

		public List<CPos> FindUnitPathToRange(CPos source, SubCell srcSub, WPos target, WRange range, Actor self)
		{
			var mi = self.Info.Traits.Get<MobileInfo>();
			var targetCell = world.Map.CellContaining(target);
			var rangeSquared = range.Range * range.Range;

			// Correct for SubCell offset
			target -= world.Map.OffsetOfSubCell(srcSub);

			// Select only the tiles that are within range from the requested SubCell
			// This assumes that the SubCell does not change during the path traversal
			var tilesInRange = world.Map.FindTilesInCircle(targetCell, range.Range / 1024 + 1)
				.Where(t => (world.Map.CenterOfCell(t) - target).LengthSquared <= rangeSquared
							&& mi.CanEnterCell(self.World as World, self as Actor, t));

			// See if there is any cell within range that does not involve a cross-domain request
			// Really, we only need to check the circle perimeter, but it's not clear that would be a performance win
			var domainIndex = world.WorldActor.TraitOrDefault<DomainIndex>();
			if (domainIndex != null)
			{
				var passable = mi.GetMovementClass(world.TileSet);
				tilesInRange = new List<CPos>(tilesInRange.Where(t => domainIndex.IsPassable(source, t, (uint)passable)));
				if (!tilesInRange.Any())
					return EmptyPath;
			}

			var path = FindBidiPath(
				PathSearch.FromPoints(world, mi, self, tilesInRange, source, true),
				PathSearch.FromPoint(world, mi, self, source, targetCell, true).Reverse());

			return path;
		}

		public List<CPos> FindPath(IPathSearch search)
		{
			var dbg = world.WorldActor.TraitOrDefault<PathfinderDebugOverlay>();
			if (dbg != null && dbg.Visible)
				search.Debug = true;

			List<CPos> path = null;

			while (!search.OpenQueue.Empty)
			{
				var p = search.Expand();
				if (search.IsTarget(p))
				{
					path = MakePath(search.Graph, p);
					break;
				}
			}

			if (dbg != null && dbg.Visible)
				dbg.AddLayer(search.Considered, search.MaxCost, search.Owner);

			search.Graph.Dispose();

			if (path != null)
				return path;

			// no path exists
			return EmptyPath;
		}

		// Searches from both ends toward each other. This is used to prevent blockings in case we find
		// units in the middle of the path that prevent us to continue.
		public List<CPos> FindBidiPath(IPathSearch fromSrc, IPathSearch fromDest)
		{
			List<CPos> path = null;

			var dbg = world.WorldActor.TraitOrDefault<PathfinderDebugOverlay>();
			if (dbg != null && dbg.Visible)
			{
				fromSrc.Debug = true;
				fromDest.Debug = true;
			}

			while (!fromSrc.OpenQueue.Empty && !fromDest.OpenQueue.Empty)
			{
				// make some progress on the first search
				var p = fromSrc.Expand();

				if (fromDest.Graph[p].Status == CellStatus.Closed &&
					fromDest.Graph[p].CostSoFar < int.MaxValue)
				{
					path = MakeBidiPath(fromSrc, fromDest, p);
					break;
				}

				// make some progress on the second search
				var q = fromDest.Expand();

				if (fromSrc.Graph[q].Status == CellStatus.Closed &&
					fromSrc.Graph[q].CostSoFar < int.MaxValue)
				{
					path = MakeBidiPath(fromSrc, fromDest, q);
					break;
				}
			}

			if (dbg != null && dbg.Visible)
			{
				dbg.AddLayer(fromSrc.Considered, fromSrc.MaxCost, fromSrc.Owner);
				dbg.AddLayer(fromDest.Considered, fromDest.MaxCost, fromDest.Owner);
			}

			fromSrc.Graph.Dispose();
			fromDest.Graph.Dispose();

			if (path != null)
				return path;

			return EmptyPath;
		}

		// Build the path from the destination. When we find a node that has the same previous
		// position than itself, that node is the source node.
		static List<CPos> MakePath(IGraph<CellInfo> cellInfo, CPos destination)
		{
			var ret = new List<CPos>();
			var currentNode = destination;

			while (cellInfo[currentNode].PreviousPos != currentNode)
			{
				ret.Add(currentNode);
				currentNode = cellInfo[currentNode].PreviousPos;
			}

			ret.Add(currentNode);
			CheckSanePath(ret);
			return ret;
		}

		static List<CPos> MakeBidiPath(IPathSearch a, IPathSearch b, CPos confluenceNode)
		{
			var ca = a.Graph;
			var cb = b.Graph;

			var ret = new List<CPos>();

			var q = confluenceNode;
			while (ca[q].PreviousPos != q)
			{
				ret.Add(q);
				q = ca[q].PreviousPos;
			}

			ret.Add(q);

			ret.Reverse();

			q = confluenceNode;
			while (cb[q].PreviousPos != q)
			{
				q = cb[q].PreviousPos;
				ret.Add(q);
			}

			CheckSanePath(ret);
			return ret;
		}

		[Conditional("SANITY_CHECKS")]
		static void CheckSanePath(IList<CPos> path)
		{
			if (path.Count == 0)
				return;
			var prev = path[0];
			foreach (var cell in path)
			{
				var d = cell - prev;
				if (Math.Abs(d.X) > 1 || Math.Abs(d.Y) > 1)
					throw new InvalidOperationException("(PathFinder) path sanity check failed");
				prev = cell;
			}
		}

		[Conditional("SANITY_CHECKS")]
		static void CheckSanePath2(IList<CPos> path, CPos src, CPos dest)
		{
			if (path.Count == 0)
				return;

			if (path[0] != dest)
				throw new InvalidOperationException("(PathFinder) sanity check failed: doesn't go to dest");
			if (path[path.Count - 1] != src)
				throw new InvalidOperationException("(PathFinder) sanity check failed: doesn't come from src");
		}
	}
}
