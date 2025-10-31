#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	public class ScalePowerWithHealthInfo : TraitInfo, Requires<PowerInfo>, Requires<IHealthInfo>
	{
		public override object Create(ActorInitializer init) { return new ScalePowerWithHealth(init.Self); }
	}

	public class ScalePowerWithHealth : IPowerModifier, INotifyDamage, INotifyOwnerChanged
	{
		readonly IHealth health;
		PowerManager power;

		public ScalePowerWithHealth(Actor self)
		{
			power = self.Owner.PlayerActor.Trait<PowerManager>();
			health = self.Trait<IHealth>();
		}

		int IPowerModifier.GetPowerModifier()
		{
			// Cast to long to avoid overflow when multiplying by the health
			return (int)(100L * health.HP / health.MaxHP);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e) { power.UpdateActor(self); }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			power = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
