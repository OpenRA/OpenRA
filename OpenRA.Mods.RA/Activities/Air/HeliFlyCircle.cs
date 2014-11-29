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
using System.Collections.Generic;
using OpenRA.Traits;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class HeliFlyCircle : Activity
	{
		readonly Helicopter helicopter;

		public HeliFlyCircle(Actor self)
		{
			helicopter = self.Trait<Helicopter>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.CruiseAltitude))
				return this;

			var move = helicopter.FlyStep(helicopter.Facing);
			helicopter.SetPosition(self, helicopter.CenterPosition + move);

			var desiredFacing = helicopter.Facing + 64;
			helicopter.Facing = Util.TickFacing(helicopter.Facing, desiredFacing, helicopter.ROT / 3);

			return this;
		}
	}
}
