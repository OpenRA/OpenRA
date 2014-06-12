#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	public class TiberiumRefineryInfo : RefineryInfo
	{
		public override object Create(ActorInitializer init) { return new TiberiumRefinery(init.self, this); }
	}

	public class TiberiumRefinery : Refinery
	{
		public TiberiumRefinery(Actor self, TiberiumRefineryInfo info) : base(self, info) { }

		public override Activity DockSequence(Actor harv, Actor self)
		{
			return new HarvesterDockSequence(harv, self);
		}
	}
}
