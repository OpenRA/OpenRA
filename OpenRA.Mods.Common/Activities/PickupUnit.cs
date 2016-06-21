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
		readonly Carryable carryable;
		readonly Carryall carryall;
		readonly Aircraft aircraft;
		readonly IFacing cargoFacing;
		readonly IFacing selfFacing;

		enum State { Intercept, LockCarryable, MoveToCarryable, Turn, Pickup, TakeOff }

		State state;

		public PickupUnit(Actor self, Actor cargo)
		{
			this.cargo = cargo;
			carryable = cargo.Trait<Carryable>();
			cargoFacing = cargo.Trait<IFacing>();
			movement = self.Trait<IMove>();
			carryall = self.Trait<Carryall>();
			aircraft = self.Trait<Aircraft>();
			selfFacing = self.Trait<IFacing>();
			state = State.Intercept;
		}

		public override Activity Tick(Actor self)
		{
			if (cargo.IsDead || !carryall.IsBusy)
			{
				carryall.UnreserveCarryable();
				return NextActivity;
			}

			switch (state)
			{
				case State.Intercept:
					state = State.LockCarryable;
					return ActivityUtils.SequenceActivities(movement.MoveWithinRange(Target.FromActor(cargo), WDist.FromCells(4)), this);

				case State.LockCarryable:
					// Last check
					if (carryable.StandbyForPickup(self))
					{
						state = State.MoveToCarryable;
						return this;
					}

					// We got cancelled
					carryall.UnreserveCarryable();
					return NextActivity;

				case State.MoveToCarryable: // We arrived, move on top
					if (self.Location == cargo.Location)
					{
						state = State.Turn;
						return this;
					}

					return ActivityUtils.SequenceActivities(movement.MoveTo(cargo.Location, 0), this);

				case State.Turn: // Align facing and Land
					if (selfFacing.Facing != cargoFacing.Facing)
						return ActivityUtils.SequenceActivities(new Turn(self, cargoFacing.Facing), this);
					state = State.Pickup;
					return ActivityUtils.SequenceActivities(new HeliLand(self, false), new Wait(10), this);

				case State.Pickup:
					// Remove our carryable from world
					self.World.AddFrameEndTask(w => cargo.World.Remove(cargo));
					carryall.AttachCarryable(cargo);
					state = State.TakeOff;
					return this;
				case State.TakeOff:
					if (HeliFly.AdjustAltitude(self, aircraft, aircraft.Info.CruiseAltitude))
						return this;
					return NextActivity;
			}

			return NextActivity;
		}

		public override void Cancel(Actor self)
		{
			// TODO: Drop the unit at the nearest available cell
		}
	}
}
