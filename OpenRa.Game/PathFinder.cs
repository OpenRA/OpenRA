using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.FileFormats;
using OpenRa.Support;
using OpenRa.Traits;
using System.Diagnostics;

namespace OpenRa
{
	public class PathFinder
	{
		readonly World world;
		float[][,] passableCost = new float[4][,];
		
		public PathFinder( World world )
		{
			this.world = world;
			for (var umt = UnitMovementType.Foot; umt <= UnitMovementType.Float; umt++)
				passableCost[(int)umt] = new float[128, 128];
			for( int x = 0 ; x < 128 ; x++ )
				for( int y = 0 ; y < 128 ; y++ )
					for (var umt = UnitMovementType.Foot; umt <= UnitMovementType.Float; umt++ )
						passableCost[(int)umt][ x, y ] = ( world.Map.IsInMap( x, y ) )
							? (float)TerrainCosts.Cost( umt, world.TileSet.GetWalkability( world.Map.MapTiles[ x, y ] ) )
							: float.PositiveInfinity;
		}

		public List<int2> FindUnitPath( int2 from, int2 target, UnitMovementType umt )
		{
			using (new PerfSample("find_unit_path"))
			{
				var pb = FindBidiPath(
					PathSearch.FromPoint(world, target, from, umt, false).WithCustomBlocker(AvoidUnitsNear(from, 4)),
					PathSearch.FromPoint(world, from, target, umt, false).WithCustomBlocker(AvoidUnitsNear(from, 4)));

				CheckSanePath2(pb, from, target);
				return pb;
			}
		}

		public List<int2> FindUnitPathToRange( int2 src, int2 target, UnitMovementType umt, int range )
		{
			using( new PerfSample( "find_unit_path_multiple_src" ) )
			{
				var tilesInRange = world.FindTilesInCircle(target, range)
					.Where( t => world.IsCellBuildable( t, umt ) );

				var path = FindPath( PathSearch.FromPoints( world, tilesInRange, src, umt, false ).WithCustomBlocker(AvoidUnitsNear(src, 4)));
				path.Reverse();
				return path;
			}
		}
		
		public Func<int2, bool> AvoidUnitsNear(int2 p, int dist)
		{
			return q =>
				p != q &&
				((p - q).LengthSquared < dist * dist) &&
				(world.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(q).Any());
		}

		public List<int2> FindPath( PathSearch search )
		{
			using (new PerfSample("find_path_inner"))
			{
				while (!search.queue.Empty)
				{
					var p = search.Expand( world, passableCost );
					PerfHistory.Increment("nodes_expanded", .01);

					if (search.heuristic(p) == 0)
						return MakePath(search.cellInfo, p);
				}

				// no path exists
				return new List<int2>();
			}
		}

		static List<int2> MakePath( CellInfo[ , ] cellInfo, int2 destination )
		{
			List<int2> ret = new List<int2>();
			int2 pathNode = destination;

			while( cellInfo[ pathNode.X, pathNode.Y ].Path != pathNode )
			{
				ret.Add( pathNode );
				pathNode = cellInfo[ pathNode.X, pathNode.Y ].Path;
			}

			ret.Add(pathNode);
			CheckSanePath(ret);
			return ret;
		}



		List<int2> FindBidiPath(			/* searches from both ends toward each other */
			PathSearch fromSrc,
			PathSearch fromDest)
		{
			while (!fromSrc.queue.Empty && !fromDest.queue.Empty)
			{
				/* make some progress on the first search */
				var p = fromSrc.Expand( world, passableCost );

				if (fromDest.cellInfo[p.X, p.Y].Seen && fromDest.cellInfo[p.X, p.Y].MinCost < float.PositiveInfinity)
					return MakeBidiPath(fromSrc, fromDest, p);

				/* make some progress on the second search */
				var q = fromDest.Expand( world, passableCost );

				if (fromSrc.cellInfo[q.X, q.Y].Seen && fromSrc.cellInfo[q.X, q.Y].MinCost < float.PositiveInfinity)
					return MakeBidiPath(fromSrc, fromDest, q);
			}

			return new List<int2>();
		}

		static List<int2> MakeBidiPath(PathSearch a, PathSearch b, int2 p)
		{
			var ca = a.cellInfo;
			var cb = b.cellInfo;

			var ret = new List<int2>();

			var q = p;
			while (ca[q.X, q.Y].Path != q)
			{
				ret.Add( q );
				q = ca[ q.X, q.Y ].Path;
			}
			ret.Add(q);

			ret.Reverse();

			q = p;
			while (cb[q.X, q.Y].Path != q)
			{
				q = cb[q.X, q.Y].Path;
				ret.Add(q);
			}

			CheckSanePath( ret );
			return ret;
		}

		[Conditional( "SANITY_CHECKS" )]
		static void CheckSanePath( List<int2> path )
		{
			if( path.Count == 0 )
				return;
			var prev = path[ 0 ];
			for( int i = 0 ; i < path.Count ; i++ )
			{
				var d = path[ i ] - prev;
				if( Math.Abs( d.X ) > 1 || Math.Abs( d.Y ) > 1 )
					throw new InvalidOperationException( "(PathFinder) path sanity check failed" );
				prev = path[ i ];
			}
		}

		[Conditional("SANITY_CHECKS")]
		static void CheckSanePath2(List<int2> path, int2 src, int2 dest)
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

	public struct PathDistance : IComparable<PathDistance>
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
