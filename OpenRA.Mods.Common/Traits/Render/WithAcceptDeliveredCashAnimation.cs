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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the building animation when it accepts a cash delivery unit.")]
	public class WithAcceptDeliveredCashAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		public object Create(ActorInitializer init) { return new WithAcceptDeliveredCashAnimation(init.Self, this); }
	}

	public class WithAcceptDeliveredCashAnimation : INotifyCashTransfer, INotifyBuildComplete, INotifySold
	{
		readonly WithAcceptDeliveredCashAnimationInfo info;
		readonly WithSpriteBody[] wsbs;
		bool buildComplete;

		public WithAcceptDeliveredCashAnimation(Actor self, WithAcceptDeliveredCashAnimationInfo info)
		{
			this.info = info;
			wsbs = self.TraitsImplementing<WithSpriteBody>().ToArray();
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

			foreach (var wsb in wsbs)
			{
				playing = true;
				wsb.PlayCustomAnimation(self, info.Sequence, () => playing = false);
			}
		}

		void INotifyCashTransfer.OnDeliveringCash(Actor self, Actor acceptor) { }
	}
}