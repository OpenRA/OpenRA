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

using OpenRA.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.yupgi_alert.Traits.Render
{
	[Desc("Replaces the building animation when it accepts a cash delivery unit.")]
	public class WithAcceptDeliveredCashAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		public readonly bool PauseOnLowPower = false;

		public object Create(ActorInitializer init) { return new WithAcceptDeliveredCashAnimation(init.Self, this); }
	}

	public class WithAcceptDeliveredCashAnimation : INotifyCashTransfer, INotifyBuildComplete, INotifySold
	{
		readonly WithAcceptDeliveredCashAnimationInfo info;
		readonly WithSpriteBody spriteBody;
		bool buildComplete;

		public WithAcceptDeliveredCashAnimation(Actor self, WithAcceptDeliveredCashAnimationInfo info)
		{
			this.info = info;
			spriteBody = self.TraitOrDefault<WithSpriteBody>();
		}

		bool playing;
		void INotifyCashTransfer.OnCashTransfer(Actor self, Actor donor)
		{
			if (buildComplete && !playing && spriteBody != null && !(info.PauseOnLowPower && self.IsDisabled()))
			{
				playing = true;
				spriteBody.PlayCustomAnimation(self, info.Sequence, () => {
					spriteBody.CancelCustomAnimation(self);
					playing = false;
				});
			}
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
	}
}