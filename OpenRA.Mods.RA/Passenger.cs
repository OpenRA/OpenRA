#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA
{
	class PassengerInfo : TraitInfo<Passenger>
	{
		public readonly PipType ColorOfCargoPip = PipType.Green;
	}

	class Passenger : IIssueOrder, IResolveOrder, IOrderCursor
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

			// Todo: Use something better for cargo management
			//var umt = self.traits.Get<IMove>().GetMovementType();
			//if (!underCursor.Info.Traits.Get<CargoInfo>().PassengerTypes.Contains(umt))
				return null;

			//return new Order("EnterTransport", self, underCursor);
		}
		
		public string CursorForOrder(Actor self, Order order)
		{
			return (order.OrderString == "EnterTransport") ? "enter" : null;
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

		public PipType ColorOfCargoPip( Actor self )
		{
			return self.Info.Traits.Get<PassengerInfo>().ColorOfCargoPip;
		}
	}
}
