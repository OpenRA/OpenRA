#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class OreRefineryInfo : RefineryInfo
	{
		public override object Create(ActorInitializer init) { return new OreRefinery(init.self, this); }
	}

	public class OreRefinery : Refinery

	{
		public override Activity DockSequence(Actor harv, Actor self) { return new RAHarvesterDockSequence(harv, self, Info.DockAngle); }

		public OreRefinery(Actor self, OreRefineryInfo info) : base(self, info) { }
	}
}
