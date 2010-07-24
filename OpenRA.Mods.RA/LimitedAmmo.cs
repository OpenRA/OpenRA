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
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class LimitedAmmoInfo : ITraitInfo
	{
		public readonly int Ammo = 0;
		public readonly int PipCount = 0;

		public object Create(ActorInitializer init) { return new LimitedAmmo(this); }
	}

	public class LimitedAmmo : INotifyAttack, IPips
	{
		[Sync]
		int ammo;
		LimitedAmmoInfo Info;
		
		public LimitedAmmo(LimitedAmmoInfo info)
		{
			ammo = info.Ammo;
			Info = info;
		}

		public bool FullAmmo() { return ammo == Info.Ammo; }
		public bool HasAmmo() { return ammo > 0; }
		public bool GiveAmmo()
		{
			if (ammo >= Info.Ammo) return false;
			++ammo;
			return true;
		}

		public void Attacking(Actor self) { --ammo; }

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var pips = Info.PipCount != 0 ? Info.PipCount : Info.Ammo;
			return Graphics.Util.MakeArray(pips, 
				i => (ammo * pips) / Info.Ammo > i ? PipType.Green : PipType.Transparent);
		}
	}
}
