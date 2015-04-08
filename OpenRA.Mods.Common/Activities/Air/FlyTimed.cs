#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyTimed : Activity
	{
		readonly Plane plane;
		int remainingTicks;

		public FlyTimed(int ticks, Actor self)
		{
			remainingTicks = ticks;
			plane = self.Trait<Plane>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || remainingTicks-- == 0)
				return NextActivity;

			var cruiseAltitude = self.World.Map.TerrainHeightAt(self.CenterPosition) +
				plane.Info.CruiseAltitude;

			Fly.FlyToward(self, plane, plane.Facing, cruiseAltitude);

			return this;
		}
	}
}
