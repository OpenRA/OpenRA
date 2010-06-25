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
using System.Diagnostics;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class AircraftInfluenceInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new AircraftInfluence( init.world ); }
	}

	public class AircraftInfluence : ITick
	{
		List<Actor>[,] influence;
		Map map;

		public AircraftInfluence( World world )
		{
			map = world.Map;
			influence = new List<Actor>[world.Map.MapSize.X, world.Map.MapSize.Y];
			for (int i = 0; i < world.Map.MapSize.X; i++)
				for (int j = 0; j < world.Map.MapSize.Y; j++)
					influence[ i, j ] = new List<Actor>();

			world.ActorRemoved += a => Remove( a, a.traits.GetOrDefault<IOccupyAir>() );
		}

		public void Tick( Actor self )
		{
			SanityCheck( self );
		}

		[Conditional( "SANITY_CHECKS" )]
		void SanityCheck( Actor self )
		{
			for( int x = 0 ; x < self.World.Map.MapSize.X ; x++ )
				for( int y = 0 ; y < self.World.Map.MapSize.Y ; y++ )
					if( influence[ x, y ] != null )
						foreach (var a in influence[ x, y ])
							if (!a.traits.Get<IOccupyAir>().OccupiedAirCells().Contains( new int2( x, y ) ) )
								throw new InvalidOperationException( "AIM: Sanity check failed A" );

			foreach( var t in self.World.Queries.WithTraitMultiple<IOccupyAir>() )
				foreach( var cell in t.Trait.OccupiedAirCells() )
					if (!influence[cell.X, cell.Y].Contains(t.Actor))
						throw new InvalidOperationException( "AIM: Sanity check failed B" );
		}

		Actor[] noActors = { };
		public IEnumerable<Actor> GetUnitsAt( int2 a )
		{
			if (!map.IsInMap(a)) return noActors;
			return influence[ a.X, a.Y ];
		}

		public void Add( Actor self, IOccupyAir unit )
		{
			foreach( var c in unit.OccupiedAirCells() )
				influence[c.X, c.Y].Add(self);
		}

		public void Remove( Actor self, IOccupyAir unit )
		{
			if (unit != null)
				foreach (var c in unit.OccupiedAirCells())
					influence[c.X, c.Y].Remove(self);
		}

		public void Update(Actor self, IOccupyAir unit)
		{
			Remove(self, unit);
			if (!self.IsDead) Add(self, unit);
		}
	}
}
