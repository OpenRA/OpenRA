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
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class DeliverResources : IActivity
	{
		public IActivity NextActivity { get; set; }

		bool isDocking;
		Actor refinery;

		public DeliverResources() { }

		public DeliverResources( Actor refinery )
		{
			this.refinery = refinery;
		}

		Actor ChooseRefinery(Actor self)
		{
			var mobile = self.traits.Get<Mobile>();

			var search = new PathSearch(self.World)
			{
				heuristic = PathSearch.DefaultEstimator(self.Location),
				umt = mobile.GetMovementType(),
				checkForBlocked = false,
			};
			var refineries = self.World.Queries.OwnedBy[self.Owner]
				.Where(x => x.traits.Contains<IAcceptOre>())
				.ToList();
			if (refinery != null)
				search.AddInitialCell(self.World, refinery.Location + refinery.traits.Get<IAcceptOre>().DeliverOffset);
			else
				foreach (var r in refineries)
					search.AddInitialCell(self.World, r.Location + r.traits.Get<IAcceptOre>().DeliverOffset);

			var path = self.World.PathFinder.FindPath(search);
			path.Reverse();
			if (path.Count != 0)
				return refineries.FirstOrDefault(x => x.Location + x.traits.Get<IAcceptOre>().DeliverOffset == path[0]);
			else
				return null;
		}

		public IActivity Tick( Actor self )
		{
			var mobile = self.traits.Get<Mobile>();

			if( NextActivity != null )
				return NextActivity;

			if( refinery != null && refinery.IsDead )
				refinery = null;

			if( refinery == null || self.Location != refinery.Location + refinery.traits.Get<IAcceptOre>().DeliverOffset )
			{
				refinery = ChooseRefinery(self);
				if (refinery == null)
					return new Wait(10) { NextActivity = this };

				return new Move(refinery.Location + refinery.traits.Get<IAcceptOre>().DeliverOffset, 0) { NextActivity = this };
			}
			else if (!isDocking)
			{
				isDocking = true;
				refinery.traits.Get<IAcceptOre>().OnDock(self, this);
			}
			
			return new Wait(10) { NextActivity = this };
		}

		public void Cancel(Actor self)
		{
			// TODO: allow canceling of deliver orders?
		}
	}
}
