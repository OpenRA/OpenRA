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
	[Desc("Scale power amount with the current health.")]
	public class ScalePowerWithHealthInfo : ITraitInfo, Requires<PowerInfo>, Requires<HealthInfo>
	{
		public object Create(ActorInitializer init) { return new ScalePowerWithHealth(init.Self); }
	}

	public class ScalePowerWithHealth : IPowerModifier, INotifyDamage, INotifyOwnerChanged
	{
		readonly Health health;
		PowerManager power;

		public ScalePowerWithHealth(Actor self)
		{
			power = self.Owner.PlayerActor.Trait<PowerManager>();
			health = self.Trait<Health>();
		}

		public int GetPowerModifier()
		{
			return 100 * health.HP / health.MaxHP;
		}

		public void Damaged(Actor self, AttackInfo e) { power.UpdateActor(self); }
		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			power = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
