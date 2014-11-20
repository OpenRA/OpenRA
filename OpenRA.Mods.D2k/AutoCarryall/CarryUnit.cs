#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion
using System;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	public class CarryUnit : Activity
	{
		readonly Actor self;
		readonly Actor carryable;
		readonly IMove movement;
		readonly Carryable c;
		readonly AutoCarryall aca;
		readonly Helicopter helicopter;
		readonly IPositionable positionable;
		readonly IFacing cFacing; // Carryable facing
		readonly IFacing sFacing; // Self facing

		enum State { Intercept, LockCarryable, MoveToCarryable, Turn, Pickup, Transport, Land, Release, Takeoff, Done }

		State state;

		public CarryUnit(Actor self, Actor carryable)
		{
			this.self = self;
			this.carryable = carryable;
			movement = self.Trait<IMove>();
			c = carryable.Trait<Carryable>();
			aca = self.Trait<AutoCarryall>();
			helicopter = self.Trait<Helicopter>();
			positionable = carryable.Trait<IPositionable>();
			cFacing = carryable.Trait<IFacing>();
			sFacing = self.Trait<IFacing>();

			state = State.Intercept;
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
			if (carryable.IsDead)
			{
				aca.UnreserveCarryable();
				return NextActivity;
			}

			switch (state)
			{
				case State.Intercept: // Move towards our carryable

					state = State.LockCarryable;
					return Util.SequenceActivities(movement.MoveWithinRange(Target.FromActor(carryable), WRange.FromCells(4)), this);

				case State.LockCarryable:
					// Last check
					if (c.StandbyForPickup(self))
					{
						state = State.MoveToCarryable;
						return this;
					} 
					else
					{
						// We got cancelled
						aca.UnreserveCarryable();
						return NextActivity;
					}

				case State.MoveToCarryable: // We arrived, move on top

					if (self.Location == carryable.Location)
					{
						state = State.Turn;
						return this;
					}
					else
						return Util.SequenceActivities(movement.MoveTo(carryable.Location, 0), this);

				case State.Turn: // Align facing and Land

					if (sFacing.Facing != cFacing.Facing)
						return Util.SequenceActivities(new Turn(self, cFacing.Facing), this);
					else
					{
						state = State.Pickup;
						return Util.SequenceActivities(new HeliLand(false), new Wait(10), this);
					}

				case State.Pickup:

					// Remove our carryable from world
					self.World.AddFrameEndTask(w => carryable.World.Remove(carryable));

					aca.AttachCarryable(carryable);
					state = State.Transport;
					return this;

				case State.Transport:

					// Move self to destination
					var targetl = GetLocationToDrop(c.Destination);

					state = State.Land;
					return Util.SequenceActivities(movement.MoveTo(targetl, 0), this);

				case State.Land:

					if (!CanDropHere())
					{
						state = State.Transport;
						return this;
					}

					if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.LandAltitude))
						return this;
					else
					{
						state = State.Release;
						return Util.SequenceActivities(new Wait(15), this);
					}

				case State.Release:

					if (!CanDropHere())
					{
						state = State.Transport;
						return this;
					}

					positionable.SetPosition(carryable, self.Location, SubCell.FullCell);

					cFacing.Facing = sFacing.Facing;

					// Put back into world
					self.World.AddFrameEndTask(w => carryable.World.Add(carryable));

					// Unlock carryable
					aca.CarryableReleased();
					c.Dropped();

					state = State.Done;
					return Util.SequenceActivities(new Wait(10),  this);

				case State.Done:

					self.Trait<AutoCarryall>().UnreserveCarryable();
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
