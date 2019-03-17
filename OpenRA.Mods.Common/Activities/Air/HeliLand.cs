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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliLand : Activity
	{
		readonly Aircraft aircraft;
		readonly WDist landAltitude;
		readonly bool requireSpace;
		readonly Actor ignoreActor;

		bool soundPlayed;
		bool landingInitiated;

		public HeliLand(Actor self, bool requireSpace, Actor ignoreActor = null)
			: this(self, requireSpace, self.Info.TraitInfo<AircraftInfo>().LandAltitude, ignoreActor) { }

		public HeliLand(Actor self, bool requireSpace, WDist landAltitude, Actor ignoreActor = null)
		{
			this.requireSpace = requireSpace;
			this.landAltitude = landAltitude;
			this.ignoreActor = ignoreActor;
			aircraft = self.Trait<Aircraft>();
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (IsCanceling)
			{
				aircraft.RemoveInfluence();
				return NextActivity;
			}

			if (requireSpace && !landingInitiated)
			{
				var landingCell = self.Location;
				if (!aircraft.CanLand(landingCell, ignoreActor))
				{
					QueueChild(self, new Wait(25), true);
					self.NotifyBlocker(landingCell);
					return this;
				}

				aircraft.AddInfluence(landingCell);
				aircraft.EnteringCell(self);
				landingInitiated = true;
			}

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
