#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Air
{
	class ReturnOnIdleInfo : TraitInfo<ReturnOnIdle> { }

	class ReturnOnIdle : INotifyIdle
	{
		public void TickIdle(Actor self)
		{
			// We're on the ground, let's stay there.
			if (self.CenterPosition.Z == 0)
				return;

			var airfield = ReturnToBase.ChooseAirfield(self, true);
			if (airfield != null)
			{
				self.QueueActivity(new ReturnToBase(self, airfield));
				self.QueueActivity(new ResupplyAircraft());
			}
			else
			{
				// nowhere to land, pick something friendly and circle over it.

				// i'd prefer something we own
				var someBuilding = self.World.ActorsWithTrait<Building>()
					.Select(a => a.Actor)
					.FirstOrDefault(a => a.Owner == self.Owner);

				// failing that, something unlikely to shoot at us
				if (someBuilding == null)
					someBuilding = self.World.ActorsWithTrait<Building>()
						.Select(a => a.Actor)
						.FirstOrDefault(a => self.Owner.Stances[a.Owner] == Stance.Ally);

				if (someBuilding == null)
				{
					// ... going down the garden to eat worms ...
					self.QueueActivity(new FlyOffMap());
					self.QueueActivity(new RemoveSelf());
					return;
				}

				self.QueueActivity(new Fly(self, Target.FromActor(someBuilding)));
				self.QueueActivity(new FlyCircle());
			}
		}
	}

	class FlyAwayOnIdleInfo : TraitInfo<FlyAwayOnIdle> { }

	class FlyAwayOnIdle : INotifyIdle
	{
		public void TickIdle(Actor self)
		{
			self.QueueActivity(new FlyOffMap());
			self.QueueActivity(new RemoveSelf());
		}
	}
}
