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

using System.Collections.Generic;
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

		public override bool Tick(Actor self)
		{
			if (cargo != carryall.Carryable)
				return true;

			if (IsCanceling)
			{
				if (carryall.State == Carryall.CarryallState.Reserved)
					carryall.UnreserveCarryable(self);

				return true;
			}

			if (cargo.IsDead || carryable.IsTraitDisabled || !cargo.AppearsFriendlyTo(self))
			{
				carryall.UnreserveCarryable(self);
				return true;
			}

			if (carryall.State != Carryall.CarryallState.Reserved)
				return true;

			switch (state)
			{
				case PickupState.Intercept:
					QueueChild(movement.MoveWithinRange(Target.FromActor(cargo), WDist.FromCells(4)));
					state = PickupState.LockCarryable;
					return false;

				case PickupState.LockCarryable:
					if (!carryable.LockForPickup(cargo, self))
						Cancel(self);

					state = PickupState.Pickup;
					return false;

				case PickupState.Pickup:
				{
					// Land at the target location
					var localOffset = carryall.OffsetForCarryable(self, cargo).Rotate(carryableBody.QuantizeOrientation(self, cargo.Orientation));
					QueueChild(new Land(self, Target.FromActor(cargo), -carryableBody.LocalToWorld(localOffset), carryableFacing.Facing));

					// Pause briefly before attachment for visual effect
					if (delay > 0)
						QueueChild(new Wait(delay, false));

					// Remove our carryable from world
					QueueChild(new AttachUnit(self, cargo));
					QueueChild(new TakeOff(self));
					return false;
				}
			}

			return true;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(cargo), Color.Yellow);
		}

		class AttachUnit : Activity
		{
			readonly Actor cargo;
			readonly Carryable carryable;
			readonly Carryall carryall;

			public AttachUnit(Actor self, Actor cargo)
			{
				this.cargo = cargo;
				carryable = cargo.Trait<Carryable>();
				carryall = self.Trait<Carryall>();
			}

			protected override void OnFirstRun(Actor self)
			{
				// The cargo might have become invalid while we were moving towards it.
				if (cargo == null || cargo.IsDead || carryable.IsTraitDisabled || !cargo.AppearsFriendlyTo(self))
					return;

				self.World.AddFrameEndTask(w =>
				{
					cargo.World.Remove(cargo);
					carryable.Attached(cargo);
					carryall.AttachCarryable(self, cargo);
				});
			}
		}
	}
}
