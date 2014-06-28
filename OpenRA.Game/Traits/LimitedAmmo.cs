#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;

namespace OpenRA.Traits
{
	public class LimitedAmmoInfo : ITraitInfo
	{
		public readonly int Ammo = 0;
		[Desc("Defaults to value in Ammo.")]
		public readonly int PipCount = 0;
		public readonly PipType PipType = PipType.Green;
		public readonly PipType PipTypeEmpty = PipType.Transparent;
		[Desc("Time to reload measured in ticks.")]
		public readonly int ReloadTicks = 25 * 2;

		public object Create(ActorInitializer init) { return new LimitedAmmo(this); }
	}

	public class LimitedAmmo : INotifyAttack, IPips, ISync
	{
		[Sync] int ammo;
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

		public bool TakeAmmo()
		{
			if (ammo <= 0) return false;
			--ammo;
			return true;
		}

		public int ReloadTimePerAmmo() { return Info.ReloadTicks; }

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { TakeAmmo(); }

		public int GetAmmoCount() { return ammo; }

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var pips = Info.PipCount != 0 ? Info.PipCount : Info.Ammo;
			return Exts.MakeArray(pips,
				i => (ammo * pips) / Info.Ammo > i ? Info.PipType : Info.PipTypeEmpty);
		}
	}
}
