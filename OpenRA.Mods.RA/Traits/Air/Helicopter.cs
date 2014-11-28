#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class HelicopterInfo : AircraftInfo, IMoveInfo
	{
		[Desc("Allow the helicopter land after it has no more commands.")]
		public readonly bool LandWhenIdle = true;

		[Desc("Allow the helicopter turn before landing.")]
		public readonly bool TurnToLand = false;
		public readonly WRange LandAltitude = WRange.Zero;

		[Desc("How fast the helicopter ascends or descends.")]
		public readonly WRange AltitudeVelocity = new WRange(43);

		public override object Create(ActorInitializer init) { return new Helicopter(init, this); }
	}

	class Helicopter : Aircraft, ITick, IResolveOrder, IMove
	{
		public HelicopterInfo Info;
		Actor self;
		bool firstTick = true;
		public bool IsMoving { get { return self.CenterPosition.Z > 0; } set { } }

		public Helicopter(ActorInitializer init, HelicopterInfo info)
			: base(init, info)
		{
			self = init.self;
			Info = info;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (Reservation != null)
			{
				Reservation.Dispose();
				Reservation = null;
			}

			if (order.ID == OrderCode.Move)
			{
				var cell = self.World.Map.Clamp(order.TargetLocation);
				var explored = self.Owner.Shroud.IsExplored(cell);

				if (!explored && !Info.MoveIntoShroud)
					return;

				var target = Target.FromCell(self.World, cell);

				self.SetTargetLine(target, Color.Green);
				self.CancelActivity();
				self.QueueActivity(new HeliFly(self, target));

				if (Info.LandWhenIdle)
				{
					if (Info.TurnToLand)
						self.QueueActivity(new Turn(self, Info.InitialFacing));

					self.QueueActivity(new HeliLand(true));
				}
			}

			if (order.ID == OrderCode.Enter)
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
					var offset = (exit != null) ? exit.SpawnOffset : WVec.Zero;

					self.SetTargetLine(Target.FromActor(order.TargetActor), Color.Green);

					self.CancelActivity();
					self.QueueActivity(new HeliFly(self, Target.FromPos(order.TargetActor.CenterPosition + offset)));
					self.QueueActivity(new Turn(self, Info.InitialFacing));
					self.QueueActivity(new HeliLand(false));
					self.QueueActivity(new ResupplyAircraft());
					self.QueueActivity(new TakeOff());
				}
			}

			if (order.ID == OrderCode.ReturnToBase)
			{
				self.CancelActivity();
				self.QueueActivity(new HeliReturn());
			}

			if (order.ID == OrderCode.Stop)
			{
				self.CancelActivity();

				if (Info.LandWhenIdle)
				{
					if (Info.TurnToLand)
						self.QueueActivity(new Turn(self, Info.InitialFacing));

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

				var host = GetActorBelow();
				if (host == null)
					return;

				self.QueueActivity(new TakeOff());
			}

			Repulse();
		}

		public Activity MoveTo(CPos cell, int nearEnough) { return new HeliFly(self, Target.FromCell(self.World, cell)); }
		public Activity MoveTo(CPos cell, Actor ignoredActor) { return new HeliFly(self, Target.FromCell(self.World, cell)); }
		public Activity MoveWithinRange(Target target, WRange range) { return new HeliFly(self, target, WRange.Zero, range); }
		public Activity MoveWithinRange(Target target, WRange minRange, WRange maxRange) { return new HeliFly(self, target, minRange, maxRange); }
		public Activity MoveFollow(Actor self, Target target, WRange minRange, WRange maxRange) { return new Follow(self, target, minRange, maxRange); }
		public CPos NearestMoveableCell(CPos cell) { return cell; }

		public Activity MoveIntoWorld(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			return new HeliFly(self, Target.FromCell(self.World, cell, subCell));
		}

		public Activity MoveIntoTarget(Actor self, Target target) { return new HeliLand(false); }
		public Activity MoveToTarget(Actor self, Target target)
		{
			return Util.SequenceActivities(new HeliFly(self, target), new Turn(self, Info.InitialFacing));
		}

		public Activity VisualMove(Actor self, WPos fromPos, WPos toPos)
		{
			// TODO: Ignore repulsion when moving
			return Util.SequenceActivities(new CallFunc(() => SetVisualPosition(self, fromPos)), new HeliFly(self, Target.FromPos(toPos)));
		}

		public override IEnumerable<Activity> GetResupplyActivities(Actor a)
		{
			foreach (var b in base.GetResupplyActivities(a))
				yield return b;
		}
	}
}
