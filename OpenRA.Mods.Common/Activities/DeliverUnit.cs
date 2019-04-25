#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class DeliverUnit : Activity
	{
		readonly Actor self;
		readonly Carryall carryall;
		readonly BodyOrientation body;
		Target destination;

		public DeliverUnit(Actor self, CPos destination)
			: this(self, Target.FromCell(self.World, destination)) { }

		public DeliverUnit(Actor self)
			: this(self, Target.Invalid) { }

		DeliverUnit(Actor self, Target destination)
		{
			this.self = self;
			this.destination = destination;

			carryall = self.Trait<Carryall>();
			body = self.Trait<BodyOrientation>();
		}

		Target FindDropLocation(Target requested, WDist maxSearchDistance)
		{
			var positionable = carryall.Carryable.Trait<IPositionable>();
			var centerPosition = requested.CenterPosition;
			var targetCell = self.World.Map.CellContaining(centerPosition);

			// The easy case
			if (positionable.CanEnterCell(targetCell, self))
				return requested;

			var cellRange = (maxSearchDistance.Length + 1023) / 1024;
			foreach (var c in self.World.Map.FindTilesInCircle(targetCell, cellRange))
			{
				if (!positionable.CanEnterCell(c, self))
					continue;

				var delta = self.World.Map.CenterOfCell(c) - centerPosition;
				if (delta.LengthSquared < maxSearchDistance.LengthSquared)
					return Target.FromCell(self.World, c);
			}

			return Target.Invalid;
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (IsCanceling || carryall.State != Carryall.CarryallState.Carrying || carryall.Carryable.IsDead)
				return NextActivity;

			// Drop the actor at the current position
			if (destination.Type == TargetType.Invalid)
				destination = Target.FromCell(self.World, self.Location);

			var target = FindDropLocation(destination, carryall.Info.DropRange);

			// Can't land, so wait at the target until something changes
			if (target.Type == TargetType.Invalid)
			{
				QueueChild(self, new HeliFly(self, destination), true);
				QueueChild(self, new Wait(25));
				return this;
			}

			// Move to drop-off location
			var localOffset = carryall.CarryableOffset.Rotate(body.QuantizeOrientation(self, self.Orientation));
			var carryablePosition = self.CenterPosition + body.LocalToWorld(localOffset);
			if ((carryablePosition - target.CenterPosition).HorizontalLengthSquared != 0)
			{
				// For non-zero offsets the drop position depends on the carryall facing
				// We therefore need to predict/correct for the facing *at the drop point*
				if (carryall.CarryableOffset.HorizontalLengthSquared != 0)
				{
					var dropFacing = (target.CenterPosition - self.CenterPosition).Yaw.Facing;
					localOffset = carryall.CarryableOffset.Rotate(body.QuantizeOrientation(self, WRot.FromFacing(dropFacing)));
					QueueChild(self, new HeliFly(self, Target.FromPos(target.CenterPosition - body.LocalToWorld(localOffset))), true);
					QueueChild(self, new Turn(self, dropFacing));
					return this;
				}

				QueueChild(self, new HeliFly(self, target), true);
				return this;
			}

			// Make sure that the carried actor is on the ground before releasing it
			if (self.World.Map.DistanceAboveTerrain(carryablePosition) != WDist.Zero)
				QueueChild(self, new Land(self), true);

			// Pause briefly before releasing for visual effect
			if (carryall.Info.UnloadingDelay > 0)
				QueueChild(self, new Wait(carryall.Info.UnloadingDelay, false), true);

			// Release carried actor
			QueueChild(self, new ReleaseUnit(self));
			QueueChild(self, new HeliFly(self, Target.FromPos(self.CenterPosition)));
			return this;
		}

		class ReleaseUnit : Activity
		{
			readonly Carryall carryall;
			readonly BodyOrientation body;
			readonly IFacing facing;

			public ReleaseUnit(Actor self)
			{
				facing = self.Trait<IFacing>();
				carryall = self.Trait<Carryall>();
				body = self.Trait<BodyOrientation>();
			}

			protected override void OnFirstRun(Actor self)
			{
				self.Trait<Aircraft>().RemoveInfluence();

				var localOffset = carryall.CarryableOffset.Rotate(body.QuantizeOrientation(self, self.Orientation));
				var targetPosition = self.CenterPosition + body.LocalToWorld(localOffset);
				var targetLocation = self.World.Map.CellContaining(targetPosition);
				carryall.Carryable.Trait<IPositionable>().SetPosition(carryall.Carryable, targetLocation, SubCell.FullCell);

				// HACK: directly manipulate the turret facings to match the new orientation
				// This can eventually go away, when we make turret facings relative to the body
				var carryableFacing = carryall.Carryable.Trait<IFacing>();
				var facingDelta = facing.Facing - carryableFacing.Facing;
				foreach (var t in carryall.Carryable.TraitsImplementing<Turreted>())
					t.TurretFacing += facingDelta;

				carryableFacing.Facing = facing.Facing;

				// Put back into world
				self.World.AddFrameEndTask(w =>
				{
					var cargo = carryall.Carryable;
					var carryable = carryall.Carryable.Trait<Carryable>();
					w.Add(cargo);
					carryall.DetachCarryable(self);
					carryable.UnReserve(cargo);
					carryable.Detached(cargo);
				});
			}

			public override Activity Tick(Actor self)
			{
				return NextActivity;
			}
		}
	}
}
