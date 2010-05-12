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
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	class HelicopterInfo : ITraitInfo
	{
		public readonly string[] RepairBuildings = { "fix" };
		public readonly string[] RearmBuildings = { "hpad" };
		public readonly int CruiseAltitude = 20;
		public readonly int IdealSeparation = 80;
		public object Create(Actor self) { return new Helicopter(self); }
	}

	class Helicopter : IIssueOrder, IResolveOrder, IMovement
	{
		public IDisposable reservation;
		public Helicopter(Actor self) {}

		static bool HeliCanEnter(Actor self, Actor a)
		{
			if (self.Info.Traits.Get<HelicopterInfo>().RearmBuildings.Contains(a.Info.Name)) return true;
			if (self.Info.Traits.Get<HelicopterInfo>().RepairBuildings.Contains(a.Info.Name)) return true;
			return false;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor == null)
			{
				if (self.traits.GetOrDefault<IMovement>().CanEnterCell(xy))
					return new Order("Move", self, xy);
			}

			if (HeliCanEnter(self, underCursor)
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
				self.QueueActivity(new HeliFly(Util.CenterOfCell(order.TargetLocation)));
				self.QueueActivity(new Turn(self.Info.Traits.GetOrDefault<UnitInfo>().InitialFacing));
				self.QueueActivity(new HeliLand(true));	
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;
				var res = order.TargetActor.traits.GetOrDefault<Reservable>();
				if (res != null)
					reservation = res.Reserve(self);

				var productionInfo = order.TargetActor.Info.Traits.GetOrDefault<ProductionInfo>();
				var offset = productionInfo != null ? productionInfo.SpawnOffset : null;
				var offsetVec = offset != null ? new float2(offset[0], offset[1]) : float2.Zero;

				self.CancelActivity();
				self.QueueActivity(new HeliFly(order.TargetActor.CenterLocation + offsetVec));
				self.QueueActivity(new Turn(self.Info.Traits.GetOrDefault<UnitInfo>().InitialFacing));
				self.QueueActivity(new HeliLand(false));
				self.QueueActivity(self.Info.Traits.Get<HelicopterInfo>().RearmBuildings.Contains(order.TargetActor.Info.Name)
					? (IActivity)new Rearm() : new Repair());
			}
		}

		public UnitMovementType GetMovementType() { return UnitMovementType.Fly; }
		public bool CanEnterCell(int2 location) { return true; }
	}
}
