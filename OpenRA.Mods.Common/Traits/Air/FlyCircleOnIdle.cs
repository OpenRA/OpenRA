#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Fly in circle while idle (on stop,..).")]
	public class FlyCircleOnIdleInfo : ITraitInfo, Requires<AircraftInfo>
	{
		public object Create(ActorInitializer init) { return new FlyCircleOnIdle(init.Self, this); }
	}

	public class FlyCircleOnIdle : INotifyIdle
	{
		readonly AircraftInfo aircraftInfo;

		public FlyCircleOnIdle(Actor self, FlyCircleOnIdleInfo info)
		{
			aircraftInfo = self.Info.TraitInfo<AircraftInfo>();
		}

		public void TickIdle(Actor self)
		{
			// We're on the ground, let's stay there.
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length < aircraftInfo.MinAirborneAltitude)
				return;

			self.QueueActivity(new FlyCircle(self));
		}
	}
}
