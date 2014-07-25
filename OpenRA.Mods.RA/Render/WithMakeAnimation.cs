#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	public class WithMakeAnimationInfo : ITraitInfo, Requires<BuildingInfo>, Requires<RenderBuildingInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "make";

		public object Create(ActorInitializer init) { return new WithMakeAnimation(init, this); }
	}

	public class WithMakeAnimation : ICustomBuild
	{
		readonly WithMakeAnimationInfo info;
		readonly RenderBuilding renderBuilding;
		bool skipMakeAnimation;
		bool done = false;

		public WithMakeAnimation(ActorInitializer init, WithMakeAnimationInfo info)
		{
			this.info = info;
			var self = init.self;
			renderBuilding = self.Trait<RenderBuilding>();

			skipMakeAnimation = init.Contains<SkipMakeAnimsInit>();
		}

		public void CustomBuild(Actor self)
		{
			if (skipMakeAnimation)
			{
				done = true;
				return;
			}
			renderBuilding.PlayCustomAnimThen(self, info.Sequence, () => 
			{
				done = true;
				self.Trait<Building>().NotifyBuildingComplete(self);
			});
		}

		public bool IsCustomBuildComplete(Actor self)
		{
			return done;
		}

		public void Reverse(Actor self, Activity activity)
		{
			renderBuilding.PlayCustomAnimBackwards(self, info.Sequence, () =>
			{
				// avoids visual glitches as we wait for the actor to get destroyed
				renderBuilding.DefaultAnimation.PlayFetchIndex(info.Sequence, () => 0);
				self.QueueActivity(activity);
			});
		}
	}
}
