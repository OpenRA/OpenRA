#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class FlyCircle : Activity
	{
		public override Activity Tick(Actor self)
		{
			var cruiseAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;

			if (IsCanceled) return NextActivity;

			var aircraft = self.Trait<Aircraft>();

			var desiredFacing = aircraft.Facing + 64;   // we can't possibly turn this fast.
			if (aircraft.Altitude == cruiseAltitude)
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.ROT);

			if (aircraft.Altitude < cruiseAltitude)
				++aircraft.Altitude;

			FlyUtil.Fly(self, cruiseAltitude);
			return this;
		}
	}
}
