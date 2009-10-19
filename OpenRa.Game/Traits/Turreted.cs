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

		public void Tick( Actor self, Game game )
		{
			var df = desiredFacing ?? ( self.traits.Contains<Mobile>() ? self.traits.Get<Mobile>().facing : turretFacing );
			Util.TickFacing( ref turretFacing, df, self.unitInfo.ROT );
		}
	}
}
