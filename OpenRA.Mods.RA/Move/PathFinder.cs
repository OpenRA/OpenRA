#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	public class PathFinderInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new PathFinder(init.world); }
	}

	public class PathFinder
	{
		const int MaxPathAge = 50;	/* x 40ms ticks */
		const int ShortWindow = 15;	// Tick window for grouping move orders
		static readonly List<CPos> EmptyPath = new List<CPos>(0);

		readonly World world;
		public PathFinder(World world) { this.world = world; }

		class CachedPath
		{
			public CPos From;
			public CPos To;
			public List<CPos> Result;
			public int Tick;
			public Actor Actor;
			public WRange Range;
			public int ShareCount;
		}

		ConcurrentDictionary<CPos, List<CachedPath>> fromCellCachedPaths = new ConcurrentDictionary<CPos, List<CachedPath>>();
		int lastCleanTick = -1;
		int lastSweepTick = -1;

		// Number of counterclockwise eighth turns from (1, 0) to (x, y) rounded to the nearest eighth turn
		uint EighthTurns(int x, int y) { return (uint)((WAngle.ArcTan(y, x).Angle + 64) >> 7); }

		// Vector of integral point ajacent to (0, 0) specified number of eighth turns counterclockwise from (1, 0)
		CVec AdjacentVecOfEighthTurn(uint eighthTurns) { return CVec.directions[eighthTurns & 7]; }

		public List<CPos> FindUnitPathToRange(CPos src, SubCell srcSub, WPos target, WRange minRange, WRange maxRange, Actor self)
		{
			using (new PerfSample("Pathfinder"))
			{
				var mi = self.Info.Traits.Get<MobileInfo>();
				var targetCell = self.World.Map.CellContaining(target);
				var rangeSquared = minRange.Range * minRange.Range;
				var srcPos = self.World.Map.CenterOfSubCell(src, srcSub);
				var moveVec = target - srcPos;

				if (moveVec.LengthSquared <= rangeSquared)
					return null;

				var canEnterTarget = mi.CanEnterCell(world, self, targetCell);
				var push = false;

				// If within range, change range to mid-distance
				if (moveVec.HorizontalLengthSquared <= rangeSquared)
				{
					Log.Write("debug", "PathFinder.FindUnitPathToRange(Actor #{0} at ({1})) - using mid-distance", self.ActorID, self.Location);
					rangeSquared = (int)moveVec.HorizontalLengthSquared / 4;
					minRange = new WRange(Exts.ISqrt(rangeSquared, Exts.ISqrtRoundMode.Nearest));
				}

				// Push if within pushing range
				if (moveVec.HorizontalLengthSquared <= maxRange.Range * maxRange.Range)
				{
					if (!canEnterTarget)
						push = self.World.ActorMap.GetUnitsAt(targetCell).All(a => a.TraitsImplementing<INotifyBlockingMove>().Any());
					if (push)
						Log.Write("debug", "PathFinder.FindUnitPathToRange(Actor #{0} at ({1})) - might nudge at destination", self.ActorID, self.Location);
				}

				Func<CPos, CachedPath> selectCachedPath = start =>
				{
					if (!mi.CanEnterCell(self.World, self, start) || !fromCellCachedPaths.ContainsKey(start))
						return null;
					return fromCellCachedPaths[start]
						.Where(p => p.To == targetCell && p.Range >= minRange && p.Range <= maxRange)
						.MinByOrDefault(p => p.Range);
				};

				// TODO: handle multiple terrain crossing actors such as water-land
				var cached = selectCachedPath(src);
				if (cached != null)
				{
					Log.Write("debug", "Actor {0} #{1} {2} path from {3} or {4} tick(s) ago",
						self.Info.Name, self.ActorID, cached.Actor == self ? "asked for a" : "using",
						world.WorldTick - cached.Tick, world.WorldTick - cached.Tick + ShortWindow);
					//if (world.WorldTick - cached.Tick > MaxPathAge)
						//fromCellCachedPaths[src].Remove(cached);
					var result = new List<CPos>(cached.Result);
					var shouldTrimPath = true;

					// Path can not be trimmed
					if (push || result.Count <= 1 || cached.Range.Range + 1024 >= minRange.Range)
						shouldTrimPath = false;

					// Probably not a co-path and target is not reserved/occupied
					else if (cached.Tick > world.WorldTick + ShortWindow && canEnterTarget)
						shouldTrimPath = false;

					// Target is not fully allocated
					else if (mi.SharesCell && cached.ShareCount < world.Map.SharedSubCellCount)
						shouldTrimPath = false;

					if (shouldTrimPath)
					{
						result.RemoveAt(0);
						fromCellCachedPaths.GetOrAdd(src, c => new List<CachedPath>()).Add(new CachedPath
						{
							From = src, To = targetCell, Actor = self, Result = new List<CPos>(result), Tick = world.WorldTick + ShortWindow,
							Range = new WRange((self.World.Map.CenterOfCell(targetCell) - self.World.Map.CenterOfCell(result.First())).Length),
							ShareCount = mi.SharesCell ? 1 : world.Map.SharedSubCellCount
						});
					}
					else
						cached.ShareCount = mi.SharesCell ? cached.ShareCount + 1 : world.Map.SharedSubCellCount;
					return result;
				}
				else
				{
					// Get direction
					var eighthTurns = EighthTurns(moveVec.X, moveVec.Y);
					var adjacent = src + CVec.directions[eighthTurns];

					// Try finding an adjacent neighbor path forward
					cached = selectCachedPath(adjacent);
					if (cached == null)
					{
						adjacent = src + CVec.directions[(eighthTurns + 1) & 7];
						cached = selectCachedPath(adjacent);
						if (cached == null)
						{
							adjacent = src + CVec.directions[(eighthTurns + 7) & 7];
							cached = selectCachedPath(adjacent);
						}
					}

					if (cached != null)
					{
						Log.Write("debug", "Actor {0} #{1} using adjacent neighbor path forward from {2} or {3} tick(s) ago from ({4}) to ({5}), which is {6} from ({7}) linking to ({8}) in direction {9} - sign-x: {10}, sign-y: {11}",
								self.Info.Name, self.ActorID, world.WorldTick - cached.Tick, world.WorldTick - cached.Tick + ShortWindow,
								src.ToString(), cached.Result.First().ToString(), (targetCell - cached.Result.First()).Length,
								targetCell.ToString(), cached.From.ToString(),
								EighthTurns(cached.From.X - src.X, cached.From.Y - src.Y), cached.From.X - src.X, cached.From.Y - src.Y);
						if (world.WorldTick - cached.Tick > MaxPathAge)
							fromCellCachedPaths[cached.From].Remove(cached);
						var result = new List<CPos>(cached.Result);
						result.Add(adjacent);
						return new List<CPos>(result);
					}
				}

				List<CPos> path;

				if (minRange.Range > 0)
				{
					// Correct for SubCell offset
					target -= self.World.Map.OffsetOfSubCell(srcSub);

					// Select only the tiles that are within range from the requested SubCell
					// This assumes that the SubCell does not change during the path traversal
					var tilesInRange = world.Map.FindTilesInCircle(targetCell, minRange.Range / 1024 + 1)
						.Where(t => (world.Map.CenterOfCell(t) - target).LengthSquared <= rangeSquared
							&& mi.CanEnterCell(self.World, self, t));

					// See if there is any cell within range that does not involve a cross-domain request
					// Really, we only need to check the circle perimeter, but it's not clear that would be a performance win
					// TODO: handle transitions such as water-land
					var domainIndex = self.World.WorldActor.TraitOrDefault<DomainIndex>();
					if (domainIndex != null)
					{
						var passable = mi.GetMovementClass(world.TileSet);
						tilesInRange = new List<CPos>(tilesInRange.Where(t => domainIndex.IsPassable(src, t, (uint)passable)));
						if (!tilesInRange.Any())
							return EmptyPath;
					}

					path = FindBidiPath(
						PathSearch.FromPoints(world, mi, self, tilesInRange, src, true),
						PathSearch.FromPoint(world, mi, self, src, targetCell, true).Reverse());
				}
				else
					path = FindBidiPath(
						PathSearch.FromPoint(world, mi, self, targetCell, src, true),
						PathSearch.FromPoint(world, mi, self, src, targetCell, true).Reverse());

				// Remove stale paths and empty lists from the cache at most once per tick
				if (lastCleanTick != world.WorldTick)
				{
					lastCleanTick = world.WorldTick;

					// Periodically remove empty lists while path-finding is busy
					if (lastCleanTick > lastSweepTick + MaxPathAge * 4)
					{
						lastSweepTick = lastCleanTick;
						List<CachedPath> o;
						var emptyLists = fromCellCachedPaths.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key);
						foreach (var empty in emptyLists)
							fromCellCachedPaths.TryRemove(empty, out o);
					}

					// Remove stale paths from the cache
					foreach (var cachedPaths in fromCellCachedPaths)
						cachedPaths.Value.RemoveAll(p => world.WorldTick - p.Tick > MaxPathAge);
				}

				if (path.Count == 0)
					Log.Write("debug", "Actor {0} #{1} failed pathing from ({2}) to ({3})", self.Info.Name, self.ActorID, src.ToString(), targetCell.ToString());
				else
				{
					Log.Write("debug", "Actor {0} #{1} pathing from ({2}) to ({3}), which is {4} from ({5})",
							self.Info.Name, self.ActorID, src.ToString(), path.First().ToString(), (targetCell - path.First()).Length, targetCell.ToString());
					if (path.Count > 1)
						fromCellCachedPaths.GetOrAdd(src, c => new List<CachedPath>()).Add(new CachedPath
						{
							From = src, To = targetCell, Actor = self, Result = path, Tick = world.WorldTick,
							Range = new WRange((world.Map.CenterOfCell(targetCell) - world.Map.CenterOfCell(path.First())).Length),
							ShareCount = mi.SharesCell ? 1 : world.Map.SharedSubCellCount
						});
				}

				return new List<CPos>(path);
			}
		}

		public List<CPos> FindPath(PathSearch search)
		{
			using (new PerfSample("Pathfinder"))
			{
				using (search)
				{
					List<CPos> path = null;

					while (!search.Queue.Empty)
					{
						var p = search.Expand(world);
						if (search.Heuristic(p) == 0)
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

		// Searches from both ends toward each other
		public List<CPos> FindBidiPath(PathSearch fromSrc, PathSearch fromDest)
		{
			using (new PerfSample("Pathfinder"))
			{
				using (fromSrc)
				using (fromDest)
				{
					List<CPos> path = null;

					while (!fromSrc.Queue.Empty && !fromDest.Queue.Empty)
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
		static void CheckSanePath(List<CPos> path)
		{
			if (path.Count == 0)
				return;
			var prev = path[0];
			for (var i = 0; i < path.Count; i++)
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
