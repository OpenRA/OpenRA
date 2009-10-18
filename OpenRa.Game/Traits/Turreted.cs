using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class Turreted : ITick
	{
		public int turretFacing = 0;
		public int? desiredFacing;

		public Turreted(Actor self)
		{
		}

		public void Tick(Actor self, Game game, int dt)
		{
			// TODO: desiredFacing should follow the base unit's facing only when not in combat.
			// also, we want to be able to use this for GUN; avoid referencing Mobile.
			var df = desiredFacing ?? self.traits.Get<Mobile>().facing;

			if( turretFacing != desiredFacing )
			{
				var leftTurn = ( 32 + turretFacing - desiredFacing ) % 32;
				var rightTurn = ( 32 + desiredFacing - turretFacing ) % 32;
				if( leftTurn > rightTurn )
					turretFacing = ( turretFacing + 1 ) % 32;
				else
					turretFacing = ( turretFacing + 31 ) % 32;
			}
		}
	}
}
