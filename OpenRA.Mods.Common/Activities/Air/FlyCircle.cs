#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
	public class FlyCircle : Activity
	{
		readonly Plane plane;

		public FlyCircle(Actor self)
		{
			plane = self.Trait<Plane>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var terrainHeight = self.World.Map.TerrainHeightAt(self.CenterPosition);
			var destAltitude = terrainHeight + plane.Info.CruiseAltitude;

			// We can't possibly turn this fast
			var desiredFacing = plane.Facing + 64;
			Fly.FlyToward(self, plane, desiredFacing, destAltitude);

			return this;
		}
	}
}
