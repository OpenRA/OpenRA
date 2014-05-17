#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
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

		public object Create(ActorInitializer init) { return new Reloads(init.self, this); }
	}

	public class Reloads : ITick
	{
		[Sync] int remainingTicks;
		ReloadsInfo Info;
		LimitedAmmo la;
		int previousAmmo;

		public Reloads(Actor self, ReloadsInfo info)
		{
			Info = info;
			remainingTicks = info.Period;
			la = self.Trait<LimitedAmmo>();
			previousAmmo = la.GetAmmoCount();
		}

		public void Tick(Actor self)
		{
			if (!la.FullAmmo() && --remainingTicks == 0)
			{
				remainingTicks = Info.Period;

				for (var i = 0; i < Info.Count; i++)
					la.GiveAmmo();

				previousAmmo = la.GetAmmoCount();
			}

			// Resets the tick counter if ammo was fired.
			if (Info.ResetOnFire && la.GetAmmoCount() < previousAmmo)
			{
				remainingTicks = Info.Period;
				previousAmmo = la.GetAmmoCount();
			}
		}
	}
}
