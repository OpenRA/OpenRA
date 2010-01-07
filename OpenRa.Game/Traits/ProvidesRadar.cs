using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class ProvidesRadar
	{
		Actor self;
		public ProvidesRadar(Actor self)
		{
			this.self = self;
		}
		
		public bool IsActive()
		{
			// TODO: Check for nearby MRJ
			
			// Check if powered
			var b = self.traits.Get<Building>();
			if (b != null && b.Disabled)
				return false;
			
			return true;
		}
	}
}
