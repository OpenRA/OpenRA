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
	[Desc("Modifies the power usage/output of this actor.")]
	public class PowerMultiplierInfo : ConditionalTraitInfo
	{
		[FieldLoader.Require]
		[Desc("Percentage modifier to apply.")]
		public readonly int Modifier = 100;

		public override object Create(ActorInitializer init) { return new PowerMultiplier(init.Self, this); }
	}

	public class PowerMultiplier : ConditionalTrait<PowerMultiplierInfo>, IPowerModifier, INotifyOwnerChanged
	{
		PowerManager power;

		public PowerMultiplier(Actor self, PowerMultiplierInfo info)
			: base(info)
		{
			power = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		protected override void TraitEnabled(Actor self) { power.UpdateActor(self); }
		protected override void TraitDisabled(Actor self) { power.UpdateActor(self); }

		int IPowerModifier.GetPowerModifier() { return IsTraitDisabled ? 100 : Info.Modifier; }

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			power = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
