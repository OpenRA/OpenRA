#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to actors which should be able to regenerate their health points.")]
	class SelfHealingInfo : UpgradableTraitInfo, Requires<HealthInfo>
	{
		public readonly int Step = 5;
		public readonly int Delay = 5;
		[Desc("Heal if current health is below this percentage of full health.")]
		public readonly int HealIfBelow = 50;
		public readonly int DamageCooldown = 0;

		public override object Create(ActorInitializer init) { return new SelfHealing(init.Self, this); }
	}

	class SelfHealing : UpgradableTrait<SelfHealingInfo>, ITick, INotifyDamage
	{
		readonly Health health;

		[Sync] int ticks;
		[Sync] int damageTicks;

		public SelfHealing(Actor self, SelfHealingInfo info)
			: base(info)
		{
			health = self.Trait<Health>();
		}

		public void Tick(Actor self)
		{
			if (self.IsDead || IsTraitDisabled)
				return;

			if (health.HP >= Info.HealIfBelow * health.MaxHP / 100)
				return;

			if (damageTicks > 0)
			{
				--damageTicks;
				return;
			}

			if (--ticks <= 0)
			{
				ticks = Info.Delay;
				self.InflictDamage(self, new Damage(-Info.Step));
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value > 0)
				damageTicks = Info.DamageCooldown;
		}
	}
}
