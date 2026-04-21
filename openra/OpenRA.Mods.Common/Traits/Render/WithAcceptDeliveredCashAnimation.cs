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
	[Desc("Replaces the building animation when it accepts a cash delivery unit.")]
	public class WithAcceptDeliveredCashAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[SequenceReference]
		[Desc("Sequence name to use")]
		public readonly string Sequence = "active";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithAcceptDeliveredCashAnimation(init.Self, this); }
	}

	public class WithAcceptDeliveredCashAnimation : ConditionalTrait<WithAcceptDeliveredCashAnimationInfo>, INotifyCashTransfer
	{
		readonly WithSpriteBody wsb;

		public WithAcceptDeliveredCashAnimation(Actor self, WithAcceptDeliveredCashAnimationInfo info)
			: base(info)
		{
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
		}

		bool playing;
		void INotifyCashTransfer.OnAcceptingCash(Actor self, Actor donor)
		{
			if (IsTraitDisabled || playing)
				return;

			playing = true;
			wsb.PlayCustomAnimation(self, Info.Sequence, () => playing = false);
		}

		void INotifyCashTransfer.OnDeliveringCash(Actor self, Actor acceptor) { }
	}
}
