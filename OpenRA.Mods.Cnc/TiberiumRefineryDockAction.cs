#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Drawing;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.Cnc
{
	class TiberiumRefineryDockActionInfo : TraitInfo<TiberiumRefineryDockAction> {}
	class TiberiumRefineryDockAction : OreRefineryDockAction
	{
		public override IActivity DockSequence(Actor harv, Actor self)
		{
			return new HarvesterDockSequence(harv, self);
		}
	}
}
