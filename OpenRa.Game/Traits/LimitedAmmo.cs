using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class LimitedAmmo : INotifyAttack, IPips
	{
		int ammo;
		Actor self;

		public LimitedAmmo(Actor self)
		{
			ammo = self.Info.Ammo;
			this.self = self;
		}

		public bool HasAmmo() { return ammo > 0; }

		public void Attacking(Actor self) { --ammo; }

		public IEnumerable<PipType> GetPips()
		{
			return Graphics.Util.MakeArray(self.Info.Ammo, 
				i => ammo > i ? PipType.Green : PipType.Transparent);
		}
	}
}
