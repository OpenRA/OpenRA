#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Reloads an ammo pool.")]
	public class ReloadAmmoPoolInfo : PausableConditionalTraitInfo
	{
		[Desc("Reload ammo pool with this name.")]
		public readonly string AmmoPool = "primary";

		[Desc("Reload time in ticks per Count.")]
		public readonly int Delay = 50;

		[Desc("How much ammo is reloaded after Delay.")]
		public readonly int Count = 1;

		[Desc("Whether or not reload timer should be reset when ammo has been fired.")]
		public readonly bool ResetOnFire = false;

		[Desc("Play this sound each time ammo is reloaded.")]
		public readonly string Sound = null;

		public override object Create(ActorInitializer init) { return new ReloadAmmoPool(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (ai.TraitInfos<AmmoPoolInfo>().Count(ap => ap.Name == AmmoPool) != 1)
				throw new YamlException("ReloadsAmmoPool.AmmoPool requires exactly one AmmoPool with matching Name!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class ReloadAmmoPool : PausableConditionalTrait<ReloadAmmoPoolInfo>, ITick, INotifyCreated, INotifyAttack, ISync
	{
		AmmoPool ammoPool;

		[Sync] int remainingTicks;

		public ReloadAmmoPool(ReloadAmmoPoolInfo info)
			: base(info) { }

		void INotifyCreated.Created(Actor self)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().Single(ap => ap.Info.Name == Info.AmmoPool);
			remainingTicks = Info.Delay;
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (Info.ResetOnFire)
				remainingTicks = Info.Delay;
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel) { }

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			Reload(self, Info.Delay, Info.Count, Info.Sound);
		}

		protected virtual void Reload(Actor self, int reloadDelay, int reloadCount, string sound)
		{
			if (!ammoPool.FullAmmo() && --remainingTicks == 0)
			{
				remainingTicks = reloadDelay;
				if (!string.IsNullOrEmpty(sound))
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, sound, self.CenterPosition);

				ammoPool.GiveAmmo(self, reloadCount);
			}
		}
	}
}
