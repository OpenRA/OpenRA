#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Attach this to actors which should be able to regenerate their health points.")]
	class SelfHealingInfo : ITraitInfo, Requires<HealthInfo>
	{
		public readonly int Step = 5;
		public readonly int Ticks = 5;
		public readonly float HealIfBelow = .5f;
		public readonly int DamageCooldown = 0;

		[Desc("Enable only if this upgrade is enabled.")]
		public readonly string RequiresUpgrade = null;

		public virtual object Create(ActorInitializer init) { return new SelfHealing(init.self, this); }
	}

	class SelfHealing : ITick, ISync, INotifyDamage, IUpgradable
	{
		readonly SelfHealingInfo info;
		readonly Health health;

		[Sync] int ticks;
		[Sync] int damageTicks;
		[Sync] bool disabled;


		public SelfHealing(Actor self, SelfHealingInfo info)
		{
			this.info = info;

			health = self.Trait<Health>();

			// Disable if an upgrade is required
			disabled = info.RequiresUpgrade != null;
		}

		public bool AcceptsUpgrade(string type)
		{
			return type == info.RequiresUpgrade;
		}

		public void UpgradeAvailable(Actor self, string type, bool available)
		{
			if (type == info.RequiresUpgrade)
				disabled = !available;
		}

		public void Tick(Actor self)
		{
			if (self.Flagged(ActorFlag.Dead) || disabled)
				return;

			if (health.HP >= info.HealIfBelow * health.MaxHP)
				return;
			
			if (damageTicks > 0)
			{
				--damageTicks;
				return;
			}

			if (--ticks <= 0)
			{
				ticks = info.Ticks;
				self.InflictDamage(self, -info.Step, null);
			}
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.Damage > 0)
				damageTicks = info.DamageCooldown;
		}
	}
}
