#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Reloads an AmmoPool trait externally based on the upgrade criteria.")]
	public class ReloadAmmoWhenEnabledInfo : UpgradableTraitInfo, Requires<AmmoPoolInfo>
	{
		[FieldLoader.Require]
		[Desc("The AmmoPool's name you want to reload.")]
		public readonly string Name = null;

		[Desc("How much ammo is reloaded after a certain period.")]
		public readonly int ReloadCount = 1;

		[Desc("Time to reload per ReloadCount.")]
		public readonly int ReloadDelay = 50;

		[Desc("Reset the delay when the trait is getting enabled.")]
		public readonly bool ResetDelayOnEnable = true;

		public override object Create(ActorInitializer init) { return new ReloadAmmoWhenEnabled(init.Self, this); }
	}

	public class ReloadAmmoWhenEnabled : UpgradableTrait<ReloadAmmoWhenEnabledInfo>, ITick
	{
		ReloadAmmoWhenEnabledInfo info;
		AmmoPool pool;
		int delay;

		public ReloadAmmoWhenEnabled(Actor self, ReloadAmmoWhenEnabledInfo info)
			: base(info)
		{
			this.info = info;
			pool = self.TraitsImplementing<AmmoPool>().First(p => p.Info.Name == info.Name);
			delay = info.ReloadDelay;
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (--delay < 0)
			{
				for (var i = 0; i < info.ReloadCount; i++)
				{
					if (pool.GiveAmmo() && pool.Info.RearmSound != null)
						Game.Sound.Play(pool.Info.RearmSound, self.CenterPosition);
				}

				delay = info.ReloadDelay;
			}
		}

		protected override void UpgradeEnabled(Actor self)
		{
			if (info.ResetDelayOnEnable)
			{
				delay = info.ReloadDelay;
			}
		}
	}
}
