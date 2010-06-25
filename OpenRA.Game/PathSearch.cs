#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.GameRules;

namespace OpenRA
{
	public class PathSearch
	{
		World world;
		public CellInfo[ , ] cellInfo;
		public PriorityQueue<PathDistance> queue;
		public Func<int2, float> heuristic;
		Func<int2, bool> customBlock;
		public bool checkForBlocked;
		public Actor ignoreBuilding;
		Actor self;
		public bool inReverse;

		public PathSearch(Actor self)
		{
			this.self = self;
			world = self.World;
			cellInfo = InitCellInfo();
			queue = new PriorityQueue<PathDistance>();
		}

		public PathSearch InReverse()
		{
			inReverse = true;
			return this;
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

		public PathSearch WithHeuristic(Func<int2, float> h)
		{
			heuristic = h;
			return this;
		}
		
		public PathSearch FromPoint(int2 from)
		{
			AddInitialCell( self.World, from );
			return this;
		}
		
		const float LaneBias = .5f;

		public int2 Expand( World world )
		{
			var	umt = self.traits.Get<Mobile>().GetMovementType();

			var p = queue.Pop();
			cellInfo[ p.Location.X, p.Location.Y ].Seen = true;
			
			var mobile = self.traits.Get<Mobile>();
			var thisCost = mobile.MovementCostForCell(self, p.Location);

			if (thisCost == float.PositiveInfinity) 
				return p.Location;

			foreach( int2 d in directions )
			{
				int2 newHere = p.Location + d;

				if (!world.Map.IsInMap(newHere.X, newHere.Y)) continue;
				if( cellInfo[ newHere.X, newHere.Y ].Seen )
					continue;

				var costHere = mobile.MovementCostForCell(self, newHere);
				
				if (costHere == float.PositiveInfinity)
					continue;

				if (checkForBlocked && !mobile.CanEnterCell(newHere, ignoreBuilding, checkForBlocked))
					continue;
				
				if (customBlock != null && customBlock(newHere))
					continue;
				
				var est = heuristic( newHere );
				if( est == float.PositiveInfinity )
					continue;

				float cellCost = ((d.X * d.Y != 0) ? 1.414213563f : 1.0f) * costHere;

				// directional bonuses for smoother flow!
				var ux = (newHere.X + (inReverse ? 1 : 0) & 1);
				var uy = (newHere.Y + (inReverse ? 1 : 0) & 1);

				if (ux == 0 && d.Y < 0) cellCost += LaneBias;
				else if (ux == 1 && d.Y > 0) cellCost += LaneBias;
				if (uy == 0 && d.X < 0) cellCost += LaneBias;
				else if (uy == 1 && d.X > 0) cellCost += LaneBias;

				float newCost = cellInfo[ p.Location.X, p.Location.Y ].MinCost + cellCost;

				if( newCost >= cellInfo[ newHere.X, newHere.Y ].MinCost )
					continue;

				cellInfo[ newHere.X, newHere.Y ].Path = p.Location;
				cellInfo[ newHere.X, newHere.Y ].MinCost = newCost;

				queue.Add( new PathDistance( newCost + est, newHere ) );
				
			}
			return p.Location;
		}

		static readonly int2[] directions =
		{
			new int2( -1, -1 ),
			new int2( -1,  0 ),
			new int2( -1,  1 ),
			new int2(  0, -1 ),
			new int2(  0,  1 ),
			new int2(  1, -1 ),
			new int2(  1,  0 ),
			new int2(  1,  1 ),
		};

		public void AddInitialCell( World world, int2 location )
		{
			if (!world.Map.IsInMap(location.X, location.Y))
				return;

			cellInfo[ location.X, location.Y ] = new CellInfo( 0, location, false );
			queue.Add( new PathDistance( heuristic( location ), location ) );
		}
		
		public static PathSearch Search( Actor self, bool checkForBlocked )
		{
			var search = new PathSearch(self) {
				checkForBlocked = checkForBlocked };
			return search;
		}
		
		public static PathSearch FromPoint( Actor self, int2 from, int2 target, bool checkForBlocked )
		{
			var search = new PathSearch(self) {
				heuristic = DefaultEstimator( target ),
				checkForBlocked = checkForBlocked };

			search.AddInitialCell( self.World, from );
			return search;
		}

		public static PathSearch FromPoints(Actor self, IEnumerable<int2> froms, int2 target, bool checkForBlocked)
		{
			var search = new PathSearch(self)
			{
				heuristic = DefaultEstimator(target),
				checkForBlocked = checkForBlocked
			};

			foreach (var sl in froms)
				search.AddInitialCell(self.World, sl);

			return search;
		}

		CellInfo[ , ] InitCellInfo()
		{
			var cellInfo = new CellInfo[ world.Map.MapSize.X, world.Map.MapSize.Y ];
			for( int x = 0 ; x < world.Map.MapSize.X ; x++ )
				for( int y = 0 ; y < world.Map.MapSize.Y ; y++ )
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
