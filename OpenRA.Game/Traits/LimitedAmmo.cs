#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Traits
{
	class LimitedAmmoInfo : ITraitInfo
	{
		public readonly int Ammo = 0;
		public readonly int PipCount = 0;

		public object Create(ActorInitializer init) { return new LimitedAmmo(init.self); }
	}

	public class LimitedAmmo : INotifyAttack, IPips
	{
		[Sync]
		int ammo;
		Actor self;

		public LimitedAmmo(Actor self)
		{
			ammo = self.Info.Traits.Get<LimitedAmmoInfo>().Ammo;
			this.self = self;
		}

		public bool HasAmmo() { return ammo > 0; }
		public bool GiveAmmo()
		{
			if (ammo >= self.Info.Traits.Get<LimitedAmmoInfo>().Ammo) return false;
			++ammo;
			return true;
		}

		public void Attacking(Actor self) { --ammo; }

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var info = self.Info.Traits.Get<LimitedAmmoInfo>();
			var pips = info.PipCount != 0 ? info.PipCount : info.Ammo;
			return Graphics.Util.MakeArray(pips, 
				i => (ammo * pips) / info.Ammo > i ? PipType.Green : PipType.Transparent);
		}
	}
}
