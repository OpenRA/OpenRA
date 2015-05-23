#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Mods.TS.Activities;

namespace OpenRA.Mods.TS.Traits
{
	public class TiberianSunRefineryInfo : RefineryInfo
	{
		public override object Create(ActorInitializer init) { return new TiberianSunRefinery(init.Self, this); }
	}

	public class TiberianSunRefinery : Refinery
	{
		public TiberianSunRefinery(Actor self, RefineryInfo info) : base(self, info) { }

		public override Activity DockSequence(Actor harv, Actor self)
		{
			return new VoxelHarvesterDockSequence(harv, self, DeliveryAngle, IsDragRequired, DragOffset, DragLength);
		}
	}
}
