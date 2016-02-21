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
using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Return to a player owned RearmBuildings. If none available, head back to base and circle over it.")]
	public class ReturnOnIdleInfo : ITraitInfo, Requires<AircraftInfo>
	{
		public object Create(ActorInitializer init) { return new ReturnOnIdle(init.Self, this); }
	}

	public class ReturnOnIdle : INotifyIdle
	{
		readonly AircraftInfo aircraftInfo;

		public ReturnOnIdle(Actor self, ReturnOnIdleInfo info)
		{
			aircraftInfo = self.Info.TraitInfo<AircraftInfo>();
		}

		public void TickIdle(Actor self)
		{
			// We're on the ground, let's stay there.
			if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length < aircraftInfo.MinAirborneAltitude)
				return;

			var airfield = ReturnToBase.ChooseAirfield(self, true);
			if (airfield != null)
			{
				self.QueueActivity(new ReturnToBase(self, airfield));
				self.QueueActivity(new ResupplyAircraft(self));
			}
			else
			{
				// nowhere to land, pick something friendly and circle over it.

				// I'd prefer something we own
				var someBuilding = self.World.ActorsHavingTrait<Building>()
					.FirstOrDefault(a => a.Owner == self.Owner);

				// failing that, something unlikely to shoot at us
				if (someBuilding == null)
					someBuilding = self.World.ActorsHavingTrait<Building>()
						.FirstOrDefault(a => self.Owner.Stances[a.Owner] == Stance.Ally);

				if (someBuilding == null)
				{
					// ... going down the garden to eat worms ...
					self.QueueActivity(new FlyOffMap(self));
					self.QueueActivity(new RemoveSelf());
					return;
				}

				self.QueueActivity(new Fly(self, Target.FromActor(someBuilding)));
				self.QueueActivity(new FlyCircle(self));
			}
		}
	}
}
