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

namespace OpenRA.Mods.Common.Activities
{
	public class FlyOffMap : Activity
	{
		readonly Aircraft plane;

		public FlyOffMap(Actor self)
		{
			plane = self.Trait<Aircraft>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !self.World.Map.Contains(self.Location))
				return NextActivity;

			Fly.FlyToward(self, plane, plane.Facing, plane.Info.CruiseAltitude);
			return this;
		}
	}
}
