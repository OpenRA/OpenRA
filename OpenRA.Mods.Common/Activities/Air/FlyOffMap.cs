#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyOffMap : Activity
	{
		readonly Aircraft aircraft;

		public FlyOffMap(Actor self)
		{
			aircraft = self.Trait<Aircraft>();
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			if (IsCanceled)
				return NextActivity;

			// Fly towards the edge of the map then dispose the actor once it is outside
			// TODO: We will want a minimum margin here so that the visible bounds of the aircraft are fully outside the map
			// TODO: Make sure that this works for CanHover/VTOL aircraft after activities have been merged
			if (self.World.Map.Contains(self.Location))
				Fly.FlyToward(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
			else
				self.Dispose();

			return this;
		}
	}
}
