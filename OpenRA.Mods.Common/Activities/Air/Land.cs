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
	public class Land : Activity
	{
		readonly Target target;
		readonly Aircraft aircraft;
		readonly WDist landAltitude;
		readonly bool requireSpace;

		bool soundPlayed;

		public Land(Actor self, bool requireSpace = false)
			: this(self, requireSpace, WDist.Zero) { }

		public Land(Actor self, Target t)
			: this(self, false, WDist.Zero)
		{
			target = t;
		}

		public Land(Actor self, bool requireSpace, WDist landAltitude)
		{
			aircraft = self.Trait<Aircraft>();
			this.requireSpace = requireSpace;
			this.landAltitude = landAltitude != WDist.Zero ? landAltitude : aircraft.Info.LandAltitude;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (aircraft.Info.VTOL)
			{
				if (requireSpace && !aircraft.CanLand(self.Location))
					return this;

				if (!soundPlayed && aircraft.Info.LandingSounds.Length > 0 && !self.IsAtGroundLevel())
					PlayLandingSound(self);

				if (Fly.FlyToward(self, aircraft, aircraft.Facing, landAltitude, moveVerticalOnly: true))
					return this;

				return NextActivity;
			}

			if (!target.IsValidFor(self))
			{
				Cancel(self);
				return NextActivity;
			}

			if (!soundPlayed && aircraft.Info.LandingSounds.Length > 0 && !self.IsAtGroundLevel())
				PlayLandingSound(self);

			var d = target.CenterPosition - self.CenterPosition;

			// The next move would overshoot, so just set the final position
			var move = aircraft.FlyStep(aircraft.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				aircraft.SetPosition(self, target.CenterPosition);
				return NextActivity;
			}

			var landingAlt = self.World.Map.DistanceAboveTerrain(target.CenterPosition);
			Fly.FlyToward(self, aircraft, d.Yaw.Facing, landingAlt);

			return this;
		}

		void PlayLandingSound(Actor self)
		{
			Game.Sound.Play(SoundType.World, aircraft.Info.LandingSounds.Random(self.World.SharedRandom), aircraft.CenterPosition);
			soundPlayed = true;
		}
	}
}
