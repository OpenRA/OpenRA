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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the building animation when a support power is triggered.")]
	public class WithSupportPowerActivationAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "active";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithSupportPowerActivationAnimation(init.Self, this); }
	}

	public class WithSupportPowerActivationAnimation : ConditionalTrait<WithSupportPowerActivationAnimationInfo>, INotifySupportPower
	{
		readonly WithSpriteBody wsb;

		public WithSupportPowerActivationAnimation(Actor self, WithSupportPowerActivationAnimationInfo info)
			: base(info)
		{
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		void INotifySupportPower.Charged(Actor self) { }

		void INotifySupportPower.Activated(Actor self)
		{
			if (!IsTraitDisabled)
				wsb.PlayCustomAnimation(self, Info.Sequence, () => wsb.CancelCustomAnimation(self));
		}

		protected override void TraitDisabled(Actor self)
		{
			wsb.CancelCustomAnimation(self);
		}
	}
}
