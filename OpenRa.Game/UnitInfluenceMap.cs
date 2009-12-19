using System;
using System.Diagnostics;
using System.Linq;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class UnitInfluenceMap
	{
		Actor[,] influence = new Actor[128, 128];
		readonly int2 searchDistance = new int2(2,2);

		public UnitInfluenceMap()
		{
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
					if( influence[ x, y ] != null && !influence[ x, y ].traits.WithInterface<IOccupySpace>().First().OccupiedCells().Contains( new int2( x, y ) ) )
						throw new InvalidOperationException( "UIM: Sanity check failed A" );

			foreach( var a in Game.world.Actors )
				foreach( var ios in a.traits.WithInterface<IOccupySpace>() )
					foreach( var cell in ios.OccupiedCells() )
						if( influence[ cell.X, cell.Y ] != a )
							throw new InvalidOperationException( "UIM: Sanity check failed B" );
		}

		[Conditional( "SANITY_CHECKS" )]
		void SanityCheckAdd( IOccupySpace a )
		{
			foreach( var c in a.OccupiedCells() )
				if( influence[c.X, c.Y] != null )
					throw new InvalidOperationException( "UIM: Sanity check failed (Add)" );
		}

		public Actor GetUnitAt( int2 a )
		{
			return influence[ a.X, a.Y ];
		}

		public void Add( Actor self, IOccupySpace unit )
		{
			SanityCheckAdd( unit );
			foreach( var c in unit.OccupiedCells() )
				influence[c.X, c.Y] = self;
		}

		public void Remove( Actor self, IOccupySpace unit )
		{
			if (unit != null)
				foreach (var c in unit.OccupiedCells())
					influence[c.X, c.Y] = null;
		}

		public void Update(Actor self, IOccupySpace unit)
		{
			Remove(self, unit);
			if (!self.IsDead) Add(self, unit);
		}
	}
}
