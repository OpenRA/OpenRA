#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Unit will reload its limited ammo itself.")]
	public class ReloadsInfo : ITraitInfo, Requires<LimitedAmmoInfo>
	{
		[Desc("How much ammo is reloaded after a certain period.")]
		public readonly int Count = 0;
		[Desc("How long it takes to do so.")]
		public readonly int Period = 50;
		[Desc("Whether or not reload counter should be reset when ammo has been fired.")]
		public readonly bool ResetOnFire = false;

		public object Create(ActorInitializer init) { return new Reloads(init.Self, this); }
	}

	public class Reloads : ITick
	{
		[Sync] int remainingTicks;
		ReloadsInfo info;
		LimitedAmmo la;
		int previousAmmo;

		public Reloads(Actor self, ReloadsInfo info)
		{
			this.info = info;
			remainingTicks = info.Period;
			la = self.Trait<LimitedAmmo>();
			previousAmmo = la.GetAmmoCount();
		}

		public void Tick(Actor self)
		{
			if (!la.FullAmmo() && --remainingTicks == 0)
			{
				remainingTicks = info.Period;

				for (var i = 0; i < info.Count; i++)
					la.GiveAmmo();

				previousAmmo = la.GetAmmoCount();
			}

			// Resets the tick counter if ammo was fired.
			if (info.ResetOnFire && la.GetAmmoCount() < previousAmmo)
			{
				remainingTicks = info.Period;
				previousAmmo = la.GetAmmoCount();
			}
		}
	}
}
