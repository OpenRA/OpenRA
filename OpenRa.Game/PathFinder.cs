using System;
using System.Collections.Generic;
using IjwFramework.Collections;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using OpenRa.Game.Support;

namespace OpenRa.Game
{
	class PathFinder
	{
		float[][,] passableCost = new float[4][,];
		Map map;

		public PathFinder(Map map, TileSet tileSet)
		{
			this.map = map;

			for (var umt = UnitMovementType.Foot; umt <= UnitMovementType.Float; umt++)
				passableCost[(int)umt] = new float[128, 128];
			for( int x = 0 ; x < 128 ; x++ )
				for( int y = 0 ; y < 128 ; y++ )
					for (var umt = UnitMovementType.Foot; umt <= UnitMovementType.Float; umt++ )
						passableCost[(int)umt][ x, y ] = ( map.IsInMap( x, y ) )
							? (float)TerrainCosts.Cost( umt, tileSet.GetWalkability( map.MapTiles[ x, y ] ) )
							: float.PositiveInfinity;
		}

		public List<int2> FindUnitPath(int2 src, int2 dest, UnitMovementType umt)
		{
			using (new PerfSample("find_unit_path"))
			{
				var sw = new Stopwatch();
				/*if (passableCost[(int)umt][dest.X, dest.Y] == float.PositiveInfinity)
					return new List<int2>();
				if (!Game.BuildingInfluence.CanMoveHere(dest))
					return new List<int2>();*/

				var result = FindUnitPath(src, DefaultEstimator(dest), umt);
				Game.NormalPathTime += sw.ElapsedTime();
				Game.NormalPathCount++;
				return result;
			}
		}

		public List<int2> FindUnitPathToRange(int2 src, int2 dest, UnitMovementType umt, int range)
		{
			var tilesInRange = Game.FindTilesInCircle(dest, range)
				.Where(t => Game.IsCellBuildable(t, umt));

			var path = FindUnitPath(tilesInRange, DefaultEstimator(src), umt);
			path.Reverse();
			return path;
		}

		bool IsBlocked(int2 from, UnitMovementType umt)
		{
			for (int v = -1; v < 2; v++)
				for (int u = -1; u < 2; u++)
					if (u != 0 || v != 0)
					{
						var p = from + new int2(u, v);
						if (passableCost[(int)umt][from.X + u, from.Y + v] < float.PositiveInfinity)
							if (Game.BuildingInfluence.CanMoveHere(p) && (Game.UnitInfluence.GetUnitAt(p) == null))
								return false;
					}
			return true;
		}

		public List<int2> FindPathToPath( int2 from, List<int2> path, UnitMovementType umt )
		{
			using (new PerfSample("find_path_to_path"))
			{
				if (IsBlocked(from, umt))
					return new List<int2>();

				CellInfo[,] cellInfo = null;
				var queue = new PriorityQueue<PathDistance>();
				var estimator = DefaultEstimator(from);

				var cost = 0.0f;
				var prev = path[0];
				for (int i = 0; i < path.Count; i++)
				{
					var sl = path[i];
					if ( /*i == 0 || */(Game.BuildingInfluence.CanMoveHere(path[i]) && Game.UnitInfluence.GetUnitAt(path[i]) == null))
					{
						queue.Add(new PathDistance(estimator(sl), sl));
						if (cellInfo == null)
							cellInfo = InitCellInfo();

						cellInfo[sl.X, sl.Y] = new CellInfo(cost, prev, false);
					}
					var d = sl - prev;
					cost += ((d.X * d.Y != 0) ? 1.414213563f : 1.0f) * passableCost[(int)umt][sl.X, sl.Y];
					prev = sl;
				}
				if (queue.Empty) return new List<int2>();

				var h2 = DefaultEstimator(path[0]);
				var otherQueue = new PriorityQueue<PathDistance>();
				otherQueue.Add(new PathDistance(h2(from), from));

				var ret = FindBidiPath(cellInfo, InitCellInfo(), queue, otherQueue, estimator, h2, umt, true);

				//var ret = FindPath(cellInfo, queue, estimator, umt, true);
				ret.Reverse();
				return ret;
			}
		}

		public List<int2> FindUnitPath( int2 unitLocation, Func<int2,float> estimator, UnitMovementType umt )
		{
			return FindUnitPath( new[] { unitLocation }, estimator, umt );
		}

		public List<int2> FindUnitPath( IEnumerable<int2> startLocations, Func<int2, float> estimator, UnitMovementType umt )
		{
			var cellInfo = InitCellInfo();
			var queue = new PriorityQueue<PathDistance>();

			foreach (var sl in startLocations)
			{
				queue.Add(new PathDistance(estimator(sl), sl));
				cellInfo[sl.X, sl.Y].MinCost = 0;
			}

			return FindPath( cellInfo, queue, estimator, umt, false );
		}

