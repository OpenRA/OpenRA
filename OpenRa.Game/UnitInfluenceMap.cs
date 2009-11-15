using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	class UnitInfluenceMap
	{
		Actor[,] influence = new Actor[128, 128];
		readonly int2 searchDistance = new int2(2,2);

		public UnitInfluenceMap()
		{
			Game.world.ActorRemoved += a => Remove(a.traits.GetOrDefault<Mobile>());
		}

		public void Tick()
		{
			//SanityCheck();

			//var units = Game.world.Actors
			//    .Select( a => a.traits.GetOrDefault<Traits.Mobile>() ).Where( m => m != null );

			//foreach (var u in units)
			//    Update(u);

			SanityCheck();
		}

		[System.Diagnostics.Conditional( "SANITY_CHECKS" )]
		void SanityCheck()
		{
			for( int y = 0 ; y < 128 ; y++ )
				for( int x = 0 ; x < 128 ; x++ )
					if( influence[ x, y ] != null && !influence[ x, y ].traits.Get<Mobile>().OccupiedCells().Contains( new int2( x, y ) ) )
						throw new InvalidOperationException( "UIM: Sanity check failed A" );

			foreach( var a in Game.world.Actors )
			{
				if( !a.traits.Contains<Mobile>() )
					continue;
				foreach( var cell in a.traits.Get<Mobile>().OccupiedCells() )
					if( influence[ cell.X, cell.Y ] != a )
						throw new InvalidOperationException( "UIM: Sanity check failed B" );
			}
		}

		[System.Diagnostics.Conditional( "SANITY_CHECKS" )]
		void SanityCheckAdd( Mobile a )
		{
			foreach( var c in a.OccupiedCells() )
				if( influence[c.X, c.Y] != null )
					throw new InvalidOperationException( "UIM: Sanity check failed (Add)" );
		}

		public Actor GetUnitAt( int2 a )
		{
			return influence[ a.X, a.Y ];
		}

		public void Add(Mobile a)
		{
			SanityCheckAdd(a);
			foreach (var c in a.OccupiedCells())
				influence[c.X, c.Y] = a.self;
		}

		public void Remove(Mobile a)
		{
			if (a == null) return;

			var min = int2.Max(new int2(0, 0), a.self.Location - searchDistance);
			var max = int2.Min(new int2(128, 128), a.self.Location + searchDistance);

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (influence[i, j] == a.self)
						influence[i, j] = null;
		}

		public void Update(Mobile a)
		{
			Remove(a);
			if (!a.self.IsDead) Add(a);
		}
	}
}
