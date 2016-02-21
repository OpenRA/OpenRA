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
	[Desc("The power usage/output of this actor is multiplied based on upgrade level if specified.")]
	public class PowerMultiplierInfo : UpgradeMultiplierTraitInfo
	{
		public override object Create(ActorInitializer init) { return new PowerMultiplier(init.Self, this); }
	}

	public class PowerMultiplier : UpgradeMultiplierTrait, IPowerModifier, INotifyOwnerChanged
	{
		PowerManager power;

		public PowerMultiplier(Actor self, PowerMultiplierInfo info)
			: base(info, "PowerMultiplier", self.Info.Name) { power = self.Owner.PlayerActor.Trait<PowerManager>(); }

		public int GetPowerModifier() { return GetModifier(); }
		protected override void Update(Actor self) { power.UpdateActor(self); }
		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			power = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
