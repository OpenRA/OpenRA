#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;
using OpenRA.Traits.Activities;
using System.Drawing;

namespace OpenRA.Mods.RA
{
	class HelicopterInfo : AircraftInfo
	{
		public readonly float InstabilityMagnitude = 2.0f;
		public readonly int InstabilityTicks = 5;	
		public readonly int IdealSeparation = 40;
		public readonly bool LandWhenIdle = true;

		public override object Create( ActorInitializer init ) { return new Helicopter( init, this); }
	}

	class Helicopter : Aircraft, ITick, IIssueOrder, IResolveOrder, IOrderCursor, IOrderVoice
	{
		public IDisposable reservation;
		HelicopterInfo Info;

		public Helicopter( ActorInitializer init, HelicopterInfo info) : base( init, info ) 
		{
			Info = info;
		}

		public Order IssueOrder(Actor self, int2 xy, MouseInput mi, Actor underCursor)
		{
			if (mi.Button == MouseButton.Left) return null;

			if (underCursor == null)
				if (self.traits.GetOrDefault<IMove>().CanEnterCell(xy))
					return new Order("Move", self, xy);

			if (AircraftCanEnter(self, underCursor)
				&& underCursor.Owner == self.Owner)
				return new Order("Enter", self, underCursor);
			
			return null;
		}
		
		public string CursorForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move") return "move";
			if (order.OrderString == "Enter")
				return Reservable.IsReserved(order.TargetActor) ? "enter-blocked" : "enter";
			
			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Move" || order.OrderString == "Enter") ? "Move" : null;
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
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						w.Add(new MoveFlash(self.World, order.TargetLocation));
						var line = self.traits.GetOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Green);
					});
				
				self.CancelActivity();
				self.QueueActivity(new HeliFly(Util.CenterOfCell(order.TargetLocation)));
				
				if (Info.LandWhenIdle)
				{
					self.QueueActivity(new Turn(Info.InitialFacing));
					self.QueueActivity(new HeliLand(true));
				}
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;
				var res = order.TargetActor.traits.GetOrDefault<Reservable>();
				if (res != null)
					reservation = res.Reserve(self);

				var pi = order.TargetActor.traits.Get<Production>();
				var offset = pi != null ? pi.Spawns.First().Key : float2.Zero;
				
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.traits.GetOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Green);
					});
				
				self.CancelActivity();
				self.QueueActivity(new HeliFly(order.TargetActor.CenterLocation + offset));
				self.QueueActivity(new Turn(Info.InitialFacing));
				self.QueueActivity(new HeliLand(false));
				self.QueueActivity(Info.RearmBuildings.Contains(order.TargetActor.Info.Name)
					? (IActivity)new Rearm() : new Repair(order.TargetActor));
			}
		}
		
		int offsetTicks = 0;
		public void Tick(Actor self)
		{
			var aircraft = self.traits.Get<Aircraft>();
			if (aircraft.Altitude <= 0)
				return;
			
			var rawSpeed = .2f * aircraft.MovementSpeedForCell(self, self.Location);
			var otherHelis = self.World.FindUnitsInCircle(self.CenterLocation, Info.IdealSeparation)
				.Where(a => a.traits.Contains<Helicopter>());

			var f = otherHelis
				.Select(h => self.traits.Get<Helicopter>().GetRepulseForce(self, h))
				.Aggregate(float2.Zero, (a, b) => a + b);

			self.CenterLocation += rawSpeed * f;

			if (--offsetTicks <= 0)
			{
				self.CenterLocation += Info.InstabilityMagnitude * self.World.SharedRandom.Gauss2D(5);
				aircraft.Altitude += (int)(Info.InstabilityMagnitude * self.World.SharedRandom.Gauss1D(5));
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
			
			if (d.Length > Info.IdealSeparation)
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
