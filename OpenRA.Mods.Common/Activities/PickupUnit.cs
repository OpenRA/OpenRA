#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
		readonly Carryall carryall;
		readonly Carryable carryable;
		readonly IFacing carryableFacing;
		readonly BodyOrientation carryableBody;

		readonly int delay;
		readonly Color? targetLineColor;

		// TODO: Expose this to yaml
		readonly WDist targetLockRange = WDist.FromCells(4);

		enum PickupState { Intercept, LockCarryable, Pickup }
		PickupState state = PickupState.Intercept;

		public PickupUnit(Actor self, Actor cargo, int delay, Color? targetLineColor)
		{
			this.cargo = cargo;
			this.delay = delay;
			this.targetLineColor = targetLineColor;
			carryable = cargo.Trait<Carryable>();
			carryableFacing = cargo.Trait<IFacing>();
			carryableBody = cargo.Trait<BodyOrientation>();

			carryall = self.Trait<Carryall>();

			ChildHasPriority = false;
		}

		protected override void OnFirstRun(Actor self)
		{
			// The cargo might have become invalid while we were moving towards it.
			if (cargo.IsDead || carryable.IsTraitDisabled || !cargo.AppearsFriendlyTo(self))
				return;

			if (carryall.ReserveCarryable(self, cargo))
			{
				// Fly to the target and wait for it to be locked for pickup
				// These activities will be cancelled and replaced by Land once the target has been locked
				QueueChild(new Fly(self, Target.FromActor(cargo)));
				QueueChild(new FlyIdle(self, idleTurn: false));
			}
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			if (cargo.IsDead || carryable.IsTraitDisabled || !cargo.AppearsFriendlyTo(self) || cargo != carryall.Carryable)
			{
				Cancel(self, true);
				return false;
			}

			// Wait until we are near the target before we try to lock it
			if (state == PickupState.Intercept && (cargo.CenterPosition - self.CenterPosition).HorizontalLengthSquared <= targetLockRange.LengthSquared)
				state = PickupState.LockCarryable;

			if (state == PickupState.LockCarryable)
			{
				var lockResponse = carryable.LockForPickup(cargo, self);
				if (lockResponse == LockResponse.Failed)
				{
					Cancel(self, true);
					return false;
				}
				else if (lockResponse == LockResponse.Success)
				{
					// Pickup position and facing are now known - swap the fly/wait activity with Land
					ChildActivity.Cancel(self);

					var localOffset = carryall.OffsetForCarryable(self, cargo).Rotate(carryableBody.QuantizeOrientation(cargo.Orientation));
					QueueChild(new Land(self, Target.FromActor(cargo), -carryableBody.LocalToWorld(localOffset), carryableFacing.Facing));

					// Pause briefly before attachment for visual effect
					if (delay > 0)
						QueueChild(new Wait(delay, false));

					// Remove our carryable from world
					QueueChild(new AttachUnit(self, cargo));
					QueueChild(new TakeOff(self));

					state = PickupState.Pickup;
				}
			}

			// Return once we are in the pickup state and the pickup activities have completed.
			return TickChild(self) && state == PickupState.Pickup;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			base.Cancel(self, keepQueue);

			// We are safe to bail here as base won't set IsCanceling to true if not interruptible.
			if (!IsInterruptible)
				return;

			// This nulls caryall storage, so to avoid deleting units make sure it is not called while carrying one.
			if (carryall.State == Carryall.CarryallState.Reserved)
				carryall.UnreserveCarryable(self);

			// TakeOff is not interruptible, but this activity is. To deal with it we bail. We transfer
			// priority both to dispose of this activity and to make sure TakeOff is not disposed with it.
			if (ChildActivity is TakeOff)
			{
				ChildHasPriority = true;
				return;
			}

			// Make sure we run the TakeOff activity if we are / have landed.
			if (self.Trait<Aircraft>().HasInfluence())
			{
				ChildHasPriority = true;
				QueueChild(new TakeOff(self));
			}
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
				yield return new TargetLineNode(Target.FromActor(cargo), targetLineColor.Value);
		}

		sealed class AttachUnit : Activity
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
