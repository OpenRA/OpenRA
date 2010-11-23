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
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	class HelicopterInfo : AircraftInfo
	{
		public readonly int InstabilityMagnitude = 1024;
		public readonly int InstabilityTicks = 5;	
		public readonly int IdealSeparation = 40;
		public readonly bool LandWhenIdle = true;

		public override object Create( ActorInitializer init ) { return new Helicopter( init, this); }
	}

	class Helicopter : Aircraft, ITick, IIssueOrder, IResolveOrder, IOrderVoice
	{
		public IDisposable reservation;
		HelicopterInfo Info;

		public Helicopter( ActorInitializer init, HelicopterInfo info) : base( init, info ) 
		{
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterOrderTargeter<Building>( "Enter", 5, false, true,
					target => AircraftCanEnter( target ), target => !Reservable.IsReserved( target ) );

				yield return new AircraftMoveOrderTargeter();
			}
		}

		public Order IssueOrder( Actor self, IOrderTargeter order, Target target, bool queued )
		{
			if( order.OrderID == "Enter" )
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			if( order.OrderID == "Move" )
				return new Order(order.OrderID, self, queued) { TargetLocation = Util.CellContaining(target.CenterLocation) };

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
						if (self.Destroyed) return;
						w.Add(new MoveFlash(self.World, order.TargetLocation));
						var line = self.TraitOrDefault<DrawLineToTarget>();
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
				var res = order.TargetActor.TraitOrDefault<Reservable>();
				if (res != null)
					reservation = res.Reserve(self);

				var exit = order.TargetActor.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
				var offset = exit != null ? exit.SpawnOffset : int2.Zero;
				
				if (self.Owner == self.World.LocalPlayer)
					self.World.AddFrameEndTask(w =>
					{
						if (self.Destroyed) return;
						w.Add(new FlashTarget(order.TargetActor));
						var line = self.TraitOrDefault<DrawLineToTarget>();
						if (line != null)
							line.SetTarget(self, Target.FromOrder(order), Color.Green);
					});
				
				self.CancelActivity();
				self.QueueActivity(new HeliFly(order.TargetActor.Trait<IHasLocation>().PxPosition + offset));
				self.QueueActivity(new Turn(Info.InitialFacing));
				self.QueueActivity(new HeliLand(false));
				self.QueueActivity(Info.RearmBuildings.Contains(order.TargetActor.Info.Name)
					? (IActivity)new Rearm() : new Repair(order.TargetActor));
			}
		}
		
		int offsetTicks = 0;
		public void Tick(Actor self)
		{
			var aircraft = self.Trait<Aircraft>();
			if (aircraft.Altitude <= 0)
				return;
			
			var otherHelis = self.World.FindUnitsInCircle(self.CenterLocation, Info.IdealSeparation)
				.Where(a => a.HasTrait<Helicopter>());

			var f = otherHelis
				.Select(h => self.Trait<Helicopter>().GetRepulseForce(self, h, h.Trait<Helicopter>()))
				.Aggregate(float2.Zero, (a, b) => a + b);

			RepulsionFacing = Util.GetFacing( f, -1 );
			if( RepulsionFacing != -1 )
				aircraft.TickMove( 1024 * aircraft.MovementSpeed, RepulsionFacing );

			if (--offsetTicks <= 0)
			{
				InstabilityFacing = Util.GetFacing( self.World.SharedRandom.Gauss2D( 5 ), -1 );
				if( InstabilityFacing != -1 )
					aircraft.TickMove( Info.InstabilityMagnitude, InstabilityFacing );

				aircraft.Altitude += (int)(Info.InstabilityMagnitude / 1024 * self.World.SharedRandom.Gauss1D(5));
				offsetTicks = Info.InstabilityTicks;
			}
		}

		[Sync]
		int RepulsionFacing;
		[Sync]
		int InstabilityFacing;

		const float Epsilon = .5f;
		public float2 GetRepulseForce(Actor self, Actor h, Aircraft hAir)
		{
			if (self == h)
				return float2.Zero;
			if( hAir.Altitude < Altitude )
				return float2.Zero;
			var d = self.CenterLocation - h.CenterLocation;
			
			if (d.Length > Info.IdealSeparation)
				return float2.Zero;

			if (d.LengthSquared < Epsilon)
				return float2.FromAngle((float)self.World.SharedRandom.NextDouble() * 3.14f);
			return (5 / d.LengthSquared) * d;
		}
	}
}
