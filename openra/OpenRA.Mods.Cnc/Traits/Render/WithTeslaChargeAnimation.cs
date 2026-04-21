#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits.Render
{
	[Desc("This actor displays a charge-up animation before firing.")]
	public class WithTeslaChargeAnimationInfo : TraitInfo, Requires<WithSpriteBodyInfo>, Requires<RenderSpritesInfo>
	{
		[SequenceReference]
		[Desc("Sequence to use for charge animation.")]
		public readonly string ChargeSequence = "active";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithTeslaChargeAnimation(init, this); }
	}

	public class WithTeslaChargeAnimation : INotifyTeslaCharging
	{
		readonly WithTeslaChargeAnimationInfo info;
		readonly WithSpriteBody wsb;

		public WithTeslaChargeAnimation(ActorInitializer init, WithTeslaChargeAnimationInfo info)
		{
			this.info = info;
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
		}

		void INotifyTeslaCharging.Charging(Actor self, in Target target)
		{
			wsb.PlayCustomAnimation(self, info.ChargeSequence, () => wsb.CancelCustomAnimation(self));
		}
	}
}
