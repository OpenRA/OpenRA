#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	[Desc("Attach this to actors which should regenerate or lose health points over time.")]
	class ChangesHealthInfo : ConditionalTraitInfo, Requires<IHealthInfo>
	{
		[Desc("Absolute amount of health points added in each step.",
			"Use negative values to apply damage.")]
		public readonly int Step = 5;

		[Desc("Relative percentages of health added in each step.",
			"Use negative values to apply damage.",
			"When both values are defined, their summary will be applied.")]
		public readonly int PercentageStep = 0;

		[Desc("Time in ticks to wait between each health modification.")]
		public readonly int Delay = 5;

		[Desc("Heal if current health is below this percentage of full health.")]
		public readonly int StartIfBelow = 50;

		[Desc("Time in ticks to wait after taking damage.")]
		public readonly int DamageCooldown = 0;

		[Desc("Apply the health change when encountering these damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		public override object Create(ActorInitializer init) { return new ChangesHealth(init.Self, this); }
	}

	class ChangesHealth : ConditionalTrait<ChangesHealthInfo>, ITick, INotifyDamage, ISync
	{
		readonly IHealth health;

		[Sync]
		int ticks;

		[Sync]
		int damageTicks;

		public ChangesHealth(Actor self, ChangesHealthInfo info)
			: base(info)
		{
			health = self.Trait<IHealth>();
		}

		void ITick.Tick(Actor self)
		{
			if (self.IsDead || IsTraitDisabled)
				return;

			// Cast to long to avoid overflow when multiplying by the health
			if (health.HP >= Info.StartIfBelow * (long)health.MaxHP / 100)
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
				self.InflictDamage(self, new Damage((int)-(Info.Step + Info.PercentageStep * (long)health.MaxHP / 100), Info.DamageTypes));
			}
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage.Value > 0)
				damageTicks = Info.DamageCooldown;
		}
	}
}
