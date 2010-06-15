#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Cnc
{
	class TiberiumRefineryDockActionInfo : TraitInfo<TiberiumRefineryDockAction> {}

	class TiberiumRefineryDockAction : IAcceptOreDockAction, ITraitPrerequisite<IAcceptOre>
	{
		Actor dockedHarv = null;
		public void OnDock(Actor self, Actor harv, DeliverResources dockOrder)
		{
			float2 startDock = harv.CenterLocation;
			float2 endDock = self.CenterLocation + new float2(-15,8);
			var harvester = harv.traits.Get<Harvester>();

			harv.QueueActivity( new Turn(112) );
			harv.QueueActivity( new CallFunc( () =>
			{
				dockedHarv = harv;
				self.traits.Get<RenderBuilding>().PlayCustomAnim(self, "active");
			}) );
			harv.QueueActivity( new Drag(startDock, endDock, 11) );
			harv.QueueActivity( new CallFunc( () =>
			{
				self.World.AddFrameEndTask( w1 =>
				{
					harvester.Visible = false;
					harvester.Deliver(harv, self);
				});
			}) );
			harv.QueueActivity( new Wait(18) );
			harv.QueueActivity( new CallFunc( () => harvester.Visible = true) );
			harv.QueueActivity( new Drag(endDock, startDock, 11) );
			harv.QueueActivity( new CallFunc( () => dockedHarv = null) );
			harv.QueueActivity( new Harvest() );
		}
		
	}
}
