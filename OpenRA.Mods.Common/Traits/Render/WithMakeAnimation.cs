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

using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Replaces the sprite during construction.")]
	public class WithMakeAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "make";

		public object Create(ActorInitializer init) { return new WithMakeAnimation(init, this); }
	}

	public class WithMakeAnimation
	{
		readonly WithMakeAnimationInfo info;
		readonly WithSpriteBody wsb;

		public WithMakeAnimation(ActorInitializer init, WithMakeAnimationInfo info)
		{
			this.info = info;
			var self = init.Self;
			wsb = self.Trait<WithSpriteBody>();

			var building = self.TraitOrDefault<Building>();
			if (building != null && !building.SkipMakeAnimation)
			{
				wsb.PlayCustomAnimation(self, info.Sequence, () =>
				{
					building.NotifyBuildingComplete(self);
				});
			}
		}

		public void Reverse(Actor self, Activity activity, bool queued = true)
		{
			wsb.PlayCustomAnimationBackwards(self, info.Sequence, () =>
			{
				// avoids visual glitches as we wait for the actor to get destroyed
				wsb.DefaultAnimation.PlayFetchIndex(info.Sequence, () => 0);
				self.QueueActivity(queued, activity);
			});
		}
	}
}
