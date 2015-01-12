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
		readonly Actor toPickup;
		readonly IMove movement;
		readonly Carryable carryable;
		readonly Carryall aca;
		readonly Helicopter helicopter;
		readonly IFacing carryableFacing;
		readonly IFacing selfFacing;

		enum State { Intercept, LockCarryable, MoveToCarryable, Turn, Pickup, TakeOff }

		State state;

		public PickupUnit(Actor self, Actor client)
		{
			this.toPickup = client;
			movement = self.Trait<IMove>();
			carryable = client.Trait<Carryable>();
			aca = self.Trait<Carryall>();
			helicopter = self.Trait<Helicopter>();
			carryableFacing = client.Trait<IFacing>();
			selfFacing = self.Trait<IFacing>();
			state = State.Intercept;
		}

		public override Activity Tick(Actor self)
		{
			if (toPickup.IsDead)
			{
				aca.UnreserveCarryable();
				return NextActivity;
			}

			switch (state)
			{
				case State.Intercept: // Move towards our carryable

					state = State.LockCarryable;
					return Util.SequenceActivities(movement.MoveWithinRange(Target.FromActor(toPickup), WRange.FromCells(4)), this);

				case State.LockCarryable:
					// Last check
					if (carryable.StandbyForPickup(self))
					{
						state = State.MoveToCarryable;
						return this;
					} 

					// We got cancelled
					aca.UnreserveCarryable();
					return NextActivity;

				case State.MoveToCarryable: // We arrived, move on top

					if (self.Location == toPickup.Location)
					{
						state = State.Turn;
						return this;
					}

					return Util.SequenceActivities(movement.MoveTo(toPickup.Location, 0), this);

				case State.Turn: // Align facing and Land

					if (selfFacing.Facing != carryableFacing.Facing)
						return Util.SequenceActivities(new Turn(self, carryableFacing.Facing), this);
			
					state = State.Pickup;
					return Util.SequenceActivities(new HeliLand(false), new Wait(10), this);

				case State.Pickup:

					// Remove our carryable from world
					self.World.AddFrameEndTask(w => toPickup.World.Remove(toPickup));

					aca.AttachCarryable(toPickup);
					state = State.TakeOff;
					return this;

				case State.TakeOff:

					if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.CruiseAltitude))
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