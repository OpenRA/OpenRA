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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliReturnToBase : Activity
	{
		readonly Aircraft heli;

		public HeliReturnToBase(Actor self)
		{
			heli = self.Trait<Aircraft>();
		}

		public Actor ChooseHelipad(Actor self)
		{
			var rearmBuildings = heli.Info.RearmBuildings;
			return self.World.Actors.Where(a => a.Owner == self.Owner).FirstOrDefault(
				a => rearmBuildings.Contains(a.Info.Name) && !Reservable.IsReserved(a));
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var dest = ChooseHelipad(self);
			var initialFacing = heli.Info.InitialFacing;

			if (dest == null)
			{
				var rearmBuildings = heli.Info.RearmBuildings;
				var nearestHpad = self.World.ActorsHavingTrait<Reservable>()
									.Where(a => a.Owner == self.Owner && rearmBuildings.Contains(a.Info.Name))
									.ClosestTo(self);

				if (nearestHpad == null)
					return ActivityUtils.SequenceActivities(new Turn(self, initialFacing), new HeliLand(self, true), NextActivity);
				else
					return ActivityUtils.SequenceActivities(new HeliFly(self, Target.FromActor(nearestHpad)));
			}

			heli.MakeReservation(dest);

			var exit = dest.Info.TraitInfos<ExitInfo>().FirstOrDefault();
			var offset = (exit != null) ? exit.SpawnOffset : WVec.Zero;

			return ActivityUtils.SequenceActivities(
				new HeliFly(self, Target.FromPos(dest.CenterPosition + offset)),
				new Turn(self, initialFacing),
				new HeliLand(self, false),
				new ResupplyAircraft(self));
		}
	}
}
