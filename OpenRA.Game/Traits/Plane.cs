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
using OpenRA.Traits.Activities;

namespace OpenRA.Traits
{
	class PlaneInfo : ITraitInfo
	{
		public readonly int CruiseAltitude = 20;
		public readonly string[] RearmBuildings = { "afld" };
		public readonly string[] RepairBuildings = { "fix" };

		public object Create(Actor self) { return new Plane(self); }
	}

	class Plane : IIssueOrder, IResolveOrder, IMovement
	{
		public IDisposable reservation;

		public Plane(Actor self) {}

		static bool PlaneCanEnter(Actor self, Actor a)
		{
			var plane = self.Info.Traits.Get<PlaneInfo>();
			return plane.RearmBuildings.Contains(a.Info.Name) 
				|| plane.RepairBuildings.Contains(a.Info.Name);
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;
			
			if (underCursor == null)
				return new Order("Move", self, xy);

			if (PlaneCanEnter(self, underCursor)
				&& underCursor.Owner == self.Owner
				&& !Reservable.IsReserved(underCursor))
				return new Order("Enter", self, underCursor);

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (reservation != null)
			{
				reservation.Dispose();
				reservation = null;
			}

			if (order.OrderString == "Move")
			{
				self.CancelActivity();
				self.QueueActivity(new Fly(Util.CenterOfCell(order.TargetLocation)));
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;
				var res = order.TargetActor.traits.GetOrDefault<Reservable>();
				if (res != null)
					reservation = res.Reserve(self);

				var info = self.Info.Traits.Get<PlaneInfo>();

				self.CancelActivity();
				self.QueueActivity(new ReturnToBase(self, order.TargetActor));
				self.QueueActivity(
					info.RearmBuildings.Contains(order.TargetActor.Info.Name)
						? (IActivity)new Rearm() : new Repair(true));
			}
		}

		public UnitMovementType GetMovementType() { return UnitMovementType.Fly; }
		public bool CanEnterCell(int2 location) { return true; }
	}
}
