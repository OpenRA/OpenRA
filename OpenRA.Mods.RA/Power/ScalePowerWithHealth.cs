#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Power
{
	[Desc("Scale power amount with the current health.")]
	public class ScalePowerWithHealthInfo : ITraitInfo, Requires<PowerInfo>, Requires<HealthInfo>
	{
		public object Create(ActorInitializer init) { return new ScalePowerWithHealth(init.self); }
	}

	public class ScalePowerWithHealth : IPowerModifier, INotifyDamage
	{
		readonly Power power;
		readonly Health health;

		public ScalePowerWithHealth(Actor self)
		{
			power = self.Trait<Power>();
			health = self.Trait<Health>();
		}

		public int GetPowerModifier()
		{
			return 100 * health.HP / health.MaxHP;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			power.PlayerPower.UpdateActor(self);
		}
	}
}
