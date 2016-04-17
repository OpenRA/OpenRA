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

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("This actor displays a charge-up animation before firing.")]
	public class WithChargeAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		[Desc("Sequence to use for charge animation.")]
		[SequenceReference] public readonly string ChargeSequence = "active";

		public object Create(ActorInitializer init) { return new WithChargeAnimation(init, this); }
	}

	public class WithChargeAnimation : INotifyCharging
	{
		readonly WithChargeAnimationInfo info;
		readonly WithSpriteBody wsb;

		public WithChargeAnimation(ActorInitializer init, WithChargeAnimationInfo info)
		{
			this.info = info;
			wsb = init.Self.Trait<WithSpriteBody>();
		}

		public void Charging(Actor self, Target target)
		{
			wsb.PlayCustomAnimation(self, info.ChargeSequence, () => wsb.CancelCustomAnimation(self));
		}
	}
}
