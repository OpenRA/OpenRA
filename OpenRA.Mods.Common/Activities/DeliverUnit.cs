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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class DeliverUnit : Activity
	{
		readonly Actor self;
		readonly Carryable carryable;
		readonly Carryall carryall;
		readonly IPositionable positionable;
		readonly BodyOrientation body;
		readonly IFacing carryableFacing;
		readonly IFacing carryallFacing;
		readonly CPos destination;

		enum DeliveryState { Transport, Land, Wait, Release, TakeOff, Aborted }

		DeliveryState state;
		Activity innerActivity;

		public DeliverUnit(Actor self, CPos destination)
		{
			this.self = self;
			this.destination = destination;

			carryallFacing = self.Trait<IFacing>();
			carryall = self.Trait<Carryall>();
			body = self.Trait<BodyOrientation>();

			carryable = carryall.Carryable.Trait<Carryable>();
			positionable = carryall.Carryable.Trait<IPositionable>();
			carryableFacing = carryall.Carryable.Trait<IFacing>();
			state = DeliveryState.Transport;
		}

		CPos? FindDropLocation(CPos targetCell, WDist maxSearchDistance)
		{
			// The easy case
			if (positionable.CanEnterCell(targetCell))
				return targetCell;

			var cellRange = (maxSearchDistance.Length + 1023) / 1024;
			var centerPosition = self.World.Map.CenterOfCell(targetCell);
			foreach (var c in self.World.Map.FindTilesInCircle(targetCell, cellRange))
			{
				if (!positionable.CanEnterCell(c))
					continue;

				var delta = self.World.Map.CenterOfCell(c) - centerPosition;
				if (delta.LengthSquared < maxSearchDistance.LengthSquared)
					return c;
			}

			return null;
		}

		// Check if we can drop the unit at our current location.
		bool CanDropHere()
		{
			var localOffset = carryall.CarryableOffset.Rotate(body.QuantizeOrientation(self, self.Orientation));
			var targetCell = self.World.Map.CellContaining(self.CenterPosition + body.LocalToWorld(localOffset));
			return positionable.CanEnterCell(targetCell);
		}

		public override Activity Tick(Actor self)
		{
			if (innerActivity != null)
			{
				innerActivity = ActivityUtils.RunActivity(self, innerActivity);
				return this;
			}

			if (IsCanceled)
				return NextActivity;

			if ((carryall.State == Carryall.CarryallState.Idle || carryall.Carryable.IsDead) && state != DeliveryState.TakeOff)
				state = DeliveryState.Aborted;

			switch (state)
			{
				case DeliveryState.Transport:
				{
					var targetLocation = FindDropLocation(destination, carryall.Info.DropRange);

					// Can't land, so wait at the target until something changes
					if (!targetLocation.HasValue)
					{
						innerActivity = ActivityUtils.SequenceActivities(
							new HeliFly(self, Target.FromCell(self.World, destination)),
							new Wait(25));

						return this;
					}

					var targetPosition = self.World.Map.CenterOfCell(targetLocation.Value);

					var localOffset = carryall.CarryableOffset.Rotate(body.QuantizeOrientation(self, self.Orientation));
					var carryablePosition = self.CenterPosition + body.LocalToWorld(localOffset);
					if ((carryablePosition - targetPosition).HasNonZeroHorizontalLength)
					{
						// For non-zero offsets the drop position depends on the carryall facing
						// We therefore need to predict/correct for the facing *at the drop point*
						if (carryall.CarryableOffset.HasNonZeroHorizontalLength)
						{
							var facing = (targetPosition - self.CenterPosition).Yaw.Facing;
							localOffset = carryall.CarryableOffset.Rotate(body.QuantizeOrientation(self, WRot.FromFacing(facing)));
							innerActivity = ActivityUtils.SequenceActivities(
								new HeliFly(self, Target.FromPos(targetPosition - body.LocalToWorld(localOffset))),
								new Turn(self, facing));

							return this;
						}

						innerActivity = new HeliFly(self, Target.FromPos(targetPosition));
						return this;
					}

					state = DeliveryState.Land;
					return this;
				}

				case DeliveryState.Land:
				{
					if (!CanDropHere())
					{
						state = DeliveryState.Transport;
						return this;
					}

					// Make sure that the carried actor is on the ground before releasing it
					var localOffset = carryall.CarryableOffset.Rotate(body.QuantizeOrientation(self, self.Orientation));
					var carryablePosition = self.CenterPosition + body.LocalToWorld(localOffset);
					if (self.World.Map.DistanceAboveTerrain(carryablePosition) != WDist.Zero)
					{
						innerActivity = new HeliLand(self, false, -new WDist(carryall.CarryableOffset.Z));
						return this;
					}

					state = carryall.Info.UnloadingDelay > 0 ? DeliveryState.Wait : DeliveryState.Release;
					return this;
				}

				case DeliveryState.Wait:
					state = DeliveryState.Release;
					innerActivity = new Wait(carryall.Info.UnloadingDelay, false);
					return this;

				case DeliveryState.Release:
					if (!CanDropHere())
					{
						state = DeliveryState.Transport;
						return this;
					}

					Release();
					state = DeliveryState.TakeOff;
					return this;

				case DeliveryState.TakeOff:
					return ActivityUtils.SequenceActivities(new HeliFly(self, Target.FromPos(self.CenterPosition)), NextActivity);

				case DeliveryState.Aborted:
					carryall.UnreserveCarryable(self);
					break;
			}

			return NextActivity;
		}

		void Release()
		{
			var localOffset = carryall.CarryableOffset.Rotate(body.QuantizeOrientation(self, self.Orientation));
			var targetPosition = self.CenterPosition + body.LocalToWorld(localOffset);
			var targetLocation = self.World.Map.CellContaining(targetPosition);
			positionable.SetPosition(carryall.Carryable, targetLocation, SubCell.FullCell);

			// HACK: directly manipulate the turret facings to match the new orientation
			// This can eventually go away, when we make turret facings relative to the body
			var facingDelta = carryallFacing.Facing - carryableFacing.Facing;
			foreach (var t in carryall.Carryable.TraitsImplementing<Turreted>())
				t.TurretFacing += facingDelta;

			carryableFacing.Facing = carryallFacing.Facing;

			// Put back into world
			self.World.AddFrameEndTask(w =>
			{
				var cargo = carryall.Carryable;
				w.Add(cargo);
				carryall.DetachCarryable(self);
				carryable.UnReserve(cargo);
				carryable.Detached(cargo);
			});
		}

		public override bool Cancel(Actor self, bool keepQueue = false)
		{
			if (!IsCanceled && innerActivity != null && !innerActivity.Cancel(self))
				return false;

			return base.Cancel(self);
		}
	}
}
