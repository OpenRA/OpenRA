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
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.D2k.Traits;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Activities
{
	public class PickupUnit : Activity
	{
		readonly Actor carryable;
		readonly IMove movement;
		readonly Carryable c;
		readonly AutoCarryall aca;
		readonly Helicopter helicopter;
		readonly IFacing cFacing; // Carryable facing
		readonly IFacing sFacing; // Self facing

		enum State { Intercept, LockCarryable, MoveToCarryable, Turn, Pickup, TakeOff }

		State state;

		public PickupUnit(Actor self, Actor carryable)
		{
			this.carryable = carryable;
			movement = self.Trait<IMove>();
			c = carryable.Trait<Carryable>();
			aca = self.Trait<AutoCarryall>();
			helicopter = self.Trait<Helicopter>();
			cFacing = carryable.Trait<IFacing>();
			sFacing = self.Trait<IFacing>();
			state = State.Intercept;
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
					state = State.TakeOff;
					return this;

				case State.TakeOff:

					if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.CruiseAltitude))
						return this;
					else
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