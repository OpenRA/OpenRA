#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Yupgi_alert.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Yupgi_alert.Activities
{
	public class BlimpFallToEarth : Activity
	{
		readonly Aircraft aircraft;
		readonly BlimpFallsToEarthInfo info;
		int acceleration = 0;
		int spin = 0;

		public BlimpFallToEarth(Actor self, BlimpFallsToEarthInfo info)
		{
			this.info = info;
			aircraft = self.Trait<Aircraft>();
			if (info.Spins)
			{
				spin = info.SpinInitial;
				acceleration = info.SpinAcceleration;
			}
		}

		public override Activity Tick(Actor self)
		{
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length <= 0)
			{
				if (info.ExplosionWeapon != null)
				{
					// Use .FromPos since this actor is killed. Cannot use Target.FromActor
					info.ExplosionWeapon.Impact(Target.FromPos(self.CenterPosition), self, Enumerable.Empty<int>());
				}

				self.Dispose();
				return null;
			}

			if (info.Spins)
			{
				spin += acceleration;
				aircraft.Facing = (aircraft.Facing + spin) % 256;
			}

			var move = info.Moves ? aircraft.FlyStep(aircraft.Facing) : WVec.Zero;
			move -= new WVec(WDist.Zero, WDist.Zero, info.Velocity);
			aircraft.SetPosition(self, aircraft.CenterPosition + move);

			return this;
		}
	}
}
