#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Traits
{
	class LimitedAmmoInfo : ITraitInfo
	{
		public readonly int Ammo = 0;
		public readonly int PipCount = 0;

		public object Create(Actor self) { return new LimitedAmmo(self); }
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
