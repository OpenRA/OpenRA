﻿#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class FlyTimed : Activity
	{
		int remainingTicks;

		public FlyTimed(int ticks) { remainingTicks = ticks; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || remainingTicks-- == 0)
				return NextActivity;

			var plane = self.Trait<Plane>();
			var cruiseAltitude = new WRange(plane.Info.CruiseAltitude * 1024 / Game.CellSize);
			Fly.FlyToward(self, plane, plane.Facing, cruiseAltitude);

			return this;
		}
	}

	public class FlyOffMap : Activity
	{
		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !self.World.Map.IsInMap(self.Location))
				return NextActivity;

			var plane = self.Trait<Plane>();
			var cruiseAltitude = new WRange(plane.Info.CruiseAltitude * 1024 / Game.CellSize);
			Fly.FlyToward(self, plane, plane.Facing, cruiseAltitude);
			return this;
		}

		public override void Cancel(Actor self)
		{
			base.Cancel(self);
		}
	}
}
