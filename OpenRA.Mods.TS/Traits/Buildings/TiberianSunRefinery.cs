#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits
{
	public class TiberianSunRefineryInfo : OreRefineryInfo
	{
		public override object Create(ActorInitializer init) { return new TiberianSunRefinery(init.self, this); }
	}

	public class TiberianSunRefinery : OreRefinery
	{
		public TiberianSunRefinery(Actor self, TiberianSunRefineryInfo info) : base(self, info) { }

		public override Activity DockSequence(Actor harv, Actor self)
		{
			return new VoxelHarvesterDockSequence(harv, self);
		}
	}
}
