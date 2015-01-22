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
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Calculates routes for mobile units based on the A* search algorithm.", " Attach this to the world actor.")]
	public class PathFinderInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PathFinder(new NonCachedPathFinder(init.World), new PathCacheStorage(init.World)); }
	}

	public interface IPathFinder
	{
		List<CPos> FindUnitPath(CPos from, CPos target, IActor self);
		List<CPos> FindUnitPathToRange(CPos src, SubCell srcSub, WPos target, WRange range, IActor self);
		List<CPos> FindPath(PathSearch search);
		List<CPos> FindBidiPath(PathSearch fromSrc, PathSearch fromDest);
	}

	public class PathFinder : IPathFinder
	{
		readonly IPathFinder pathFinder;
		readonly ICacheStorage<List<CPos>> cacheStorage;

		public PathFinder(IPathFinder pathFinder, ICacheStorage<List<CPos>> cacheStorage)
		{
			this.pathFinder = pathFinder;
			this.cacheStorage = cacheStorage;
		}

		public List<CPos> FindUnitPath(CPos from, CPos target, IActor self)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindUnitPath" + self.ActorID + from.X + from.Y + target.X + target.Y;
				var cachedPath = cacheStorage.Retrieve(key);

				if (cachedPath != null)
					return cachedPath;

				var pb = pathFinder.FindUnitPath(from, target, self);

				cacheStorage.Store(key, pb);

				return pb;
			}
		}

		public List<CPos> FindUnitPathToRange(CPos src, SubCell srcSub, WPos target, WRange range, IActor self)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindUnitPathToRange" + self.ActorID + src.X + src.Y + target.X + target.Y;
				var cachedPath = cacheStorage.Retrieve(key);

				if (cachedPath != null)
					return cachedPath;

				var pb = pathFinder.FindUnitPathToRange(src, srcSub, target, range, self);

				cacheStorage.Store(key, pb);

				return pb;
			}
		}

		public List<CPos> FindPath(PathSearch search)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindPath" + search.Id;
				var cachedPath = cacheStorage.Retrieve(key);

				if (cachedPath != null)
					return cachedPath;

				var pb = pathFinder.FindPath(search);

				cacheStorage.Store(key, pb);

				return pb;
			}
		}

		public List<CPos> FindBidiPath(PathSearch fromSrc, PathSearch fromDest)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindBidiPath" + fromSrc.Id + fromDest.Id;
				var cachedPath = cacheStorage.Retrieve(key);

				if (cachedPath != null)
					return cachedPath;

				var pb = pathFinder.FindBidiPath(fromSrc, fromDest);

				cacheStorage.Store(key, pb);

				return pb;
			}
		}
	}

	public class NonCachedPathFinder : IPathFinder
	{
		static readonly List<CPos> EmptyPath = new List<CPos>(0);

		readonly IWorld world;
		public NonCachedPathFinder(IWorld world) { this.world = world; }

		public List<CPos> FindUnitPath(CPos from, CPos target, IActor self)
		{
			var mi = self.TraitInfo<IMobileInfo>();

			// If a water-land transition is required, bail early
			var domainIndex = world.WorldActor.TraitOrDefault<DomainIndex>();
			if (domainIndex != null)
			{
				var passable = mi.GetMovementClass(world.TileSet);
				if (!domainIndex.IsPassable(from, target, (uint)passable))
					return EmptyPath;
			}

			var pb = FindBidiPath(
				PathSearch.FromPoint(world, mi, self, target, from, true),
				PathSearch.FromPoint(world, mi, self, from, target, true).Reverse());

			CheckSanePath2(pb, from, target);

			return pb;
		}

		public List<CPos> FindUnitPathToRange(CPos src, SubCell srcSub, WPos target, WRange range, IActor self)
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
				tilesInRange = new List<CPos>(tilesInRange.Where(t => domainIndex.IsPassable(src, t, (uint)passable)));
				if (!tilesInRange.Any())
					return EmptyPath;
			}

			var path = FindBidiPath(
				PathSearch.FromPoints(world, mi, self, tilesInRange, src, true),
				PathSearch.FromPoint(world, mi, self, src, targetCell, true).Reverse());

			return path;
		}

		public List<CPos> FindPath(PathSearch search)
		{
			using (search)
			{
				List<CPos> path = null;

				while (!search.OpenQueue.Empty)
				{
					var p = search.Expand(world);
					if (search.IsTarget(p))
					{
						path = MakePath(search.CellInfo, p);
						break;
					}
				}

				var dbg = world.WorldActor.TraitOrDefault<PathfinderDebugOverlay>();
				if (dbg != null)
					dbg.AddLayer(search.Considered.Select(p => new Pair<CPos, int>(p, search.CellInfo[p].MinCost)), search.MaxCost, search.Owner);

				if (path != null)
					return path;
			}

			// no path exists
			return EmptyPath;
		}

		// Searches from both ends toward each other
		public List<CPos> FindBidiPath(PathSearch fromSrc, PathSearch fromDest)
		{
			using (fromSrc)
			using (fromDest)
			{
				List<CPos> path = null;

				while (!fromSrc.OpenQueue.Empty && !fromDest.OpenQueue.Empty)
				{
					/* make some progress on the first search */
					var p = fromSrc.Expand(world);

					if (fromDest.CellInfo[p].Seen &&
						fromDest.CellInfo[p].MinCost < float.PositiveInfinity)
					{
						path = MakeBidiPath(fromSrc, fromDest, p);
						break;
					}

					/* make some progress on the second search */
					var q = fromDest.Expand(world);

					if (fromSrc.CellInfo[q].Seen &&
						fromSrc.CellInfo[q].MinCost < float.PositiveInfinity)
					{
						path = MakeBidiPath(fromSrc, fromDest, q);
						break;
					}
				}

				var dbg = world.WorldActor.TraitOrDefault<PathfinderDebugOverlay>();
				if (dbg != null)
				{
					dbg.AddLayer(fromSrc.Considered.Select(p => new Pair<CPos, int>(p, fromSrc.CellInfo[p].MinCost)), fromSrc.MaxCost, fromSrc.Owner);
					dbg.AddLayer(fromDest.Considered.Select(p => new Pair<CPos, int>(p, fromDest.CellInfo[p].MinCost)), fromDest.MaxCost, fromDest.Owner);
				}

				if (path != null)
					return path;
			}

			return EmptyPath;
		}

		static List<CPos> MakePath(CellLayer<CellInfo> cellInfo, CPos destination)
		{
			var ret = new List<CPos>();
			var pathNode = destination;

			while (cellInfo[pathNode].Path != pathNode)
			{
				ret.Add(pathNode);
				pathNode = cellInfo[pathNode].Path;
			}

			ret.Add(pathNode);
			CheckSanePath(ret);
			return ret;
		}

		static List<CPos> MakeBidiPath(PathSearch a, PathSearch b, CPos p)
		{
			var ca = a.CellInfo;
			var cb = b.CellInfo;

			var ret = new List<CPos>();

			var q = p;
			while (ca[q].Path != q)
			{
				ret.Add(q);
				q = ca[q].Path;
			}

			ret.Add(q);

			ret.Reverse();

			q = p;
			while (cb[q].Path != q)
			{
				q = cb[q].Path;
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

	public struct CellInfo
	{
		public readonly int MinCost;
		public readonly CPos Path;
		public readonly bool Seen;

		public CellInfo(int minCost, CPos path, bool seen)
		{
			MinCost = minCost;
			Path = path;
			Seen = seen;
		}
	}

	public struct PathDistance : IComparable<PathDistance>
	{
		public readonly int EstTotal;
		public readonly CPos Location;

		public PathDistance(int estTotal, CPos location)
		{
			EstTotal = estTotal;
			Location = location;
		}

		public int CompareTo(PathDistance other)
		{
			return Math.Sign(EstTotal - other.EstTotal);
		}
	}
}
