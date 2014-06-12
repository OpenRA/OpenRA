#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.Common.Move
{
	public class PathFinderInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PathFinder(init.world); }
	}

	public class PathFinder
	{
		readonly static List<CPos> emptyPath = new List<CPos>(0);

		readonly World world;
		public PathFinder(World world) { this.world = world; }

		class CachedPath
		{
			public CPos from;
			public CPos to;
			public List<CPos> result;
			public int tick;
			public Actor actor;
		}

		List<CachedPath> CachedPaths = new List<CachedPath>();
		const int MaxPathAge = 50;	/* x 40ms ticks */

		public List<CPos> FindUnitPath(CPos from, CPos target, Actor self)
		{
			using (new PerfSample("Pathfinder"))
			{
				var cached = CachedPaths.FirstOrDefault(p => p.from == from && p.to == target && p.actor == self);
				if (cached != null)
				{
					Log.Write("debug", "Actor {0} asked for a path from {1} tick(s) ago", self.ActorID, world.WorldTick - cached.tick);
					if (world.WorldTick - cached.tick > MaxPathAge)
						CachedPaths.Remove(cached);
					return new List<CPos>(cached.result);
				}

				var mi = self.Info.Traits.Get<MobileInfo>();

				// If a water-land transition is required, bail early
				var domainIndex = self.World.WorldActor.TraitOrDefault<DomainIndex>();
				if (domainIndex != null)
				{
					var passable = mi.GetMovementClass(world.TileSet);
					if (!domainIndex.IsPassable(from, target, (uint)passable))
						return emptyPath;
				}

				var pb = FindBidiPath(
					PathSearch.FromPoint(world, mi, self, target, from, true),
					PathSearch.FromPoint(world, mi, self, from, target, true).InReverse()
				);

				CheckSanePath2(pb, from, target);

				CachedPaths.RemoveAll(p => world.WorldTick - p.tick > MaxPathAge);
				CachedPaths.Add(new CachedPath { from = from, to = target, actor = self, result = pb, tick = world.WorldTick });
				return new List<CPos>(pb);
			}
		}

		public List<CPos> FindUnitPathToRange(CPos src, SubCell srcSub, WPos target, WRange range, Actor self)
		{
			using (new PerfSample("Pathfinder"))
			{
				var mi = self.Info.Traits.Get<MobileInfo>();
				var targetCell = target.ToCPos();
				var rangeSquared = range.Range*range.Range;

				// Correct for SubCell offset
				target -= MobileInfo.SubCellOffsets[srcSub];

				// Select only the tiles that are within range from the requested SubCell
				// This assumes that the SubCell does not change during the path traversal
				var tilesInRange = world.FindTilesInCircle(targetCell, range.Range / 1024 + 1)
					.Where(t => (t.CenterPosition - target).LengthSquared <= rangeSquared
					       && mi.CanEnterCell(self.World, self, t, null, true, true));

				// See if there is any cell within range that does not involve a cross-domain request
				// Really, we only need to check the circle perimeter, but it's not clear that would be a performance win
				var domainIndex = self.World.WorldActor.TraitOrDefault<DomainIndex>();
				if (domainIndex != null)
				{
					var passable = mi.GetMovementClass(world.TileSet);
					tilesInRange = new List<CPos>(tilesInRange.Where(t => domainIndex.IsPassable(src, t, (uint)passable)));
					if (!tilesInRange.Any())
						return emptyPath;
				}

				var path = FindBidiPath(
					PathSearch.FromPoints(world, mi, self, tilesInRange, src, true),
					PathSearch.FromPoint(world, mi, self, src, targetCell, true).InReverse()
				);

				return path;
			}
		}

		public List<CPos> FindPath(PathSearch search)
		{
			using (new PerfSample("Pathfinder"))
			{
				using (search)
				{
					List<CPos> path = null;

					while (!search.queue.Empty)
					{
						var p = search.Expand(world);
						if (search.heuristic(p) == 0)
						{
							path = MakePath(search.cellInfo, p);
							break;
						}
					}

					var dbg = world.WorldActor.TraitOrDefault<PathfinderDebugOverlay>();
					if (dbg != null)
					{
						dbg.AddLayer(search.considered.Select(p => new Pair<CPos, int>(p, search.cellInfo[p.X, p.Y].MinCost)), search.maxCost, search.owner);
					}

					if (path != null)
						return path;
				}

				// no path exists
				return emptyPath;
			}
		}

		static List<CPos> MakePath(CellInfo[,] cellInfo, CPos destination)
		{
			var ret = new List<CPos>();
			CPos pathNode = destination;

			while (cellInfo[pathNode.X, pathNode.Y].Path != pathNode)
			{
				ret.Add(pathNode);
				pathNode = cellInfo[pathNode.X, pathNode.Y].Path;
			}

			ret.Add(pathNode);
			CheckSanePath(ret);
			return ret;
		}

		public List<CPos> FindBidiPath(			/* searches from both ends toward each other */
			PathSearch fromSrc,
			PathSearch fromDest)
		{
			using (new PerfSample("Pathfinder"))
			{
				using (fromSrc)
				using (fromDest)
				{
					List<CPos> path = null;

					while (!fromSrc.queue.Empty && !fromDest.queue.Empty)
					{
						/* make some progress on the first search */
						var p = fromSrc.Expand(world);

						if (fromDest.cellInfo[p.X, p.Y].Seen &&
							fromDest.cellInfo[p.X, p.Y].MinCost < float.PositiveInfinity)
						{
							path = MakeBidiPath(fromSrc, fromDest, p);
							break;
						}

						/* make some progress on the second search */
						var q = fromDest.Expand(world);

						if (fromSrc.cellInfo[q.X, q.Y].Seen &&
							fromSrc.cellInfo[q.X, q.Y].MinCost < float.PositiveInfinity)
						{
							path = MakeBidiPath(fromSrc, fromDest, q);
							break;
						}
					}

					var dbg = world.WorldActor.TraitOrDefault<PathfinderDebugOverlay>();
					if (dbg != null)
					{
						dbg.AddLayer(fromSrc.considered.Select(p => new Pair<CPos, int>(p, fromSrc.cellInfo[p.X, p.Y].MinCost)), fromSrc.maxCost, fromSrc.owner);
						dbg.AddLayer(fromDest.considered.Select(p => new Pair<CPos, int>(p, fromDest.cellInfo[p.X, p.Y].MinCost)), fromDest.maxCost, fromDest.owner);
					}

					if (path != null)
						return path;
				}

				return emptyPath;
			}
		}

		static List<CPos> MakeBidiPath(PathSearch a, PathSearch b, CPos p)
		{
			var ca = a.cellInfo;
			var cb = b.cellInfo;

			var ret = new List<CPos>();

			var q = p;
			while (ca[q.X, q.Y].Path != q)
			{
				ret.Add(q);
				q = ca[q.X, q.Y].Path;
			}
			ret.Add(q);

			ret.Reverse();

			q = p;
			while (cb[q.X, q.Y].Path != q)
			{
				q = cb[q.X, q.Y].Path;
				ret.Add(q);
			}

			CheckSanePath(ret);
			return ret;
		}

		[Conditional("SANITY_CHECKS")]
		static void CheckSanePath(List<CPos> path)
		{
			if (path.Count == 0)
				return;
			var prev = path[0];
			for (int i = 0; i < path.Count; i++)
			{
				var d = path[i] - prev;
				if (Math.Abs(d.X) > 1 || Math.Abs(d.Y) > 1)
					throw new InvalidOperationException("(PathFinder) path sanity check failed");
				prev = path[i];
			}
		}

		[Conditional("SANITY_CHECKS")]
		static void CheckSanePath2(List<CPos> path, CPos src, CPos dest)
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
		public int MinCost;
		public CPos Path;
		public bool Seen;

		public CellInfo(int minCost, CPos path, bool seen)
		{
			MinCost = minCost;
			Path = path;
			Seen = seen;
		}
	}

	public struct PathDistance : IComparable<PathDistance>
	{
		public int EstTotal;
		public CPos Location;

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
