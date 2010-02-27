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

namespace OpenRA.Mods.RA
{
	class MinelayerInfo : StatelessTraitInfo<Minelayer>
	{
		public readonly string Mine = "minv";
	}

	class Minelayer : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return null;

			// Ensure that the cell is empty except for the minelayer
			if (self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(xy).Any(a => a != self))
				return null;

			if (mi.Button == MouseButton.Right && underCursor == self)
				return new Order("Deploy", self);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Deploy")
			{
				var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
				if (limitedAmmo != null)
					limitedAmmo.Attacking(self);

				self.QueueActivity( new LayMine() );
			}
		}
	}
}
