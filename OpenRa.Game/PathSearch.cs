using System;
using System.Collections.Generic;
using System.Linq;
using IjwFramework.Collections;
using OpenRa.Graphics;

namespace OpenRa
{
	public class PathSearch
	{
		public CellInfo[ , ] cellInfo;
		public PriorityQueue<PathDistance> queue;
		public Func<int2, float> heuristic;
		public UnitMovementType umt;
		Func<int2, bool> customBlock;
		public bool checkForBlocked;
		public Actor ignoreBuilding;

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

		public PathSearch WithIgnoredBuilding(Actor b)
		{
			ignoreBuilding = b;
			return this;
		}

		public int2 Expand( float[][ , ] passableCost )
		{
			var p = queue.Pop();
			cellInfo[ p.Location.X, p.Location.Y ].Seen = true;

			var custom2 = Game.world.customTerrain[p.Location.X, p.Location.Y];
			var thisCost = (custom2 != null) 
				? custom2.GetCost(p.Location, umt) 
				: passableCost[(int)umt][p.Location.X, p.Location.Y];

			if (thisCost == float.PositiveInfinity) 
				return p.Location;
					
			foreach( int2 d in Util.directions )
			{
				int2 newHere = p.Location + d;

				if (!Game.world.Map.IsInMap(newHere.X, newHere.Y)) continue;
				if( cellInfo[ newHere.X, newHere.Y ].Seen )
					continue;

				var custom = Game.world.customTerrain[newHere.X, newHere.Y];
				if (custom != null) throw new NotImplementedException();
				var costHere = (custom != null) ? custom.GetCost(newHere, umt) : passableCost[(int)umt][newHere.X, newHere.Y];

				if (costHere == float.PositiveInfinity)
					continue;

				if (!Game.world.BuildingInfluence.CanMoveHere(newHere) && 
					Game.world.BuildingInfluence.GetBuildingAt(newHere) != ignoreBuilding)
					continue;
				if (Game.world.Map.IsOverlaySolid(newHere))
					continue;
				
				// Replicate real-ra behavior of not being able to enter a cell if there is a mixture of crushable and uncrushable units
				if (checkForBlocked && (Game.world.UnitInfluence.GetUnitsAt(newHere).Any(a => !Game.world.IsActorPathableToCrush(a, umt))))
					continue;
				
				if (customBlock != null && customBlock(newHere))
					continue;
				
				var est = heuristic( newHere );
				if( est == float.PositiveInfinity )
					continue;

				float cellCost = ((d.X * d.Y != 0) ? 1.414213563f : 1.0f) * costHere;
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
			if (!Game.world.Map.IsInMap(location.X, location.Y))
				return;

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

		public static PathSearch FromPoints(IEnumerable<int2> froms, int2 target, UnitMovementType umt, bool checkForBlocked)
		{
			var search = new PathSearch
			{
				heuristic = DefaultEstimator(target),
				umt = umt,
				checkForBlocked = checkForBlocked
			};

			foreach (var sl in froms)
				search.AddInitialCell(sl);

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

		public static Func<int2, float> DefaultEstimator( int2 destination )
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
