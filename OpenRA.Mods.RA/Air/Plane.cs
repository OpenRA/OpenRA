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
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class PlaneInfo : AircraftInfo
	{
		public readonly WAngle MaximumPitch = WAngle.FromDegrees(10);

		public override object Create(ActorInitializer init) { return new Plane(init, this); }
	}

	public class Plane : Aircraft, IResolveOrder, IMove, ITick, ISync
	{
		public readonly PlaneInfo Info;
		[Sync] public WPos RTBPathHash;

		public Plane(ActorInitializer init, PlaneInfo info)
			: base(init, info)
		{
			Info = info;
		}

		bool firstTick = true;
		public void Tick(Actor self)
		{
			if (firstTick)
			{
				firstTick = false;
				if (!self.HasTrait<FallsToEarth>()) // TODO: Aircraft husks don't properly unreserve.
					ReserveSpawnBuilding();
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				UnReserve();

				var target = self.World.ClampToWorld(order.TargetLocation);
				self.SetTargetLine(Target.FromCell(target), Color.Green);
				self.CancelActivity();
				self.QueueActivity(Fly.ToCell(target));
				self.QueueActivity(new FlyCircle());
			}
			else if (order.OrderString == "Enter")
			{
				if (Reservable.IsReserved(order.TargetActor)) return;

				UnReserve();

				self.SetTargetLine(Target.FromOrder(order), Color.Green);

				self.CancelActivity();
				self.QueueActivity(new ReturnToBase(self, order.TargetActor));
				self.QueueActivity(new ResupplyAircraft());
			}
			else if (order.OrderString == "Stop")
			{
				UnReserve();
				self.CancelActivity();
			}
			else if (order.OrderString == "ReturnToBase")
			{
				var airfield = ReturnToBase.ChooseAirfield(self, true);
				if (airfield == null) return;

				UnReserve();
				self.CancelActivity();
				self.SetTargetLine(Target.FromActor(airfield), Color.Green);
				self.QueueActivity(new ReturnToBase(self, airfield));
				self.QueueActivity(new ResupplyAircraft());
			}
			else
			{
				// Game.Debug("Unreserve due to unhandled order: {0}".F(order.OrderString));
				UnReserve();
			}
		}

		public Activity MoveTo(CPos cell, int nearEnough) { return Fly.ToCell(cell); }
		public Activity MoveTo(CPos cell, Actor ignoredActor) { return Fly.ToCell(cell); }
		public Activity MoveWithinRange(Target target, WRange range) { return Fly.ToPos(target.CenterPosition); }
	}
}
