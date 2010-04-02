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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class Harvest : IActivity
	{
		public IActivity NextActivity { get; set; }
		bool isHarvesting = false;

		public IActivity Tick( Actor self )
		{
			if( isHarvesting ) return this;
			if( NextActivity != null ) return NextActivity;

			var harv = self.traits.Get<Harvester>();

			if( harv.IsFull )
				return new DeliverResources { NextActivity = NextActivity };

			if (HarvestThisTile(self))
				return this;
			else
			{
				FindMoreResource(self);
				return NextActivity;
			}
		}

		bool HarvestThisTile(Actor self)
		{
			var harv = self.traits.Get<Harvester>();
			var renderUnit = self.traits.Get<RenderUnit>();	/* better have one of these! */

			var resource = self.World.WorldActor.traits.Get<ResourceLayer>().Harvest(self.Location);
			if (resource == null)
				return false;
			
			if (renderUnit.anim.CurrentSequence.Name != "harvest")
			{
				isHarvesting = true;
				renderUnit.PlayCustomAnimation(self, "harvest", () => isHarvesting = false);
			}
			harv.AcceptResource(resource);
			return true;
		}

		void FindMoreResource(Actor self)
		{
			var res = self.World.WorldActor.traits.Get<ResourceLayer>();
			var harv = self.Info.Traits.Get<HarvesterInfo>();

			self.QueueActivity(new Move(
				() =>
				{
					var search = new PathSearch(self.World)
					{
						heuristic = loc => (res.GetResource(loc) != null 
							&& harv.Resources.Contains( res.GetResource(loc).Name )) ? 0 : 1,
						umt = UnitMovementType.Wheel,
						checkForBlocked = true
					};
					search.AddInitialCell(self.World, self.Location);
					return self.World.PathFinder.FindPath(search);
				}));
			self.QueueActivity(new Harvest());
		}

		public void Cancel(Actor self) { }
	}
}
