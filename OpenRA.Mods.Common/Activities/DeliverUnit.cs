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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class DeliverUnit : Activity
	{
		readonly Actor self;
		readonly IMove movement;
		readonly Carryable carryableTrait;
		readonly Carryall carryallTrait;
		readonly Aircraft aircraftTrait;
		readonly IPositionable positionable;
		readonly IFacing carryableFacing;
		readonly IFacing carryallFacing;
		readonly CPos location;
		readonly int delay;

		enum State { Transport, Land, Release, TakeOff, Aborted }

		State state;
		bool soundPlayed;

		public DeliverUnit(Actor self, int delay, CPos? location = null)
		{
			carryallTrait = self.Trait<Carryall>();
			this.self = self;
			var carryable = carryallTrait.Carryable;
			movement = self.Trait<IMove>();
			carryableTrait = carryable.Trait<Carryable>();
			aircraftTrait = self.Trait<Aircraft>();
			positionable = carryable.Trait<IPositionable>();
			carryableFacing = carryable.Trait<IFacing>();
			carryallFacing = self.Trait<IFacing>();
			state = State.Transport;
			this.delay = delay;
			this.location = location ?? carryableTrait.Destination;
			soundPlayed = false;
		}

		// Find a suitable location to drop our carryable
		CPos GetLocationToDrop(CPos requestedPosition)
		{
			if (positionable.CanEnterCell(requestedPosition))
				return requestedPosition;

			var candidateCells = Util.AdjacentCells(self.World, Target.FromCell(self.World, requestedPosition));

			// TODO: This will behave badly if there is no suitable drop point nearby
			do
			{
				foreach (var c in candidateCells)
					if (positionable.CanEnterCell(c))
						return c;

				// Expanding dropable cells search area
				// TODO: This also includes all of the cells we have just checked
				candidateCells = Util.ExpandFootprint(candidateCells, true);
			} while (true);
		}

		// Check if we can drop the unit at our current location.
		bool CanDropHere()
		{
			return positionable.CanEnterCell(self.Location);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if ((carryallTrait.State == Carryall.CarryallState.Idle || carryallTrait.Carryable.IsDead) && state != State.TakeOff)
				state = State.Aborted;

			switch (state)
			{
				case State.Transport:
					var targetl = GetLocationToDrop(location);
					state = State.Land;
					if (self.Location == targetl)
						return this;
					else
					{
						var altitude = self.World.Map.DistanceAboveTerrain(self.CenterPosition);
						if ((altitude - carryableTrait.CarryableHeight).Length < aircraftTrait.Info.MinAirborneAltitude)
							Game.Sound.Play(aircraftTrait.Info.TakeoffSound);
						return ActivityUtils.SequenceActivities(movement.MoveTo(targetl, 0), this);
					}

				case State.Land:
					if (!CanDropHere())
						return ChangeState(State.Transport);

					if (HeliFly.AdjustAltitude(self, aircraftTrait, aircraftTrait.Info.LandAltitude + carryableTrait.CarryableHeight))
					{
						PlayLandingSound();
						return this;
					}

					state = State.Release;
					if (delay > 0)
						return ActivityUtils.SequenceActivities(new Wait(delay), this);
					return this;

				case State.Release:
					if (!CanDropHere())
						return ChangeState(State.Transport);

					Release();
					return ChangeState(State.TakeOff);

				case State.TakeOff:
					if (HeliFly.AdjustAltitude(self, aircraftTrait, aircraftTrait.Info.CruiseAltitude))
					{
						PlayTakeoffSound();
						return this;
					}

					return NextActivity;

				case State.Aborted:
					carryallTrait.UnreserveCarryable();
					break;
			}

			return NextActivity;
		}

		void PlayLandingSound()
		{
			if (!soundPlayed)
			{
				soundPlayed = true;
				Game.Sound.Play(aircraftTrait.Info.LandingSound);
			}
		}

		void PlayTakeoffSound()
		{
			if (!soundPlayed)
			{
				soundPlayed = true;
				Game.Sound.Play(aircraftTrait.Info.TakeoffSound);
			}
		}

		void Release()
		{
			positionable.SetPosition(carryallTrait.Carryable, self.Location, SubCell.FullCell);
			carryableFacing.Facing = carryallFacing.Facing;

			// Put back into world
			self.World.AddFrameEndTask(w =>
			{
				carryallTrait.Carryable.World.Add(carryallTrait.Carryable);
				carryallTrait.DetachCarryable();
				carryableTrait.UnReserve();
				carryableTrait.Detached();
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
