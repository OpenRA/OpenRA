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

namespace OpenRa.Traits
{
	public class UnitInfluenceInfo : ITraitInfo
	{
		public object Create( Actor self ) { return new UnitInfluence( self ); }
	}

	public class UnitInfluence : ITick
	{
		List<Actor>[,] influence = new List<Actor>[128, 128];
		readonly int2 searchDistance = new int2(2,2);

		public UnitInfluence( Actor self )
		{
			for (int i = 0; i < 128; i++)
				for (int j = 0; j < 128; j++)
					influence[ i, j ] = new List<Actor>();

			self.World.ActorRemoved += a => Remove( a, a.traits.GetOrDefault<IOccupySpace>() );
		}

		public void Tick( Actor self )
		{
			// Does this belong here? NO, but it's your mess.
			
			// Get the crushable actors
			foreach (var aa in self.World.Queries.WithTrait<ICrushable>())
			{
				var a = aa.Actor;
				// Are there any units in the same cell that can crush this?
				foreach( var ios in a.traits.WithInterface<IOccupySpace>() )
					foreach( var cell in ios.OccupiedCells() )
					{
						// There should only be one (counterexample: An infantry and a tank try to pick up a crate at the same time.)
						// If there is more than one, do action on the first crusher
						var crusher = GetUnitsAt(cell).Where(b => a != b && self.World.IsActorCrushableByActor(a, b)).FirstOrDefault();
						if (crusher != null)
						{
							Log.Write("{0} crushes {1}", crusher.Info.Name, a.Info.Name);
							// Apply the crush action
							foreach (var crush in a.traits.WithInterface<ICrushable>())
								crush.OnCrush(crusher);
						}
					}
			}
			SanityCheck( self );
		}

		[Conditional( "SANITY_CHECKS" )]
		void SanityCheck( Actor self )
		{
			for( int y = 0 ; y < 128 ; y++ )
				for( int x = 0 ; x < 128 ; x++ )
					if( influence[ x, y ] != null )
						foreach (var a in influence[ x, y ])
							if (!a.traits.Get<IOccupySpace>().OccupiedCells().Contains( new int2( x, y ) ) )
								throw new InvalidOperationException( "UIM: Sanity check failed A" );

			foreach( var t in self.World.Queries.WithTraitMultiple<IOccupySpace>() )
				foreach( var cell in t.Trait.OccupiedCells() )
					if (!influence[cell.X, cell.Y].Contains(t.Actor))
						throw new InvalidOperationException( "UIM: Sanity check failed B" );
		}

		public IEnumerable<Actor> GetUnitsAt( int2 a )
		{
			return influence[ a.X, a.Y ];
		}

		public void Add( Actor self, IOccupySpace unit )
		{
			foreach( var c in unit.OccupiedCells() )
				influence[c.X, c.Y].Add(self);
		}

		public void Remove( Actor self, IOccupySpace unit )
		{
			if (unit != null)
				foreach (var c in unit.OccupiedCells())
					influence[c.X, c.Y].Remove(self);
		}

		public void Update(Actor self, IOccupySpace unit)
		{
			Remove(self, unit);
			if (!self.IsDead) Add(self, unit);
		}
	}
}
