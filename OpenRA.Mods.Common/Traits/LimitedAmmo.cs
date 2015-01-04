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
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Actor has a limited amount of ammo, after using it all the actor must reload in some way.")]
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
		LimitedAmmoInfo info;

		public LimitedAmmo(LimitedAmmoInfo info)
		{
			ammo = info.Ammo;
			this.info = info;
		}

		public bool FullAmmo() { return ammo == info.Ammo; }
		public bool HasAmmo() { return ammo > 0; }
		public bool GiveAmmo()
		{
			if (ammo >= info.Ammo) return false;
			++ammo;
			return true;
		}

		public bool TakeAmmo()
		{
			if (ammo <= 0) return false;
			--ammo;
			return true;
		}

		public int ReloadTimePerAmmo() { return info.ReloadTicks; }

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { TakeAmmo(); }

		public int GetAmmoCount() { return ammo; }

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var pips = info.PipCount != 0 ? info.PipCount : info.Ammo;
			return Enumerable.Range(0, pips).Select(i =>
				(ammo * pips) / info.Ammo > i ?
				info.PipType : info.PipTypeEmpty);
		}
	}
}
