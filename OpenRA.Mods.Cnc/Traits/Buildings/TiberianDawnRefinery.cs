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
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.RA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class TiberianDawnRefineryInfo : RefineryInfo
	{
		public override object Create(ActorInitializer init) { return new TiberianDawnRefinery(init.Self, this); }
	}

	public class TiberianDawnRefinery : Refinery
	{
		public TiberianDawnRefinery(Actor self, RefineryInfo info) : base(self, info) { }

		public override Activity DockSequence(Actor harv, Actor self)
		{
			return new TDHarvesterDockSequence(harv, self);
		}
	}
}
