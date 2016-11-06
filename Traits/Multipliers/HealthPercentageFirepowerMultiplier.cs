#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Allow the actor to use it's health percentage",
		"as a firepower multiplier.")]
	class HealthPercentageFirepowerMultiplierInfo : UpgradableTraitInfo, ITraitInfo, Requires<HealthInfo>
	{
		public override object Create(ActorInitializer init) {
			return new HealthPercentageFirepowerMultiplier(init.Self, this);
		}
	}

	class HealthPercentageFirepowerMultiplier : UpgradableTrait<HealthPercentageFirepowerMultiplierInfo>,  IFirepowerModifier
	{
		readonly Health health;

		public HealthPercentageFirepowerMultiplier(Actor self, HealthPercentageFirepowerMultiplierInfo info)
			: base(info)
		{
			health = self.Trait<Health>();
		}

		int IFirepowerModifier.GetFirepowerModifier()
		{
			return IsTraitDisabled ? 100 : 100 * health.HP / health.MaxHP;
		}
	}
}