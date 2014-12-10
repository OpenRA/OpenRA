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
using OpenRA.Mods.Common.Power;

namespace OpenRA.Mods.Common
{
	[Desc("The power usage/output of this actor is multiplied based on upgrade level.")]
	public class PowerMultiplierInfo : UpgradeMultiplierTraitInfo, ITraitInfo
	{
		public PowerMultiplierInfo()
			: base(new string[0], new int[0]) { }

		public object Create(ActorInitializer init) { return new PowerMultiplier(init.self, this); }
	}

	public class PowerMultiplier : UpgradeMultiplierTrait, IPowerModifier, INotifyOwnerChanged
	{
		PowerManager power;

		public PowerMultiplier(Actor self, PowerMultiplierInfo info)
			: base(info) { power = self.Owner.PlayerActor.Trait<PowerManager>(); }

		public int GetPowerModifier() { return GetModifier(); }
		protected override void Update(Actor self) { power.UpdateActor(self); }
		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			power = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
