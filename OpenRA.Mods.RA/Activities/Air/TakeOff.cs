#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class TakeOff : Activity
	{
		public override Activity Tick(Actor self)
		{
			var aircraft = self.Trait<Aircraft>();

			self.CancelActivity();

			var reservation = aircraft.Reservation;
			if (reservation != null)
			{
				reservation.Dispose();
				reservation = null;
			}

			var host = aircraft.GetActorBelow();
			var hasHost = host != null;
			var rp = hasHost ? host.TraitOrDefault<RallyPoint>() : null;

			var destination = rp != null ? rp.Location :
				(hasHost ? self.World.Map.CellContaining(host.CenterPosition) : self.Location);

			return new AttackMove.AttackMoveActivity(self, self.Trait<IMove>().MoveTo(destination, 1));
		}
	}
}
