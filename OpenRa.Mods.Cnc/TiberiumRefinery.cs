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

using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.Cnc
{
	class TiberiumRefineryInfo : ITraitInfo
	{
		public object Create(Actor self) { return new TiberiumRefinery(self); }
	}

	class TiberiumRefinery : IAcceptOre
	{
		Actor self;
		public TiberiumRefinery(Actor self)
		{
			this.self = self;
			self.World.AddFrameEndTask(
				w =>
				{		/* create the free harvester! */
					var harvester = w.CreateActor("harv", self.Location + new int2(0, 2), self.Owner);
					var unit = harvester.traits.Get<Unit>();
					unit.Facing = 64;
					harvester.QueueActivity(new Harvest());
				});
		}

		public int2 DeliverOffset {	get { return new int2(0, 2); } }
		public void OnDock(Actor harv, DeliverOre dockOrder)
		{
			// Todo: need to be careful about cancellation and multiple harvs
			harv.QueueActivity(new Move(self.Location + new int2(1,1), self));
			harv.QueueActivity(new Turn(96));
			harv.QueueActivity( new CallFunc( () => 
				self.traits.Get<RenderBuilding>().PlayCustomAnimThen(self, "active", () => {
					harv.traits.Get<Harvester>().Deliver(harv, self);
					harv.QueueActivity(new Move(self.Location + DeliverOffset, self));
					harv.QueueActivity(new Harvest());
			})));
		}
	}
}
