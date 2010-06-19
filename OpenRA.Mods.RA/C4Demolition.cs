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

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class C4DemolitionInfo : TraitInfo<C4Demolition>
	{
		public readonly float C4Delay = 0;
	}

	class C4Demolition : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;
			if (underCursor.Owner == self.Owner && !mi.Modifiers.HasModifier(Modifiers.Ctrl)) return null;
			if (!underCursor.traits.Contains<Building>()) return null;

			return new Order("C4", self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "C4")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 2));
				self.QueueActivity(new Demolish(order.TargetActor));
				self.QueueActivity(new Move(self.Location, 0));
			}
		}
	}
}
