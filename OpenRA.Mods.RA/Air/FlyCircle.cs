#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class FlyCircle : Activity
	{
		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var plane = self.Trait<Plane>();

			// We can't possibly turn this fast
			var desiredFacing = plane.Facing + 64;
			Fly.FlyToward(self, plane, desiredFacing, plane.Info.CruiseAltitude);

			return this;
		}
	}
}
