#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	class HelicopterInfo : AircraftInfo
	{
		public readonly int IdealSeparation = 40;
		public readonly bool LandWhenIdle = true;

		public override object Create( ActorInitializer init ) { return new Helicopter( init, this); }
	}

	class Helicopter : Aircraft, ITick, IResolveOrder
	{
		HelicopterInfo Info;
		bool firstTick = true;

		public Helicopter( ActorInitializer init, HelicopterInfo info) : base( init, info )
		{
			Info = info;
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
				var target = self.World.ClampToWorld(order.TargetLocation);

				self.SetTargetLine(Target.FromCell(target), Color.Green);
				self.CancelActivity();
				self.QueueActivity(new HeliFly(Util.CenterOfCell(target)));

				if (Info.LandWhenIdle)
				{
					self.QueueActivity(new Turn(Info.InitialFacing));
					self.QueueActivity(new HeliLand(true));
				}
			}

			if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor))
				{
					self.CancelActivity();
					self.QueueActivity(new HeliReturn());
				}
				else
				{
					var res = order.TargetActor.TraitOrDefault<Reservable>();
					if (res != null)
						reservation = res.Reserve(order.TargetActor, self, this);

					var exit = order.TargetActor.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
					var offset = exit != null ? exit.SpawnOffset : int2.Zero;

					self.SetTargetLine(Target.FromActor(order.TargetActor), Color.Green);

					self.CancelActivity();
					self.QueueActivity(new HeliFly(order.TargetActor.Trait<IHasLocation>().PxPosition + offset));
					self.QueueActivity(new Turn(Info.InitialFacing));
					self.QueueActivity(new HeliLand(false));
					self.QueueActivity(new ResupplyAircraft());
				}
			}

			if (order.OrderString == "ReturnToBase")
			{
				self.CancelActivity();
				self.QueueActivity( new HeliReturn() );
			}

			if (order.OrderString == "Stop")
			{
				self.CancelActivity();

				if (Info.LandWhenIdle)
				{
					self.QueueActivity(new Turn(Info.InitialFacing));
					self.QueueActivity(new HeliLand(true));
				}
			}
		}

		public void Tick(Actor self)
		{
			if (firstTick)
			{
				firstTick = false;
				ReserveSpawnBuilding();
			}

			/* repulsion only applies when we're flying */
			if (Altitude <= 0) return;

			var otherHelis = self.World.FindUnitsInCircle(self.CenterLocation, Info.IdealSeparation)
				.Where(a => a.HasTrait<Helicopter>());

			var f = otherHelis
				.Select(h => GetRepulseForce(self, h))
				.Aggregate(int2.Zero, (a, b) => a + b);

			int RepulsionFacing = Util.GetFacing( f, -1 );
			if( RepulsionFacing != -1 )
				TickMove( 1024 * MovementSpeed, RepulsionFacing );
		}

		// Returns an int2 in subPx units
		public int2 GetRepulseForce(Actor self, Actor h)
		{
			if (self == h)
				return int2.Zero;
			if( h.Trait<Helicopter>().Altitude < Altitude )
				return int2.Zero;
			var d = self.CenterLocation - h.CenterLocation;

			if (d.Length > Info.IdealSeparation)
				return int2.Zero;

			if (d.LengthSquared < 1)
				return Util.SubPxVector[self.World.SharedRandom.Next(255)];
			return (5120 / d.LengthSquared) * d;
		}
	}
}
