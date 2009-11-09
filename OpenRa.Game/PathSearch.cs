using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using IjwFramework.Collections;

namespace OpenRa.Game
{
	class PathSearch
	{
		public CellInfo[ , ] cellInfo;
		public PriorityQueue<PathDistance> queue;
		public Func<int2, float> heuristic;
		public UnitMovementType umt;
		Func<int2, bool> customBlock;
		public bool checkForBlocked;

		public PathSearch()
		{
			cellInfo = InitCellInfo();
			queue = new PriorityQueue<PathDistance>();
		}

		public PathSearch WithCustomBlocker(Func<int2, bool> customBlock)
		{
			this.customBlock = customBlock;
			return this;
		}

		public int2 Expand( float[][ , ] passableCost )
		{
			var p = queue.Pop();
			cellInfo[ p.Location.X, p.Location.Y ].Seen = true;

			foreach( int2 d in Util.directions )
			{
				int2 newHere = p.Location + d;

				if( cellInfo[ newHere.X, newHere.Y ].Seen )
					continue;
				if( passableCost[ (int)umt ][ newHere.X, newHere.Y ] == float.PositiveInfinity )
					continue;
				if( !Game.BuildingInfluence.CanMoveHere( newHere ) )
					continue;
				if( checkForBlocked && Game.UnitInfluence.GetUnitAt( newHere ) != null )
					continue;
				if (customBlock != null && customBlock(newHere))
					continue;

				var est = heuristic( newHere );
				if( est == float.PositiveInfinity )
					continue;

				float cellCost = ( ( d.X * d.Y != 0 ) ? 1.414213563f : 1.0f ) * passableCost[ (int)umt ][ newHere.X, newHere.Y ];
				float newCost = cellInfo[ p.Location.X, p.Location.Y ].MinCost + cellCost;

				if( newCost >= cellInfo[ newHere.X, newHere.Y ].MinCost )
					continue;

				cellInfo[ newHere.X, newHere.Y ].Path = p.Location;
				cellInfo[ newHere.X, newHere.Y ].MinCost = newCost;

				queue.Add( new PathDistance( newCost + est, newHere ) );
			}
			return p.Location;
		}

		public void AddInitialCell( int2 location )
		{
			cellInfo[ location.X, location.Y ] = new CellInfo( 0, location, false );
			queue.Add( new PathDistance( heuristic( location ), location ) );
		}

	
		
		
		
		
		
		public static PathSearch FromPoint( int2 from, int2 target, UnitMovementType umt, bool checkForBlocked )
		{
			var search = new PathSearch {
				heuristic = DefaultEstimator( target ),
				umt = umt,
				checkForBlocked = checkForBlocked };

			search.AddInitialCell( from );

			return search;
		}

		public static PathSearch FromPoints( IEnumerable<int2> froms, int2 target, UnitMovementType umt, bool checkForBlocked )
		{
			var search = new PathSearch {
				heuristic = DefaultEstimator( target ),
				umt = umt,
				checkForBlocked = checkForBlocked };

			foreach( var sl in froms )
				search.AddInitialCell( sl );

			return search;
		}





		static CellInfo[ , ] InitCellInfo()
		{
			var cellInfo = new CellInfo[ 128, 128 ];
			for( int x = 0 ; x < 128 ; x++ )
				for( int y = 0 ; y < 128 ; y++ )
					cellInfo[ x, y ] = new CellInfo( float.PositiveInfinity, new int2( x, y ), false );
			return cellInfo;
		}

		static Func<int2, float> DefaultEstimator( int2 destination )
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
}
