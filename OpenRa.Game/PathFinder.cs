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

		public List<int2> FindPathToPath( int2 from, List<int2> path, UnitMovementType umt )
		{
			using (new PerfSample("find_path_to_path"))
			{
				var anyMovePossible = false;
				for( int v = -1; v < 2; v++ )
					for( int u = -1; u < 2; u++ )
						if (u != 0 || v != 0)
						{
							var p = from + new int2(u, v);
							if (passableCost[(int)umt][from.X + u, from.Y + v] < float.PositiveInfinity)
								if (Game.BuildingInfluence.CanMoveHere(p) && (Game.UnitInfluence.GetUnitAt(p) == null))
									anyMovePossible = true;
						}

				if (!anyMovePossible)
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
				var ret = FindPath(cellInfo, queue, estimator, umt, true);
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
			int samples = 0;
			using (new PerfSample("find_path_inner"))
			{
				while (!queue.Empty)
				{
					PathDistance p = queue.Pop();
					int2 here = p.Location;
					cellInfo[here.X, here.Y].Seen = true;

					if (estimator(here) == 0.0)
					{
						PerfHistory.Increment("nodes_expanded", samples * .01);
						return MakePath(cellInfo, here);
					}

					samples++;

					foreach (int2 d in Util.directions)
					{
						int2 newHere = here + d;

						if (cellInfo[newHere.X, newHere.Y].Seen)
							continue;
						if (passableCost[(int)umt][newHere.X, newHere.Y] == float.PositiveInfinity)
							continue;
						if (!Game.BuildingInfluence.CanMoveHere(newHere))
							continue;
						if (checkForBlock && Game.UnitInfluence.GetUnitAt(newHere) != null)
							continue;
						var est = estimator(newHere);
						if (est == float.PositiveInfinity)
							continue;

						float cellCost = ((d.X * d.Y != 0) ? 1.414213563f : 1.0f) * passableCost[(int)umt][newHere.X, newHere.Y];
						float newCost = cellInfo[here.X, here.Y].MinCost + cellCost;

						if (newCost >= cellInfo[newHere.X, newHere.Y].MinCost)
							continue;

						cellInfo[newHere.X, newHere.Y].Path = here;
						cellInfo[newHere.X, newHere.Y].MinCost = newCost;

						queue.Add(new PathDistance(newCost + est, newHere));
					}
				}

				PerfHistory.Increment("nodes_expanded", samples * .01);
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
