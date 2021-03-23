#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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

		// TODO: Expose this to yaml
		readonly WDist targetLockRange = WDist.FromCells(4);

		enum PickupState { Intercept, LockCarryable, Pickup }
		PickupState state = PickupState.Intercept;

		public PickupUnit(Actor self, Actor cargo, int delay)
		{
			this.cargo = cargo;
			this.delay = delay;
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

			// Wait until we are near the target before we try to lock it
			var distSq = (cargo.CenterPosition - self.CenterPosition).HorizontalLengthSquared;
			if (state == PickupState.Intercept && distSq <= targetLockRange.LengthSquared)
				state = PickupState.LockCarryable;

			if (state == PickupState.LockCarryable)
			{
				var lockResponse = carryable.LockForPickup(cargo, self);
				if (lockResponse == LockResponse.Failed)
					Cancel(self);
				else if (lockResponse == LockResponse.Success)
				{
					// Pickup position and facing are now known - swap the fly/wait activity with Land
					ChildActivity.Cancel(self);

					var localOffset = carryall.OffsetForCarryable(self, cargo).Rotate(carryableBody.QuantizeOrientation(self, cargo.Orientation));
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

			// Return once we are in the pickup state and the pickup activities have completed
			return TickChild(self) && state == PickupState.Pickup;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(cargo), carryall.Info.TargetLineColor);
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
