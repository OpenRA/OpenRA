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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits.Activities;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA
{
	class HelicopterInfo : AircraftInfo
	{
		public readonly float InstabilityMagnitude = 2.0f;
		public readonly int InstabilityTicks = 5;	
		public readonly int IdealSeparation = 40;
		public readonly bool LandWhenIdle = true;

		public override object Create( ActorInitializer init ) { return new Helicopter( init ); }
	}

	class Helicopter : Aircraft, ITick, IIssueOrder, IResolveOrder, IProvideCursor
	{
		public IDisposable reservation;

		public Helicopter( ActorInitializer init ) : base( init ) { }

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor == null)
			{
				if (self.traits.GetOrDefault<IMove>().CanEnterCell(xy))
					return new Order("Move", self, xy);
			}

			if (AircraftCanEnter(self, underCursor)
				&& underCursor.Owner == self.Owner
				&& !Reservable.IsReserved(underCursor))
				return new Order("Enter", self, underCursor);

			return null;
		}
		
		public string CursorForOrderString(string s, Actor a, int2 location)
		{
			return (s == "Enter") ? "enter" : null;
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
				
				if (self.Info.Traits.Get<HelicopterInfo>().LandWhenIdle)
				{
					self.QueueActivity(new Turn(self.Info.Traits.GetOrDefault<UnitInfo>().InitialFacing));
					self.QueueActivity(new HeliLand(true));
				}
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
					? (IActivity)new Rearm() : new Repair(order.TargetActor));
			}
		}
		
		int offsetTicks = 0;
		public void Tick(Actor self)
		{
			var unit = self.traits.Get<Unit>();
			if (unit.Altitude <= 0)
				return;
			
			var Info = self.Info.Traits.Get<HelicopterInfo>();
			var mobile = self.traits.WithInterface<IMove>().FirstOrDefault();
			var rawSpeed = .2f * mobile.MovementSpeedForCell(self, self.Location);
			var otherHelis = self.World.FindUnitsInCircle(self.CenterLocation, Info.IdealSeparation)
				.Where(a => a.traits.Contains<Helicopter>());

			var f = otherHelis
				.Select(h => self.traits.Get<Helicopter>().GetRepulseForce(self, h))
				.Aggregate(float2.Zero, (a, b) => a + b);

			self.CenterLocation += rawSpeed * f;

			if (--offsetTicks <= 0)
			{
				self.CenterLocation += Info.InstabilityMagnitude * self.World.SharedRandom.Gauss2D(5);
				unit.Altitude += (int)(Info.InstabilityMagnitude * self.World.SharedRandom.Gauss1D(5));
				offsetTicks = Info.InstabilityTicks;
			}

			Location = Util.CellContaining(self.CenterLocation);
		}
			
		const float Epsilon = .5f;
		public float2 GetRepulseForce(Actor self, Actor h)
		{
			if (self == h)
				return float2.Zero;
			var d = self.CenterLocation - h.CenterLocation;
			
			if (d.Length > self.Info.Traits.Get<HelicopterInfo>().IdealSeparation)
				return float2.Zero;

			if (d.LengthSquared < Epsilon)
				return float2.FromAngle((float)self.World.SharedRandom.NextDouble() * 3.14f);
			return (5 / d.LengthSquared) * d;
		}
		
		public override IEnumerable<float2> GetCurrentPath(Actor self)
		{
			var move = self.GetCurrentActivity() as Activities.HeliFly;
			if (move == null) return new float2[] { };
			
			return new float2[] { move.Dest };
		}
	}
}
