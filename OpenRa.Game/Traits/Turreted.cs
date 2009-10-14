using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Turreted : ITick // temporary.
	{
		public int turretFacing = Game.CellSize;

		public Turreted(Actor self)
		{
		}

		// temporary.
		public void Tick(Actor self, Game game, int dt)
		{
			turretFacing = (turretFacing + 1) % 32;
		}
	}
}
