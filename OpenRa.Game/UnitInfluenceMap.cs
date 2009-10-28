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
			var units = Game.world.Actors
				.Select( a => a.traits.GetOrDefault<Traits.Mobile>() ).Where( m => m != null );

			foreach (var u in units)
				Update(u);
		}

		public Actor GetUnitAt( int2 a )
		{
			var actor = influence[ a.X, a.Y ];
			if( actor != null && !actor.traits.Get<Mobile>().OccupiedCells().Contains( a ) )
				throw new InvalidOperationException( "UIM: Unit is not in influenced square" );
			return actor;
		}

		public void Add(Mobile a)
		{
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
