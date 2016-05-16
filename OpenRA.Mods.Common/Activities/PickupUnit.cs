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
		readonly Aircraft aircraftTrait;
		readonly Carryable carryableTrait;
		readonly Carryall carryallTrait;
		readonly IFacing carryableFacing;
		readonly IFacing carryallFacing;
		readonly int delay;

		enum State { Intercept, LockCarryable, MoveToCarryable, Turn, Land, Pickup, Aborted }

		State state;
		bool soundPlayed;
		Activity innerActivity;

		public PickupUnit(Actor self, Actor cargo, int delay)
		{
			this.cargo = cargo;
			carryableTrait = cargo.Trait<Carryable>();
			carryableFacing = cargo.Trait<IFacing>();
			movement = self.Trait<IMove>();
			aircraftTrait = self.Trait<Aircraft>();
			carryallTrait = self.Trait<Carryall>();
			carryallFacing = self.Trait<IFacing>();
			state = State.Intercept;
			this.delay = delay;
			soundPlayed = false;
		}

		public override Activity Tick(Actor self)
		{
			if (cargo != carryallTrait.Carryable)
				return NextActivity;

			if (cargo.IsDead || IsCanceled)
			{
				carryallTrait.UnreserveCarryable();
				return NextActivity;
			}

			if (carryallTrait.State == Carryall.CarryallState.Idle)
				return NextActivity;

			if (innerActivity != null)
			{
				innerActivity = ActivityUtils.RunActivity(self, innerActivity);
				return this;
			}

			switch (state)
			{
				case State.Intercept:
					innerActivity = movement.MoveWithinRange(Target.FromActor(cargo), WDist.FromCells(4));
					ChangeState(State.LockCarryable);
					return this;
				case State.LockCarryable:
					// Last check
					if (!carryableTrait.LockForPickup(self))
						return ChangeState(State.Aborted);
					return ChangeState(State.MoveToCarryable);
				case State.MoveToCarryable: // We arrived, move on top
					if (self.Location != cargo.Location)
					{
						innerActivity = movement.MoveTo(cargo.Location, 0);
						return this;
					}

					return ChangeState(State.Turn);
				case State.Turn: // Align facing
					if (carryallFacing.Facing != carryableFacing.Facing)
					{
						innerActivity = new Turn(self, carryableFacing.Facing);
						return this;
					}

					return ChangeState(State.Land);
				case State.Land: // Land on the carryable
					if (self.Location != cargo.Location || carryallFacing.Facing != carryableFacing.Facing)
						return ChangeState(State.MoveToCarryable);

					if (HeliFly.AdjustAltitude(self, aircraftTrait, aircraftTrait.Info.LandAltitude + carryableTrait.CarryableHeight))
					{
						PlayLandingSound();
						return this;
					}

					return ChangeState(State.Pickup);
				case State.Pickup: // Wait and pick up
					if (delay > 0)
						return ActivityUtils.SequenceActivities(new Wait(delay, false), this);

					// Remove our carryable from world
					Attach(self);
					return NextActivity;
				case State.Aborted:
					// We got cancelled
					carryallTrait.UnreserveCarryable();
					break;
			}

			return NextActivity;
		}

		void PlayLandingSound()
		{
			if (!soundPlayed)
			{
				// TODO: this should be done within HeliLand once it supports a different landing height
				Game.Sound.Play(aircraftTrait.Info.LandingSound);
				soundPlayed = true;
			}
		}

		void Attach(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				cargo.World.Remove(cargo);
				carryableTrait.Attached();
				carryallTrait.AttachCarryable(cargo);
			});
		}

		Activity ChangeState(State state)
		{
			soundPlayed = false;
			this.state = state;
			return this;
		}
	}
}
