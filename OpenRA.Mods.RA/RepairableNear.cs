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
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class RepairableNearInfo : TraitInfo<RepairableNear>
	{
		[ActorReference]
		public readonly string[] Buildings = { "spen", "syrd" };
	}

	class RepairableNear : IIssueOrder, IResolveOrder, IProvideCursor
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) return null;
			if (underCursor == null) return null;

			if (underCursor.Owner == self.Owner &&
				self.Info.Traits.Get<RepairableNearInfo>().Buildings.Contains( underCursor.Info.Name ) &&
				self.Health < self.GetMaxHP())
				return new Order("Enter", self, underCursor);

			return null;
		}

		public string CursorForOrderString(string s, Actor a, int2 location)
		{
			return (s == "Enter") ? "enter" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Enter")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor, 1));
				self.QueueActivity(new Repair(order.TargetActor));
			}
		}
	}
}