		List<int2> FindPath( CellInfo[ , ] cellInfo, PriorityQueue<PathDistance> queue, Func<int2, float> estimator, UnitMovementType umt, bool checkForBlock )
		{
			int nodesExpanded = 0;
			using (new PerfSample("find_path_inner"))
			{
				while (!queue.Empty)
				{
					PathDistance p = queue.Pop();
					cellInfo[p.Location.X, p.Location.Y].Seen = true;

					if (estimator(p.Location) == 0)
					{
						PerfHistory.Increment("nodes_expanded", nodesExpanded * .01);
						return MakePath(cellInfo, p.Location);
					}

					nodesExpanded++;

					ExpandNode(cellInfo, queue, p, umt, checkForBlock, estimator);
				}

				PerfHistory.Increment("nodes_expanded", nodesExpanded * .01);
				// no path exists
				return new List<int2>();
			}
		}

		static CellInfo[ , ] InitCellInfo()
		{
			var cellInfo = new CellInfo[ 128, 128 ];
			for( int x = 0 ; x < 128 ; x++ )
				for( int y = 0 ; y < 128 ; y++ )
					cellInfo[ x, y ] = new CellInfo( float.PositiveInfinity, new int2( x, y ), false );
			return cellInfo;
		}

		List<int2> MakePath( CellInfo[ , ] cellInfo, int2 destination )
		{
			List<int2> ret = new List<int2>();
			int2 pathNode = destination;

			while( cellInfo[ pathNode.X, pathNode.Y ].Path != pathNode )
			{
				ret.Add( pathNode );
				pathNode = cellInfo[ pathNode.X, pathNode.Y ].Path;
			}

			ret.Add(pathNode);

			return ret;
		}

		static Func<int2, float> DefaultEstimator(int2 destination)
		{
			return here =>
			{
				int2 d = ( here - destination ).Abs();
				int diag = Math.Min( d.X, d.Y );
				int straight = Math.Abs( d.X - d.Y );
				return 1.5f * diag + straight;
			};
		}

		void ExpandNode(CellInfo[,] ci, PriorityQueue<PathDistance> q, PathDistance p, UnitMovementType umt, bool checkForBlock, Func<int2, float> h)
		{
			foreach (int2 d in Util.directions)
			{
				int2 newHere = p.Location + d;

				if (ci[newHere.X, newHere.Y].Seen)
					continue;
				if (passableCost[(int)umt][newHere.X, newHere.Y] == float.PositiveInfinity)
					continue;
				if (!Game.BuildingInfluence.CanMoveHere(newHere))
					continue;
				if (checkForBlock && Game.UnitInfluence.GetUnitAt(newHere) != null)
					continue;
				var est = h(newHere);
				if (est == float.PositiveInfinity)
					continue;

				float cellCost = ((d.X * d.Y != 0) ? 1.414213563f : 1.0f) * passableCost[(int)umt][newHere.X, newHere.Y];
				float newCost = ci[p.Location.X, p.Location.Y].MinCost + cellCost;

				if (newCost >= ci[newHere.X, newHere.Y].MinCost)
					continue;

				ci[newHere.X, newHere.Y].Path = p.Location;
				ci[newHere.X, newHere.Y].MinCost = newCost;

				q.Add(new PathDistance(newCost + est, newHere));
			}
		}

		List<int2> FindBidiPath(			/* searches from both ends toward each other */
			CellInfo[,] ca, 
			CellInfo[,] cb,
			PriorityQueue<PathDistance> qa,
			PriorityQueue<PathDistance> qb,
			Func<int2, float> ha,
			Func<int2, float> hb,
			UnitMovementType umt,
			bool checkForBlocked)
		{
			while (!qa.Empty && !qb.Empty)
			{
				{		/* make some progress on the first search */
					var p = qa.Pop();
					ca[p.Location.X, p.Location.Y].Seen = true;

					if (cb[p.Location.X, p.Location.Y].MinCost < float.PositiveInfinity)
						return MakeBidiPath(ca, cb, p.Location);
					else
						ExpandNode(ca, qa, p, umt, checkForBlocked, ha);
				}

				{		/* make some progress on the second search */
					var p = qb.Pop();
					cb[p.Location.X, p.Location.Y].Seen = true;

					//if (ca[p.Location.X, p.Location.Y].MinCost < float.PositiveInfinity)
					//	return MakeBidiPath(ca, cb, p.Location);
					//else
						ExpandNode(cb, qb, p, umt, checkForBlocked, hb);
				}
			}

			return new List<int2>();
		}

		static List<int2> MakeBidiPath(CellInfo[,] ca, CellInfo[,] cb, int2 p)
		{
			var a = new List<int2>();
			var b = new List<int2>();

			var q = p;
			while (ca[q.X, q.Y].Path != q)
			{
				q = ca[q.X, q.Y].Path;
				a.Add(q);
			}

			q = p;
			while (cb[q.X, q.Y].Path != q)
			{
				q = cb[q.X, q.Y].Path;
				b.Add(q);
			}

			a.Add(p);
			for (var v = b.Count - 1; v >= 0; v--) a.Add(b[v]);

			return a;
		}
	}

	struct CellInfo
	{
		public float MinCost;
		public int2 Path;
		public bool Seen;

		public CellInfo( float minCost, int2 path, bool seen )
		{
			MinCost = minCost;
			Path = path;
			Seen = seen;
		}
	}

	struct PathDistance : IComparable<PathDistance>
	{
		public float EstTotal;
		public int2 Location;

		public PathDistance(float estTotal, int2 location)
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
