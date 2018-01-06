#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class InfiltrateForPowerOutageInfo : ITraitInfo
	{
		public readonly HashSet<string> Types = new HashSet<string>();

		public readonly int Duration = 25 * 20;

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

		void INotifyInfiltrated.Infiltrated(Actor self, Actor infiltrator, HashSet<string> types)
		{
			if (!info.Types.Overlaps(types))
				return;

			playerPower.TriggerPowerOutage(info.Duration);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			playerPower = self.Owner.PlayerActor.Trait<PowerManager>();
		}
	}
}