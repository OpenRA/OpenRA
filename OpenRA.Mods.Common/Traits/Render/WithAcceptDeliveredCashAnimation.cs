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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the building animation when it accepts a cash delivery unit.")]
	public class WithAcceptDeliveredCashAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public object Create(ActorInitializer init) { return new WithAcceptDeliveredCashAnimation(init.Self, this); }
	}

	public class WithAcceptDeliveredCashAnimation : INotifyCashTransfer, INotifyBuildComplete, INotifySold
	{
		readonly WithAcceptDeliveredCashAnimationInfo info;
		readonly WithSpriteBody wsb;
		bool buildComplete;

		public WithAcceptDeliveredCashAnimation(Actor self, WithAcceptDeliveredCashAnimationInfo info)
		{
			this.info = info;
			wsb = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == info.Body);
		}

		void INotifyBuildComplete.BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		void INotifySold.Selling(Actor self)
		{
			buildComplete = false;
		}

		void INotifySold.Sold(Actor self) { }

		bool playing;
		void INotifyCashTransfer.OnAcceptingCash(Actor self, Actor donor)
		{
			if (!buildComplete || playing)
				return;

			playing = true;
			wsb.PlayCustomAnimation(self, info.Sequence, () => playing = false);
		}

		void INotifyCashTransfer.OnDeliveringCash(Actor self, Actor acceptor) { }
	}
}