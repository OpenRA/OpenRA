#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FallToEarth : Activity
	{
		readonly Aircraft aircraft;
		readonly FallsToEarthInfo info;

		readonly int acceleration;
		int spin;

		public FallToEarth(Actor self, FallsToEarthInfo info)
		{
			this.info = info;
			IsInterruptible = false;
			aircraft = self.Trait<Aircraft>();
			if (!info.MaximumSpinSpeed.HasValue || info.MaximumSpinSpeed.Value != WAngle.Zero)
				acceleration = self.World.SharedRandom.Next(2) * 2 - 1;
		}

		public override bool Tick(Actor self)
		{
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length <= 0)
			{
				if (info.ExplosionWeapon != null)
				{
					// Use .FromPos since this actor is killed. Cannot use Target.FromActor
					info.ExplosionWeapon.Impact(Target.FromPos(self.CenterPosition), self);
				}

				self.Kill(self);
				Cancel(self);
				return true;
			}

			if (acceleration != 0)
			{
				if (!info.MaximumSpinSpeed.HasValue || Math.Abs(spin) < info.MaximumSpinSpeed.Value.Angle)
					spin += 4 * acceleration; // TODO: Possibly unhardcode this

				// Allow for negative spin values and convert from facing to angle units
				aircraft.Facing = new WAngle(aircraft.Facing.Angle + spin);
			}

			var move = info.Moves ? aircraft.FlyStep(aircraft.Facing) : WVec.Zero;
			move -= new WVec(WDist.Zero, WDist.Zero, info.Velocity);
			aircraft.SetPosition(self, aircraft.CenterPosition + move);

			return false;
		}
	}
}
