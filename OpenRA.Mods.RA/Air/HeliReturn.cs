﻿#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.RA.Activities;

namespace OpenRA.Mods.RA.Air
{
	public class HeliReturn : Activity
	{
		static Actor ChooseHelipad(Actor self)
		{
			var rearmBuildings = self.Info.Traits.Get<HelicopterInfo>().RearmBuildings;
			return self.World.Actors.Where( a => a.Owner == self.Owner ).FirstOrDefault(
				a => rearmBuildings.Contains(a.Info.Name) &&
					!Reservable.IsReserved(a));
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			var dest = ChooseHelipad(self);

			var initialFacing = self.Info.Traits.Get<AircraftInfo>().InitialFacing;

			if (dest == null)
			{
				var rearmBuildings = self.Info.Traits.Get<HelicopterInfo>().RearmBuildings;
				var nearestHpad = self.World.ActorsWithTrait<Reservable>()
									.Where(a => a.Actor.Owner == self.Owner && rearmBuildings.Contains(a.Actor.Info.Name))
									.Select(a => a.Actor)
									.ClosestTo(self);

				if (nearestHpad == null)
					return Util.SequenceActivities(new Turn(initialFacing), new HeliLand(true, 0), NextActivity);
				else
					return Util.SequenceActivities(new HeliFly(Util.CenterOfCell(nearestHpad.Location)));
			}

			var res = dest.TraitOrDefault<Reservable>();
			var heli = self.Trait<Helicopter>();
			if (res != null)
				heli.reservation = res.Reserve(dest, self, heli);

			var exit = dest.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
			var offset = exit != null ? exit.SpawnOffsetVector : PVecInt.Zero;

			return Util.SequenceActivities(
				new HeliFly(dest.Trait<IHasLocation>().PxPosition + offset),
				new Turn(initialFacing),
				new HeliLand(false, 0),
				new Rearm(self),
				NextActivity);
		}
	}
}
