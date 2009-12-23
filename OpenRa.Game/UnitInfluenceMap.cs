using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class UnitInfluenceMap
	{
		List<Actor>[,] influence = new List<Actor>[128, 128];
		readonly int2 searchDistance = new int2(2,2);

		public UnitInfluenceMap()
		{
			for (int i = 0; i < 128; i++)
				for (int j = 0; j < 128; j++)
					influence[ i, j ] = new List<Actor>();
			
			Game.world.ActorRemoved += a => Remove(a, a.traits.WithInterface<IOccupySpace>().FirstOrDefault());
		}

		public void Tick()
		{
			SanityCheck();
		}

		[Conditional( "SANITY_CHECKS" )]
		void SanityCheck()
		{
			for( int y = 0 ; y < 128 ; y++ )
				for( int x = 0 ; x < 128 ; x++ )
					if( influence[ x, y ] != null )
						foreach (var a in influence[ x, y ])
							if (!a.traits.WithInterface<IOccupySpace>().First().OccupiedCells().Contains( new int2( x, y ) ) )
								throw new InvalidOperationException( "UIM: Sanity check failed A" );

			foreach( Actor a in Game.world.Actors )
				foreach( var ios in a.traits.WithInterface<IOccupySpace>() )
					foreach( var cell in ios.OccupiedCells() )
						if (!influence[cell.X, cell.Y].Contains(a))
							//if( influence[ cell.X, cell.Y ] != a )
								throw new InvalidOperationException( "UIM: Sanity check failed B" );
		}

		[Conditional( "SANITY_CHECKS" )]
		void SanityCheckAdd( IOccupySpace a )
		{
			foreach( var c in a.OccupiedCells() )
				if( influence[c.X, c.Y].Any())
					throw new InvalidOperationException( "UIM: Sanity check failed (Add)" );
		}

		public IEnumerable<Actor> GetUnitsAt( int2 a )
		{
			return influence[ a.X, a.Y ];
		}

		public void Add( Actor self, IOccupySpace unit )
		{
			SanityCheckAdd( unit );
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
