#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Attach this to actors which should be able to regenerate their health points.")]
	class SelfHealingInfo : ConditionalTraitInfo, Requires<IHealthInfo>
	{
		[Desc("Absolute amount of health points added in each step.")]
		public readonly int Step = 5;

		[Desc("Relative percentages of health added in each step.",
			"When both values are defined, their summary will be applied.")]
		public readonly int PercentageStep = 0;

		public readonly int Delay = 5;

		[Desc("Heal if current health is below this percentage of full health.")]
		public readonly int HealIfBelow = 50;

		public readonly int DamageCooldown = 0;

		[Desc("Apply the selfhealing using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		public override object Create(ActorInitializer init) { return new SelfHealing(init.Self, this); }
	}

	class SelfHealing : ConditionalTrait<SelfHealingInfo>, ITick, INotifyDamage
	{
		readonly IHealth health;

		[Sync]
		int ticks;

		[Sync]
		int damageTicks;

		public SelfHealing(Actor self, SelfHealingInfo info)
			: base(info)
		{
			health = self.Trait<IHealth>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead || IsTraitDisabled)
				return;

			// Cast to long to avoid overflow when multiplying by the health
			if (health.HP >= Info.HealIfBelow * (long)health.MaxHP / 100)
				return;

			if (damageTicks > 0)
			{
				--damageTicks;
				return;
			}

			if (--ticks <= 0)
			{
				ticks = Info.Delay;

				// Cast to long to avoid overflow when multiplying by the health
				self.InflictDamage(self, new Damage((int)(-(Info.Step + Info.PercentageStep * (long)health.MaxHP / 100)), Info.DamageTypes));
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value > 0)
				damageTicks = Info.DamageCooldown;
		}
	}
}
