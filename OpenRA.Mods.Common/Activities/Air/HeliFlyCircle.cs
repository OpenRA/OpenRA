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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliFlyCircle : Activity
	{
		readonly Aircraft helicopter;

		public HeliFlyCircle(Actor self)
		{
			helicopter = self.Trait<Aircraft>();
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (helicopter.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			if (IsCanceled)
				return NextActivity;

			if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.CruiseAltitude))
				return this;

			var move = helicopter.FlyStep(helicopter.Facing);
			helicopter.SetPosition(self, helicopter.CenterPosition + move);

			var desiredFacing = helicopter.Facing + 64;
			helicopter.Facing = Util.TickFacing(helicopter.Facing, desiredFacing, helicopter.TurnSpeed / 3);

			return this;
		}
	}
}
