using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using OpenRa.DataStructures;
using System.Windows.Forms;

namespace OpenRa.Game
{
	class PathFinder
	{
		public static PathFinder Instance;

		bool[ , ] passable = new bool[ 128, 128 ];
		Map map;

		static bool IsPassable(int terrainType)
		{
			switch (terrainType)
			{
				case 0:
				case 2:
				case 6:
				case 8:
				case 9:
					return true;
				default:
					return false;
			}
		}

		public PathFinder( Map map, TileSet tileSet )
		{
			this.map = map;

			for( int x = 0 ; x < 128 ; x++ )
			{
				for( int y = 0 ; y < 128 ; y++ )
				{
					if (x < map.XOffset || y < map.YOffset || x >= map.XOffset + map.Width || y >= map.YOffset + map.Height)
						passable[x, y] = false;
					else
					{
						TileReference r = map.MapTiles[x, y];
						if (r.tile == 0xffff || r.tile == 0xff)
							r.image = 0;

						passable[x, y] = IsPassable(tileSet.walk[r.tile][r.image]);
					}
				}
			}
		}

		public List<int2> FindUnitPath( World world, Unit unit, int2 destination )
		{
			int2 offset = new int2( map.XOffset, map.YOffset );

			destination += offset;
			int2 startLocation = unit.Location + offset;

			bool[ , ] seen = new bool[ 128, 128 ];
			int2[ , ] path = new int2[ 128, 128 ];
			double[ , ] minCost = new double[ 128, 128 ];

			for( int x = 0 ; x < 128 ; x++ )
			{
				for( int y = 0 ; y < 128 ; y++ )
				{
					path[ x, y ] = new int2( x, y );
					minCost[ x, y ] = double.PositiveInfinity;
				}
			}

			PriorityQueue<PathDistance> queue = new PriorityQueue<PathDistance>();

			queue.Add( new PathDistance( Estimate( startLocation, destination ), startLocation ) );
			minCost[ startLocation.X, startLocation.Y ] = Estimate( startLocation, destination );

			int seenCount = 0;
			int impassableCount = 0;

			while( !queue.Empty )
			{
				PathDistance p = queue.Pop();
				int2 here = p.Location;
				seen[ here.X, here.Y ] = true;

				if( p.Location == destination )
				{
					Log.Write( "{0}, {1}", seenCount, impassableCount );
					return MakePath( path, destination, offset );
				}

				foreach( int2 d in directions )
				{
					int2 newHere = here + d;

					if( seen[ newHere.X, newHere.Y ] )
					{
						++seenCount;
						continue;
					}
					if( !passable[ newHere.X, newHere.Y ] )
					{
						++impassableCount;
						continue;
					}

					double newCost = minCost[ here.X, here.Y ] + ( ( d.X * d.Y != 0 ) ? 1.414213563 : 1.0 );

					if( newCost >= minCost[ newHere.X, newHere.Y ] )
						continue;

					path[ newHere.X, newHere.Y ] = here;
					minCost[ newHere.X, newHere.Y ] = newCost;

					queue.Add( new PathDistance( newCost + Estimate( newHere, destination ), newHere ) );
				}
			}

			// no path exists
			return new List<int2>();
		}

		List<int2> MakePath( int2[ , ] path, int2 destination, int2 offset )
		{
			List<int2> ret = new List<int2>();
			int2 pathNode = destination;

			while( path[ pathNode.X, pathNode.Y ] != pathNode )
			{
				ret.Add( pathNode - offset );
				pathNode = path[ pathNode.X, pathNode.Y ];
			}

			Log.Write( "Path Length: {0}", ret.Count );
			return ret;
		}

		static readonly int2[] directions =
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

		double Estimate( int2 here, int2 destination )
		{
			int2 d = ( here - destination ).Abs();
			int diag = Math.Min( d.X, d.Y );
			int straight = Math.Abs( d.X - d.Y );
			return 1.5 * diag + straight;
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
