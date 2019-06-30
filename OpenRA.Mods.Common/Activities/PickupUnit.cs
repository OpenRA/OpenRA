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
using OpenRA.Primitives;
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

		enum PickupState { Intercept, LockCarryable, Pickup }

		PickupState state;

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

		protected override void OnFirstRun(Actor self)
		{
			carryall.ReserveCarryable(self, cargo);
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (cargo != carryall.Carryable)
				return NextActivity;

			if (IsCanceling)
			{
				if (carryall.State == Carryall.CarryallState.Reserved)
					carryall.UnreserveCarryable(self);

				return NextActivity;
			}

			if (cargo.IsDead || carryable.IsTraitDisabled || !cargo.AppearsFriendlyTo(self))
			{
				carryall.UnreserveCarryable(self);
				return NextActivity;
			}

			if (carryall.State != Carryall.CarryallState.Reserved)
				return NextActivity;

			switch (state)
			{
				case PickupState.Intercept:
					QueueChild(self, movement.MoveWithinRange(Target.FromActor(cargo), WDist.FromCells(4), targetLineColor: Color.Yellow), true);
					state = PickupState.LockCarryable;
					return this;

				case PickupState.LockCarryable:
					if (!carryable.LockForPickup(cargo, self))
						Cancel(self);

					state = PickupState.Pickup;
					return this;

				case PickupState.Pickup:
				{
					// Land at the target location
					var localOffset = carryall.OffsetForCarryable(self, cargo).Rotate(carryableBody.QuantizeOrientation(self, cargo.Orientation));
					QueueChild(self, new Land(self, Target.FromActor(cargo), -carryableBody.LocalToWorld(localOffset), carryableFacing.Facing), true);

					// Pause briefly before attachment for visual effect
					if (delay > 0)
						QueueChild(self, new Wait(delay, false));

					// Remove our carryable from world
					QueueChild(self, new CallFunc(() => Attach(self)));
					QueueChild(self, new TakeOff(self));
					return this;
				}
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
	}
}
