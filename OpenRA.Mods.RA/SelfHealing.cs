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

		[Desc("The Type defined by SelfHealingTech required to enable this.")]
		public readonly string RequiresTech = null;

		public virtual object Create(ActorInitializer init) { return new SelfHealing(this); }
	}

	class SelfHealing : ITick, ISync, INotifyDamage
	{
		[Sync] int ticks;
		[Sync] int damageTicks;
		SelfHealingInfo Info;

		public SelfHealing(SelfHealingInfo info) { Info = info; }

		public void Tick(Actor self)
		{
			if (self.IsDead())
				return;

			if (Info.RequiresTech != null && !self.World.ActorsWithTrait<SelfHealingTech>()
				.Any(a => !a.Actor.IsDead() && a.Actor.Owner.IsAlliedWith(self.Owner) && Info.RequiresTech == a.Trait.Type))
					return;

			var health = self.Trait<Health>();
			if (health.HP >= Info.HealIfBelow*health.MaxHP)
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

	[Desc("Attach this to an actor required as prerequisite for all owned units to regenerate health.")]
	class SelfHealingTechInfo : ITraitInfo
	{
		public readonly string Type = null;

		public object Create(ActorInitializer init) { return new SelfHealingTech(this); }
	}

	class SelfHealingTech
	{
		public string Type { get { return info.Type; } }

		readonly SelfHealingTechInfo info;

		public SelfHealingTech(SelfHealingTechInfo info)
		{
			this.info = info;
		}
	}
}
