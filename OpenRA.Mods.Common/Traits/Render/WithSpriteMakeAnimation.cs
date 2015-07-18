#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Temporary work-around. Duplicate of WithMakeAnimation for WithSpriteBody.")]
	public class WithSpriteMakeAnimationInfo : ITraitInfo, Requires<BuildingInfo>, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use")]
		[SequenceReference] public readonly string Sequence = "make";

		public object Create(ActorInitializer init) { return new WithSpriteMakeAnimation(init, this); }
	}

	public class WithSpriteMakeAnimation
	{
		readonly WithSpriteMakeAnimationInfo info;
		readonly WithSpriteBody wsb;

		public WithSpriteMakeAnimation(ActorInitializer init, WithSpriteMakeAnimationInfo info)
		{
			this.info = info;
			var self = init.Self;
			wsb = self.Trait<WithSpriteBody>();

			var building = self.Trait<Building>();
			if (!building.SkipMakeAnimation)
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
