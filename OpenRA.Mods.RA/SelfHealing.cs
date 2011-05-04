#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SelfHealingInfo : ITraitInfo, Requires<HealthInfo>
	{
		public readonly int Step = 5;
		public readonly int Ticks = 5;
		public readonly float HealIfBelow = .5f;
		
		public virtual object Create(ActorInitializer init) { return new SelfHealing(this); }
	}

	class SelfHealing : ITick, ISync
	{
		[Sync]
		int ticks;
		SelfHealingInfo Info;
		
		public SelfHealing(SelfHealingInfo info) { Info = info; }

		public void Tick(Actor self)
		{
			if (self.IsDead())
				return;
			
			var health = self.Trait<Health>();
			if (health.HP >= Info.HealIfBelow*health.MaxHP)
				return;

			if (--ticks <= 0)
			{
				ticks = Info.Ticks;
				self.InflictDamage(self, -Info.Step, null);
			}
		}
	}
}
