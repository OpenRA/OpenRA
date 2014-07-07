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
	public class WithMakeAnimationInfo : ITraitInfo, Requires<RenderBuildingInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "make";

		public object Create(ActorInitializer init) { return new WithMakeAnimation(init, this); }
	}

	public class WithMakeAnimation : ITick
	{
		WithMakeAnimationInfo info;
		RenderBuilding building;
		bool buildComplete;

		public WithMakeAnimation(ActorInitializer init, WithMakeAnimationInfo info)
		{
			building = init.self.Trait<RenderBuilding>();
			this.info = info;
			buildComplete = init.Contains<SkipMakeAnimsInit>();
		}

		public void Tick(Actor self)
		{
			if (self.IsDead() || buildComplete)
				return;

			buildComplete = true;

			building.PlayCustomAnimThen(self, info.Sequence, () => 
			{
				foreach (var notify in self.TraitsImplementing<INotifyBuildComplete>())
					notify.BuildingComplete(self);
			});
		}

		public void Reverse(Actor self, Activity activity)
		{
			building.PlayCustomAnimBackwards(self, info.Sequence, () => {
				building.PlayCustomAnim(self, info.Sequence); // avoids visual glitches as we wait for the actor to get destroyed
				self.QueueActivity(activity);
			});
		}
	}
}
