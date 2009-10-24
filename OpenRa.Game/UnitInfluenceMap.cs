using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game
{
	class UnitInfluenceMap
	{
		Actor[,] influence = new Actor[128, 128];

		/* todo: incremental updates for great justice [and perf] */

		public void Tick()
		{
			influence = new Actor[128, 128];

			var units = Game.world.Actors
				.Select( a => a.traits.GetOrDefault<Traits.Mobile>() ).Where( m => m != null );

			foreach (var u in units)
				foreach (var c in u.OccupiedCells())
					influence[c.X, c.Y] = u.self;
		}

		public Actor GetUnitAt(int2 a) { return influence[a.X, a.Y]; }
	}
}
