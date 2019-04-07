#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliLand : Activity
	{
		readonly Aircraft aircraft;
		readonly WDist landAltitude;
		readonly bool requireSpace;

		bool soundPlayed;

		public HeliLand(Actor self, bool requireSpace)
			: this(self, requireSpace, self.Info.TraitInfo<AircraftInfo>().LandAltitude) { }

		public HeliLand(Actor self, bool requireSpace, WDist landAltitude)
		{
			this.requireSpace = requireSpace;
			this.landAltitude = landAltitude;
			aircraft = self.Trait<Aircraft>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceling)
				return NextActivity;

			if (requireSpace && !aircraft.CanLand(self.Location))
				return this;

			if (!soundPlayed && aircraft.Info.LandingSounds.Length > 0 && !self.IsAtGroundLevel())
			{
				Game.Sound.Play(SoundType.World, aircraft.Info.LandingSounds, self.World, aircraft.CenterPosition);
				soundPlayed = true;
			}

			if (HeliFly.AdjustAltitude(self, aircraft, landAltitude))
				return this;

			return NextActivity;
		}
	}
}
