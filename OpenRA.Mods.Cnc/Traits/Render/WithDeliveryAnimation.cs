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

using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	[Desc("Building animation to play when ProductionAirdrop is used to deliver units.")]
	public class WithDeliveryAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		public readonly string ActiveSequence = "active";

		public readonly string IdleSequence = "idle";

		public object Create(ActorInitializer init) { return new WithDeliveryAnimation(init.Self, this); }
	}

	public class WithDeliveryAnimation : INotifyDelivery
	{
		readonly WithDeliveryAnimationInfo info;
		readonly WithSpriteBody wsb;

		public WithDeliveryAnimation(Actor self, WithDeliveryAnimationInfo info)
		{
			wsb = self.Trait<WithSpriteBody>();

			this.info = info;
		}

		public void IncomingDelivery(Actor self)
		{
			wsb.PlayCustomAnimationRepeating(self, info.ActiveSequence);
		}

		public void Delivered(Actor self)
		{
			wsb.PlayCustomAnimationRepeating(self, info.IdleSequence);
		}
	}
}