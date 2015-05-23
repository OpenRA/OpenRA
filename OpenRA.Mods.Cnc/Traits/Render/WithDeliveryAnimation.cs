#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[Desc("Building animation to play when ProductionAirdrop is used to deliver units.")]
	public class WithDeliveryAnimationInfo : ITraitInfo, Requires<RenderBuildingInfo>
	{
		public readonly string ActiveSequence = "active";

		public readonly string IdleSequence = "idle";

		public object Create(ActorInitializer init) { return new WithDeliveryAnimation(init.Self, this); }
	}

	public class WithDeliveryAnimation : INotifyDelivery
	{
		readonly WithDeliveryAnimationInfo info;
		readonly RenderBuilding building;

		public WithDeliveryAnimation(Actor self, WithDeliveryAnimationInfo info)
		{
			building = self.Trait<RenderBuilding>();

			this.info = info;
		}

		public void IncomingDelivery(Actor self)
		{
			building.PlayCustomAnimRepeating(self, info.ActiveSequence);
		}

		public void Delivered(Actor self)
		{
			building.PlayCustomAnimRepeating(self, info.IdleSequence);
		}
	}
}