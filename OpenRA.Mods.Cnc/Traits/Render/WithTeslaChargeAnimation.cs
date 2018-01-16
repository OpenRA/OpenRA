#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	[Desc("This actor displays a charge-up animation before firing.")]
	public class WithTeslaChargeAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		[Desc("Sequence to use for charge animation.")]
		[SequenceReference] public readonly string ChargeSequence = "active";

		public object Create(ActorInitializer init) { return new WithTeslaChargeAnimation(init, this); }
	}

	public class WithTeslaChargeAnimation : INotifyTeslaCharging
	{
		readonly WithTeslaChargeAnimationInfo info;
		readonly WithSpriteBody wsb;

		public WithTeslaChargeAnimation(ActorInitializer init, WithTeslaChargeAnimationInfo info)
		{
			this.info = info;
			wsb = init.Self.Trait<WithSpriteBody>();
		}

		void INotifyTeslaCharging.Charging(Actor self, Target target)
		{
			wsb.PlayCustomAnimation(self, info.ChargeSequence, () => wsb.CancelCustomAnimation(self));
		}
	}
}
