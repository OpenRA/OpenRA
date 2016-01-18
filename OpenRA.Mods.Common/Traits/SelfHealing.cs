#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to actors which should be able to regenerate their health points.")]
	class SelfHealingInfo : UpgradableTraitInfo, Requires<HealthInfo>
	{
		public readonly int Step = 5;
		public readonly int Ticks = 5;
		public readonly float HealIfBelow = .5f;
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

			if (health.HP >= Info.HealIfBelow * health.MaxHP)
				return;

			if (damageTicks > 0)
			{
				--damageTicks;
				return;
			}

			if (--ticks <= 0)
			{
				ticks = Info.Ticks;
				self.InflictDamage(self, -Info.Step, null);
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0)
				damageTicks = Info.DamageCooldown;
		}
	}
}
