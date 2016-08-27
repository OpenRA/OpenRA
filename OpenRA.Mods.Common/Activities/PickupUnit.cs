#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	public class PickupUnit : Activity
	{
		readonly Actor cargo;
		readonly IMove movement;

		readonly Carryall carryall;
		readonly IFacing carryallFacing;

		readonly Carryable carryable;
		readonly IFacing carryableFacing;
		readonly BodyOrientation carryableBody;

		readonly int delay;

		enum State { Intercept, LockCarryable, MoveToCarryable, Turn, Land, Wait, Pickup, Aborted }

		State state;
		Activity innerActivity;

		public PickupUnit(Actor self, Actor cargo, int delay)
		{
			this.cargo = cargo;
			this.delay = delay;
			carryable = cargo.Trait<Carryable>();
			carryableFacing = cargo.Trait<IFacing>();
			carryableBody = cargo.Trait<BodyOrientation>();

			movement = self.Trait<IMove>();
			carryall = self.Trait<Carryall>();
			carryallFacing = self.Trait<IFacing>();

			state = State.Intercept;
		}

		public override Activity Tick(Actor self)
		{
			if (innerActivity != null)
			{
				innerActivity = ActivityUtils.RunActivity(self, innerActivity);
				return this;
			}

			if (cargo != carryall.Carryable)
				return NextActivity;

			if (cargo.IsDead || IsCanceled)
			{
				carryall.UnreserveCarryable(self);
				return NextActivity;
			}

			if (carryall.State == Carryall.CarryallState.Idle)
				return NextActivity;

			switch (state)
			{
				case State.Intercept:
					innerActivity = movement.MoveWithinRange(Target.FromActor(cargo), WDist.FromCells(4));
					state = State.LockCarryable;
					return this;

				case State.LockCarryable:
					state = State.MoveToCarryable;
					if (!carryable.LockForPickup(cargo, self))
						state = State.Aborted;
					return this;

				case State.MoveToCarryable:
				{
					// Line up with the attachment point
					var localOffset = carryall.OffsetForCarryable(self, cargo).Rotate(carryableBody.QuantizeOrientation(self, cargo.Orientation));
					var targetPosition = cargo.CenterPosition - carryableBody.LocalToWorld(localOffset);
					if ((self.CenterPosition - targetPosition).HorizontalLengthSquared != 0)
					{
						// Run the first tick of the move activity immediately to avoid a one-frame pause
						innerActivity = ActivityUtils.RunActivity(self, new HeliFly(self, Target.FromPos(targetPosition)));
						return this;
					}

					state = State.Turn;
					return this;
				}

				case State.Turn:
					if (carryallFacing.Facing != carryableFacing.Facing)
					{
						innerActivity = new Turn(self, carryableFacing.Facing);
						return this;
					}

					state = State.Land;
					return this;

				case State.Land:
				{
					var localOffset = carryall.OffsetForCarryable(self, cargo).Rotate(carryableBody.QuantizeOrientation(self, cargo.Orientation));
					var targetPosition = cargo.CenterPosition - carryableBody.LocalToWorld(localOffset);
					if ((self.CenterPosition - targetPosition).HorizontalLengthSquared != 0 || carryallFacing.Facing != carryableFacing.Facing)
					{
						state = State.MoveToCarryable;
						return this;
					}

					if (targetPosition.Z != self.CenterPosition.Z)
					{
						innerActivity = new HeliLand(self, false, self.World.Map.DistanceAboveTerrain(targetPosition));
						return this;
					}

					state = delay > 0 ? State.Wait : State.Pickup;
					return this;
				}

				case State.Wait:
					state = State.Pickup;
					innerActivity = new Wait(delay, false);
					return this;

				case State.Pickup:
					// Remove our carryable from world
					Attach(self);
					return NextActivity;

				case State.Aborted:
					// We got cancelled
					carryall.UnreserveCarryable(self);
					break;
			}

			return NextActivity;
		}

		void Attach(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				cargo.World.Remove(cargo);
				carryable.Attached(cargo);
				carryall.AttachCarryable(self, cargo);
			});
		}

		public override void Cancel(Actor self)
		{
			if (innerActivity != null)
				innerActivity.Cancel(self);

			base.Cancel(self);
		}
	}
}
