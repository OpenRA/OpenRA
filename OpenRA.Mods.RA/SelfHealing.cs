#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Attach this to actors which should be able to regenerate their health points.")]
	class SelfHealingInfo : UpgradableTraitInfo, ITraitInfo, Requires<HealthInfo>
	{
		public readonly int Step = 5;
		public readonly int Ticks = 5;
		public readonly float HealIfBelow = .5f;
		public readonly int DamageCooldown = 0;

		public virtual object Create(ActorInitializer init) { return new SelfHealing(init.self, this); }
	}

	class SelfHealing : UpgradableTrait<SelfHealingInfo>, ITick, INotifyDamage
	{
		readonly Health health;

		[Sync] int ticks;
		[Sync] int damageTicks;

		public SelfHealing(Actor self, SelfHealingInfo info)
			: base (info)
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
