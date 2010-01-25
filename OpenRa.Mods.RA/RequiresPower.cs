using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Traits;

namespace OpenRa.Mods.RA
{
	class RequiresPowerInfo : ITraitInfo
	{
		public object Create(Actor self) { return new RequiresPower(self); }
	}

	class RequiresPower : IDisable
	{
		readonly Actor self;
		public RequiresPower( Actor self )
		{
			this.self = self;
		}

		public bool Disabled
		{
			get	{ return (self.Owner.GetPowerState() != PowerState.Normal);	}
			set {} // Cannot explicity set
		}
	}
}
