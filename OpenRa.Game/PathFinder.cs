using System;
using System.Collections.Generic;
using IjwFramework.Collections;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	class PathFinder
	{
		double[ , ] passableCost = new double[ 128, 128 ];
		Map map;
		BuildingInfluenceMap bim;

		public PathFinder(Map map, TileSet tileSet, BuildingInfluenceMap bim)
		{
			this.map = map;
			this.bim = bim;

			for( int x = 0 ; x < 128 ; x++ )
				for( int y = 0 ; y < 128 ; y++ )

					passableCost[ x, y ] = ( map.IsInMap( x, y ) )
						? TerrainCosts.Cost( UnitMovementType.Wheel, tileSet.GetWalkability( map.MapTiles[ x, y ] ) )
						: double.PositiveInfinity;
		}

		public List<int2> FindUnitPath( int2 unitLocation, Func<int2,double> estimator )
		{
			var startLocation = unitLocation + map.Offset;

			var cellInfo = new CellInfo[ 128, 128 ];

			for( int x = 0 ; x < 128 ; x++ )
				for( int y = 0 ; y < 128 ; y++ )
					cellInfo[ x, y ] = new CellInfo( double.PositiveInfinity, new int2( x, y ), false );

			return FindUnitPath( new[] {startLocation}, estimator, map.Offset, cellInfo );
		}

		List<int2> FindUnitPath(IEnumerable<int2> startLocations, Func<int2, double> estimator, int2 offset, CellInfo[,] cellInfo)
		{
			var queue = new PriorityQueue<PathDistance>();

			foreach (var sl in startLocations)
			{
				queue.Add(new PathDistance(estimator(sl - offset), sl));
				cellInfo[sl.X, sl.Y].MinCost = 0;
			}

			while( !queue.Empty )
			{
				PathDistance p = queue.Pop();
				int2 here = p.Location;
				cellInfo[ here.X, here.Y ].Seen = true;

				if( estimator( here - offset ) == 0.0 )
					return MakePath( cellInfo, here, offset );

				foreach( int2 d in directions )
				{
					int2 newHere = here + d;

					if( cellInfo[ newHere.X, newHere.Y ].Seen )
						continue;
					if( passableCost[ newHere.X, newHere.Y ] == double.PositiveInfinity )
						continue;
					if (bim.GetBuildingAt(newHere - offset) != null)
						continue;

					double cellCost = ( ( d.X * d.Y != 0 ) ? 1.414213563 : 1.0 ) * passableCost[ newHere.X, newHere.Y ];
					double newCost = cellInfo[ here.X, here.Y ].MinCost + cellCost;

					if( newCost >= cellInfo[ newHere.X, newHere.Y ].MinCost )
						continue;

					cellInfo[ newHere.X, newHere.Y ].Path = here;
					cellInfo[ newHere.X, newHere.Y ].MinCost = newCost;

					queue.Add( new PathDistance( newCost + estimator( newHere - offset ), newHere ) );
				}
			}

			// no path exists
			return new List<int2>();
		}

		List<int2> MakePath( CellInfo[ , ] cellInfo, int2 destination, int2 offset )
		{
			List<int2> ret = new List<int2>();
			int2 pathNode = destination;

			while( cellInfo[ pathNode.X, pathNode.Y ].Path != pathNode )
			{
				ret.Add( pathNode - offset );
				pathNode = cellInfo[ pathNode.X, pathNode.Y ].Path;
			}

			return ret;
		}

		public static readonly int2[] directions =
			new int2[] {
				new int2( -1, -1 ),
				new int2( -1,  0 ),
				new int2( -1,  1 ),
				new int2(  0, -1 ),
				new int2(  0,  1 ),
				new int2(  1, -1 ),
				new int2(  1,  0 ),
				new int2(  1,  1 ),
			};

		public static Func<int2, double> DefaultEstimator(int2 destination)
		{
			return here =>
			{
				int2 d = ( here - destination ).Abs();
				int diag = Math.Min( d.X, d.Y );
				int straight = Math.Abs( d.X - d.Y );
				return 1.5 * diag + straight;
			};
		}
	}

	struct CellInfo
	{
		public double MinCost;
		public int2 Path;
		public bool Seen;

		public CellInfo( double minCost, int2 path, bool seen )
		{
			MinCost = minCost;
			Path = path;
			Seen = seen;
		}
	}

	struct PathDistance : IComparable<PathDistance>
	{
		public double EstTotal;
		public int2 Location;

		public PathDistance(double estTotal, int2 location)
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
