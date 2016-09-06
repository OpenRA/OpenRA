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
	public class DeliverUnit : Activity
	{
		readonly Actor self;
		readonly Actor cargo;
		readonly IMove movement;
		readonly Carryable carryable;
		readonly Carryall carryall;
		readonly Aircraft aircraft;
		readonly IPositionable positionable;
		readonly IFacing cargoFacing;
		readonly IFacing selfFacing;

		enum State { Transport, Land, Release }

		State state;

		public DeliverUnit(Actor self)
		{
			carryall = self.Trait<Carryall>();
			this.self = self;
			cargo = carryall.Carrying;
			movement = self.Trait<IMove>();
			carryable = cargo.Trait<Carryable>();
			aircraft = self.Trait<Aircraft>();
			positionable = cargo.Trait<IPositionable>();
			cargoFacing = cargo.Trait<IFacing>();
			selfFacing = self.Trait<IFacing>();
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
			if (cargo.IsDead || !carryall.IsBusy)
			{
				carryall.UnreserveCarryable();
				return NextActivity;
			}

			switch (state)
			{
				case State.Transport:
					var targetl = GetLocationToDrop(carryable.Destination);
					state = State.Land;
					return ActivityUtils.SequenceActivities(movement.MoveTo(targetl, 0), this);

				case State.Land:
					if (!CanDropHere())
					{
						state = State.Transport;
						return this;
					}

					if (HeliFly.AdjustAltitude(self, aircraft, aircraft.Info.LandAltitude))
						return this;
					state = State.Release;
					return ActivityUtils.SequenceActivities(new Wait(15), this);

				case State.Release:
					if (!CanDropHere())
					{
						state = State.Transport;
						return this;
					}

					Release();
					return NextActivity;
			}

			return NextActivity;
		}

		void Release()
		{
			positionable.SetPosition(cargo, self.Location, SubCell.FullCell);
			cargoFacing.Facing = selfFacing.Facing;

			// Put back into world
			self.World.AddFrameEndTask(w =>
			{
				cargo.World.Add(cargo);
				carryall.UnreserveCarryable();
			});

			// Unlock carryable
			carryall.CarryableReleased();
			carryable.Dropped();
		}

		public override void Cancel(Actor self)
		{
			// TODO: Drop the unit at the nearest available cell
		}
	}
}
