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
using OpenRA.Traits.Activities;

namespace OpenRA.Traits
{
	class PassengerInfo : StatelessTraitInfo<Passenger> {}

	class Passenger : IIssueOrder, IResolveOrder
	{
		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button != MouseButton.Right) 
				return null;

			if (underCursor == null || underCursor.Owner != self.Owner)
				return null;

			var cargo = underCursor.traits.GetOrDefault<Cargo>();
			if (cargo == null || cargo.IsFull(underCursor))
				return null;

			var umt = self.traits.Get<IMovement>().GetMovementType();
			if (!underCursor.Info.Traits.Get<CargoInfo>().PassengerTypes.Contains(umt))
				return null;

			return new Order("EnterTransport", self, underCursor);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "EnterTransport")
			{
				self.CancelActivity();
				self.QueueActivity(new Move(order.TargetActor.Location, 1));
				self.QueueActivity(new EnterTransport(self, order.TargetActor));
			}
		}
	}
}
