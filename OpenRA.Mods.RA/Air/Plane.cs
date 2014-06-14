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
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class PlaneInfo : AircraftInfo, IMoveInfo
	{
		public readonly WAngle MaximumPitch = WAngle.FromDegrees(10);

		public override object Create(ActorInitializer init) { return new Plane(init, this); }
	}

	public class Plane : Aircraft, IResolveOrder, IMove, ITick, ISync
	{
		public readonly PlaneInfo Info;
		[Sync] public WPos RTBPathHash;
		Actor self;
		public bool IsMoving { get { return self.CenterPosition.Z > 0; } set { } }

		public Plane(ActorInitializer init, PlaneInfo info)
			: base(init, info)
		{
			self = init.self;
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

				var host = GetActorBelow();
				if (host == null)
					return;

				self.QueueActivity(new TakeOff());
			}
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Move")
			{
				UnReserve();

				var cell = self.World.ClampToWorld(order.TargetLocation);
				var t = Target.FromCell(cell);
				self.SetTargetLine(t, Color.Green);
				self.CancelActivity();
				self.QueueActivity(new Fly(self, t));
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
				self.QueueActivity(new TakeOff());
			}
			else
			{
				// Game.Debug("Unreserve due to unhandled order: {0}".F(order.OrderString));
				UnReserve();
			}
		}

		public Activity MoveTo(CPos cell, int nearEnough) { return Util.SequenceActivities(new Fly(self, Target.FromCell(cell)), new FlyCircle()); }
		public Activity MoveTo(CPos cell, Actor ignoredActor) { return Util.SequenceActivities(new Fly(self, Target.FromCell(cell)), new FlyCircle()); }
		public Activity MoveWithinRange(Target target, WRange range) { return Util.SequenceActivities(new Fly(self, target, WRange.Zero, range), new FlyCircle()); }
		public Activity MoveWithinRange(Target target, WRange minRange, WRange maxRange) { return Util.SequenceActivities(new Fly(self, target, minRange, maxRange), new FlyCircle()); }
		public Activity MoveFollow(Actor self, Target target, WRange range) { return new FlyFollow(self, target, range); }
		public CPos NearestMoveableCell(CPos cell) { return cell; }

		public Activity MoveIntoWorld(Actor self, CPos cell) { return new Fly(self, Target.FromCell(cell)); }
		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos) { return Util.SequenceActivities(new CallFunc(() => SetVisualPosition(self, fromPos)), new Fly(self, Target.FromPos(toPos))); }
	}
}
