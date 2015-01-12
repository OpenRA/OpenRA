#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion
using System;
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Activities
{
	public class DeliverUnit : Activity
	{
		readonly Actor self;
		readonly Actor toDeliver;
		readonly IMove movement;
		readonly Carryable carryable;
		readonly Carryall aca;
		readonly Helicopter helicopter;
		readonly IPositionable positionable;
		readonly IFacing carryableFacing;
		readonly IFacing selfFacing;
		readonly CPos destination;

		enum State { Transport, Land, Release, Takeoff, Done }

		State state;

		public DeliverUnit(Actor self)
		{
			aca = self.Trait<Carryall>();
			this.self = self;
			this.toDeliver = aca.Client;
			movement = self.Trait<IMove>();
			carryable = toDeliver.Trait<Carryable>();
			helicopter = self.Trait<Helicopter>();
			positionable = toDeliver.Trait<IPositionable>();
			carryableFacing = toDeliver.Trait<IFacing>();
			selfFacing = self.Trait<IFacing>();
			this.destination = carryable.Destination;
			state = State.Transport;
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
			if (toDeliver.IsDead || aca.HasCarryableAttached == false)
			{
				aca.UnreserveCarryable();
				return NextActivity;
			}

			switch (state)
			{
				case State.Transport:

					// Move self to destination
					var targetl = GetLocationToDrop(destination);

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

					state = State.Release;
					return Util.SequenceActivities(new Wait(15), this);

				case State.Release:

					if (!CanDropHere())
					{
						state = State.Transport;
						return this;
					}

					positionable.SetPosition(toDeliver, self.Location, SubCell.FullCell);

					carryableFacing.Facing = selfFacing.Facing;

					// Put back into world
					self.World.AddFrameEndTask(w => toDeliver.World.Add(toDeliver));

					// Unlock carryable
					aca.CarryableReleased();
					carryable.Dropped();

					state = State.Done;
					return Util.SequenceActivities(new Wait(10), this);

				case State.Done:

					self.Trait<Carryall>().UnreserveCarryable();
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
