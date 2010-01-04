using System.Collections.Generic;

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
		public bool GiveAmmo()
		{
			if (ammo >= self.Info.Ammo) return false;
			++ammo;
			return true;
		}

		public void Attacking(Actor self) { --ammo; }

		public IEnumerable<PipType> GetPips(Actor self)
		{
			return Graphics.Util.MakeArray(self.Info.Ammo, 
				i => ammo > i ? PipType.Green : PipType.Transparent);
		}
	}
}
