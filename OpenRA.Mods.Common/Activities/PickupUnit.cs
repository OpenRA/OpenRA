#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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

		enum PickupState { Intercept, LockCarryable, MoveToCarryable, Turn, Land, Wait, Pickup, Aborted }

		PickupState state;
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

			state = PickupState.Intercept;
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

			if (cargo.IsDead || IsCanceled || carryable.IsTraitDisabled || !cargo.AppearsFriendlyTo(self))
			{
				carryall.UnreserveCarryable(self);
				return NextActivity;
			}

			if (carryall.State == Carryall.CarryallState.Idle)
				return NextActivity;

			switch (state)
			{
				case PickupState.Intercept:
					innerActivity = movement.MoveWithinRange(Target.FromActor(cargo), WDist.FromCells(4));
					state = PickupState.LockCarryable;
					return this;

				case PickupState.LockCarryable:
					state = PickupState.MoveToCarryable;
					if (!carryable.LockForPickup(cargo, self))
						state = PickupState.Aborted;
					return this;

				case PickupState.MoveToCarryable:
				{
					// Line up with the attachment point
					var localOffset = carryall.OffsetForCarryable(self, cargo).Rotate(carryableBody.QuantizeOrientation(self, cargo.Orientation));
					var targetPosition = cargo.CenterPosition - carryableBody.LocalToWorld(localOffset);
					if ((self.CenterPosition - targetPosition).HasNonZeroHorizontalLength)
					{
						// Run the first tick of the move activity immediately to avoid a one-frame pause
						innerActivity = ActivityUtils.RunActivity(self, new HeliFly(self, Target.FromPos(targetPosition)));
						return this;
					}

					state = PickupState.Turn;
					return this;
				}

				case PickupState.Turn:
					if (carryallFacing.Facing != carryableFacing.Facing)
					{
						innerActivity = new Turn(self, carryableFacing.Facing);
						return this;
					}

					state = PickupState.Land;
					return this;

				case PickupState.Land:
				{
					var localOffset = carryall.OffsetForCarryable(self, cargo).Rotate(carryableBody.QuantizeOrientation(self, cargo.Orientation));
					var targetPosition = cargo.CenterPosition - carryableBody.LocalToWorld(localOffset);
					if ((self.CenterPosition - targetPosition).HasNonZeroHorizontalLength || carryallFacing.Facing != carryableFacing.Facing)
					{
						state = PickupState.MoveToCarryable;
						return this;
					}

					if (targetPosition.Z != self.CenterPosition.Z)
					{
						innerActivity = new HeliLand(self, false, self.World.Map.DistanceAboveTerrain(targetPosition));
						return this;
					}

					state = delay > 0 ? PickupState.Wait : PickupState.Pickup;
					return this;
				}

				case PickupState.Wait:
					state = PickupState.Pickup;
					innerActivity = new Wait(delay, false);
					return this;

				case PickupState.Pickup:
					// Remove our carryable from world
					Attach(self);
					return NextActivity;

				case PickupState.Aborted:
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

		public override bool Cancel(Actor self, bool keepQueue = false)
		{
			if (!IsCanceled && innerActivity != null && !innerActivity.Cancel(self))
				return false;

			return base.Cancel(self, keepQueue);
		}
	}
}
