using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class GpsSatellite
	{
		public GpsSatellite(Actor self)
		{
			// TODO: connect this to a special power that calls Activate();
			Activate(self);
		}
		
		public void Activate(Actor self)
		{
			// TODO: Launch satellite
			self.Owner.Shroud.RevealAll();
		}
	}
}
