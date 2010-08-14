#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SelfHealingInfo : TraitInfo<SelfHealing>, ITraitPrerequisite<HealthInfo>
	{
		public readonly int Step = 5;
		public readonly int Ticks = 5;
		public readonly float HealIfBelow = .5f;
	}

	class SelfHealing : ITick
	{
		[Sync]
		int ticks;

		public void Tick(Actor self)
		{
			if (self.IsDead())
				return;
			
			var info = self.Info.Traits.Get<SelfHealingInfo>();
			if (self.Trait<Health>().HPFraction >= info.HealIfBelow)
				return;

			if (--ticks <= 0)
			{
				ticks = info.Ticks;
				self.InflictDamage(self, -info.Step, null);
			}
		}
	}
}
