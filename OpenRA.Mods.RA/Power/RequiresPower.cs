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
	class RequiresPowerInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new RequiresPower(init.self); }
	}

	class RequiresPower : IDisable, INotifyOwnerChanged
	{
		PowerManager playerPower;

		public RequiresPower(Actor self)
		{
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		public bool Disabled
		{
			get { return playerPower.PowerProvided < playerPower.PowerDrained; }
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerPower = newOwner.PlayerActor.Trait<PowerManager>();
		}
	}
}
