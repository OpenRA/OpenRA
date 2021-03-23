#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Building animation to play when ProductionAirdrop is used to deliver units.")]
	public class WithDeliveryAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference]
		public readonly string ActiveSequence = "active";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithDeliveryAnimation(init.Self, this); }
	}

	public class WithDeliveryAnimation : ConditionalTrait<WithDeliveryAnimationInfo>, INotifyDelivery
	{
		readonly WithSpriteBody wsb;

		public WithDeliveryAnimation(Actor self, WithDeliveryAnimationInfo info)
			: base(info)
		{
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
		}

		public void IncomingDelivery(Actor self)
		{
			if (!IsTraitDisabled)
				wsb.PlayCustomAnimationRepeating(self, Info.ActiveSequence);
		}

		public void Delivered(Actor self)
		{
			// Animation has already been cancelled by TraitDisabled below
			if (!IsTraitDisabled)
				wsb.CancelCustomAnimation(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			wsb.CancelCustomAnimation(self);
		}
	}
}
