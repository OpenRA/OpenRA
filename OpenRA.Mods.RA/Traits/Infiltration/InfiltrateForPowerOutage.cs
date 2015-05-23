#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class InfiltrateForPowerOutageInfo : ITraitInfo
	{
		public readonly int Duration = 25 * 30;

		public object Create(ActorInitializer init) { return new InfiltrateForPowerOutage(init.Self, this); }
	}

	class InfiltrateForPowerOutage : INotifyOwnerChanged, INotifyInfiltrated
	{
		readonly InfiltrateForPowerOutageInfo info;
		PowerManager playerPower;

		public InfiltrateForPowerOutage(Actor self, InfiltrateForPowerOutageInfo info)
		{
			this.info = info;
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}

		public void Infiltrated(Actor self, Actor infiltrator)
		{
			playerPower.TriggerPowerOutage(info.Duration);
		}

		public void OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}
	}
}