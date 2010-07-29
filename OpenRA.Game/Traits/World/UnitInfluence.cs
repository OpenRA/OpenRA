#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class UnitInfluenceInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new UnitInfluence( init.world ); }
	}

	public class UnitInfluence : ITick
	{
		List<Actor>[,] influence;
		Map map;

		public UnitInfluence( World world )
		{
			map = world.Map;
			influence = new List<Actor>[world.Map.MapSize.X, world.Map.MapSize.Y];
			for (int i = 0; i < world.Map.MapSize.X; i++)
				for (int j = 0; j < world.Map.MapSize.Y; j++)
					influence[ i, j ] = new List<Actor>();

			world.ActorRemoved += a => Remove( a, a.traits.GetOrDefault<IOccupySpace>() );
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
							if (!a.traits.Get<IOccupySpace>().OccupiedCells().Contains( new int2( x, y ) ) )
								throw new InvalidOperationException( "UIM: Sanity check failed A" );

			foreach( var t in self.World.Queries.WithTraitMultiple<IOccupySpace>() )
				foreach( var cell in t.Trait.OccupiedCells() )
					if (!influence[cell.X, cell.Y].Contains(t.Actor))
						throw new InvalidOperationException( "UIM: Sanity check failed B" );
		}

		Actor[] noActors = { };
		public IEnumerable<Actor> GetUnitsAt( int2 a )
		{
			if (!map.IsInMap(a)) return noActors;
			return influence[ a.X, a.Y ];
		}

		public bool AnyUnitsAt(int2 a)
		{
			return /*map.IsInMap(a) && */influence[a.X, a.Y].Count > 0;
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
			if (!self.IsDead()) Add(self, unit);
		}
	}
}
