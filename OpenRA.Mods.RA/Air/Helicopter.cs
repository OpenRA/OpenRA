#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
		public readonly WRange IdealSeparation = new WRange(1706);
		public readonly bool LandWhenIdle = true;
		public readonly WRange LandAltitude = WRange.Zero;
		public readonly WRange AltitudeVelocity = new WRange(43);

		public override object Create(ActorInitializer init) { return new Helicopter(init, this); }
	}

	class Helicopter : Aircraft, ITick, IResolveOrder, IMove
	{
		public HelicopterInfo Info;
		bool firstTick = true;

		public Helicopter(ActorInitializer init, HelicopterInfo info)
			: base(init, info)
		{
			Info = info;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (Reservation != null)
			{
				Reservation.Dispose();
				Reservation = null;
			}

			if (order.OrderString == "Move")
			{
				var target = self.World.ClampToWorld(order.TargetLocation);

				self.SetTargetLine(Target.FromCell(target), Color.Green);
				self.CancelActivity();
				self.QueueActivity(new HeliFly(target));

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
						Reservation = res.Reserve(order.TargetActor, self, this);

					var exit = order.TargetActor.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
					var offset = (exit != null) ? exit.SpawnOffsetVector : WVec.Zero;

					self.SetTargetLine(Target.FromActor(order.TargetActor), Color.Green);

					self.CancelActivity();
					self.QueueActivity(new HeliFly(order.TargetActor.CenterPosition + offset));
					self.QueueActivity(new Turn(Info.InitialFacing));
					self.QueueActivity(new HeliLand(false));
					self.QueueActivity(new ResupplyAircraft());
				}
			}

			if (order.OrderString == "ReturnToBase")
			{
				self.CancelActivity();
				self.QueueActivity(new HeliReturn());
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
				if (!self.HasTrait<FallsToEarth>()) // TODO: Aircraft husks don't properly unreserve.
					ReserveSpawnBuilding();
			}

			// Repulsion only applies when we're flying!
			var altitude = CenterPosition.Z;
			var cruiseAltitude = Info.CruiseAltitude * 1024 / Game.CellSize;
			if (altitude != cruiseAltitude)
				return;

			var otherHelis = self.World.FindActorsInCircle(self.CenterPosition, Info.IdealSeparation)
				.Where(a => a.HasTrait<Helicopter>());

			var f = otherHelis
				.Select(h => GetRepulseForce(self, h))
				.Aggregate(WVec.Zero, (a, b) => a + b);

			int repulsionFacing = Util.GetFacing(f, -1);
			if (repulsionFacing != -1)
				SetPosition(self, CenterPosition + FlyStep(repulsionFacing));
		}

		public WVec GetRepulseForce(Actor self, Actor other)
		{
			if (self == other || other.CenterPosition.Z < self.CenterPosition.Z)
				return WVec.Zero;

			var d = self.CenterPosition - other.CenterPosition;
			var distSq = d.HorizontalLengthSquared;
			if (distSq > Info.IdealSeparation.Range * Info.IdealSeparation.Range)
				return WVec.Zero;

			if (distSq < 1)
			{
				var yaw = self.World.SharedRandom.Next(0, 1023);
				var rot = new WRot(WAngle.Zero, WAngle.Zero, new WAngle(yaw));
				return new WVec(1024, 0, 0).Rotate(rot);
			}

			return (d * 1024 * 8) / (int)distSq;
		}

		public Activity MoveTo(CPos cell, int nearEnough) { return new HeliFly(cell); }
		public Activity MoveTo(CPos cell, Actor ignoredActor) { return new HeliFly(cell); }
		public Activity MoveWithinRange(Target target, WRange range) { return new HeliFly(target.CenterPosition); }
	}
}
