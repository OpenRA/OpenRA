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

using OpenRa.Traits.Activities;

namespace OpenRa.Traits
{
	class OreRefineryInfo : ITraitInfo
	{
		public object Create(Actor self) { return new OreRefinery(self); }
	}

	class OreRefinery : IAcceptOre
	{
		Actor self;
		public OreRefinery(Actor self)
		{
			this.self = self;
			self.World.AddFrameEndTask(
				w =>
				{		/* create the free harvester! */
					var harvester = w.CreateActor("harv", self.Location 
						+ new int2(1, 2), self.Owner);
					var unit = harvester.traits.Get<Unit>();
					unit.Facing = 64;
					harvester.QueueActivity(new Harvest());
				});
		}
		public int2 DeliverOffset {	get { return new int2(1, 2); } }
		public void OnDock(Actor harv, DeliverOre dockOrder)
		{
			var unit = harv.traits.Get<Unit>();
			if (unit.Facing != 64)
				harv.QueueActivity(new Turn(64));
				
			harv.QueueActivity( new CallFunc( () => {
				var renderUnit = harv.traits.Get<RenderUnit>();
				if (renderUnit.anim.CurrentSequence.Name != "empty")
					renderUnit.PlayCustomAnimation(harv, "empty", () =>
					{
						harv.traits.Get<Harvester>().Deliver(harv, self);
						harv.QueueActivity(new Harvest());
					});
				}
			));
		}
	}
}
