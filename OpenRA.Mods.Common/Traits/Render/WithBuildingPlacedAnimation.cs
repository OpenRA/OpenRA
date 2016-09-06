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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	[Desc("Changes the animation when the actor constructed a building.")]
	public class WithBuildingPlacedAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>
	{
		[Desc("Sequence name to use"), SequenceReference]
		public readonly string Sequence = "build";

		public object Create(ActorInitializer init) { return new WithBuildingPlacedAnimation(init.Self, this); }
	}

	public class WithBuildingPlacedAnimation : INotifyBuildingPlaced, INotifyBuildComplete
	{
		readonly WithBuildingPlacedAnimationInfo info;
		readonly WithSpriteBody wsb;
		bool buildComplete;

		public WithBuildingPlacedAnimation(Actor self, WithBuildingPlacedAnimationInfo info)
		{
			this.info = info;
			wsb = self.Trait<WithSpriteBody>();
			buildComplete = !self.Info.HasTraitInfo<BuildingInfo>();
		}

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		public void BuildingPlaced(Actor self)
		{
			if (buildComplete)
				wsb.PlayCustomAnimation(self, info.Sequence, () => wsb.CancelCustomAnimation(self));
		}
	}
}