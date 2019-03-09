#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class TakeOff : Activity
	{
		readonly Aircraft aircraft;
		readonly IMove move;
		Func<Actor, Activity, CPos, bool> moveToRallyPoint;

		public TakeOff(Actor self)
		{
			aircraft = self.Trait<Aircraft>();
			move = self.Trait<IMove>();
			moveToRallyPoint = (actor, activity, pos) => NextActivity == null;
		}

		public TakeOff(Actor self, Func<Actor, Activity, CPos, bool> moveToRallyPoint)
			: this(self)
		{
			this.moveToRallyPoint = moveToRallyPoint;
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			aircraft.UnReserve();

			var host = aircraft.GetActorBelow();
			var hasHost = host != null;
			var rp = hasHost ? host.TraitOrDefault<RallyPoint>() : null;

			var destination = rp != null ? rp.Location :
				(hasHost ? self.World.Map.CellContaining(host.CenterPosition) : self.Location);

			if (moveToRallyPoint(self, this, destination))
				return new AttackMoveActivity(self, () => move.MoveTo(destination, 1));

			return NextActivity;
		}
	}
}
