#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Needs power to operate.")]
	class RequiresPowerInfo : UpgradableTraitInfo, ITraitInfo
	{
		public object Create(ActorInitializer init) { return new RequiresPower(init.Self, this); }
	}

	class RequiresPower : UpgradableTrait<RequiresPowerInfo>, IDisable, INotifyOwnerChanged
	{
		PowerManager playerPower;

		public RequiresPower(Actor self, RequiresPowerInfo info)
			: base(info)
		{
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		public bool Disabled
		{
			get { return playerPower.PowerProvided < playerPower.PowerDrained && !IsTraitDisabled; }
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
