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

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the building animation when `NukePower` is triggered.")]
	public class WithNukeLaunchAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		public override object Create(ActorInitializer init) { return new WithNukeLaunchAnimation(init.Self, this); }
	}

	public class WithNukeLaunchAnimation : ConditionalTrait<WithNukeLaunchAnimationInfo>, INotifyNuke, INotifyBuildComplete, INotifySold
	{
		readonly WithSpriteBody spriteBody;
		bool buildComplete;

		public WithNukeLaunchAnimation(Actor self, WithNukeLaunchAnimationInfo info)
			: base(info)
		{
			spriteBody = self.TraitOrDefault<WithSpriteBody>();
		}

		void INotifyNuke.Launching(Actor self)
		{
			if (buildComplete && spriteBody != null && !IsTraitDisabled)
				spriteBody.PlayCustomAnimation(self, Info.Sequence, () => spriteBody.CancelCustomAnimation(self));
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