#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Replaces the idle animation of a building.")]
	public class WithActiveAnimationInfo : ITraitInfo, Requires<RenderBuildingInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "active";

		public readonly int Interval = 750;

		public readonly bool PauseOnLowPower = false;

		public object Create(ActorInitializer init) { return new WithActiveAnimation(init.Self, this); }
	}

	public class WithActiveAnimation : ITick, INotifyBuildComplete, INotifySold
	{
		readonly WithActiveAnimationInfo info;
		readonly RenderBuilding renderBuilding;

		public WithActiveAnimation(Actor self, WithActiveAnimationInfo info)
		{
			renderBuilding = self.Trait<RenderBuilding>();
			this.info = info;
		}

		int ticks;
		public void Tick(Actor self)
		{
			if (!buildComplete)
				return;

			if (--ticks <= 0)
			{
				if (!(info.PauseOnLowPower && self.IsDisabled()))
					renderBuilding.PlayCustomAnim(self, info.Sequence);
				ticks = info.Interval;
			}
		}

		bool buildComplete = false;

		public void BuildingComplete(Actor self)
		{
			buildComplete = true;
		}

		public void Selling(Actor self)
		{
			buildComplete = false;
		}

		public void Sold(Actor self) { }
	}
}
