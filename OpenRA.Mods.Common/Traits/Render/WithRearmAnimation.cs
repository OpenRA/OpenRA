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
	[Desc("Replaces the building animation when it rearms a unit.")]
	public class WithRearmAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "active";

		[Desc("Which sprite body to play the animation on.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithRearmAnimation(init.Self, this); }
	}

	public class WithRearmAnimation : ConditionalTrait<WithRearmAnimationInfo>, INotifyRearm, INotifyBuildComplete, INotifySold
	{
		readonly WithSpriteBody spriteBody;
		bool buildComplete;

		public WithRearmAnimation(Actor self, WithRearmAnimationInfo info)
			: base(info)
		{
			spriteBody = self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		void INotifyRearm.Rearming(Actor self, Actor target)
		{
			if (buildComplete && !IsTraitDisabled)
				spriteBody.PlayCustomAnimation(self, Info.Sequence);
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