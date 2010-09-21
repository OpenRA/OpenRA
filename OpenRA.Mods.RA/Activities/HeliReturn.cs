#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class HeliReturn : CancelableActivity
	{
		static Actor ChooseHelipad(Actor self)
		{
			var rearmBuildings = self.Info.Traits.Get<HelicopterInfo>().RearmBuildings;
			return self.World.Queries.OwnedBy[self.Owner].FirstOrDefault(
				a => rearmBuildings.Contains(a.Info.Name) &&
					!Reservable.IsReserved(a));
		}

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			var dest = ChooseHelipad(self);

			var initialFacing = self.Info.Traits.Get<AircraftInfo>().InitialFacing;

			if (dest == null)
				return Util.SequenceActivities(
					new Turn(initialFacing), 
					new HeliLand(true),
					NextActivity);

			var res = dest.TraitOrDefault<Reservable>();
			if (res != null)
				self.Trait<Helicopter>().reservation = res.Reserve(self);

			var exit = dest.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
			var offset = exit != null ? exit.SpawnOffset : float2.Zero;

			return Util.SequenceActivities(
				new HeliFly(dest.CenterLocation + offset),
				new Turn(initialFacing),
				new HeliLand(false),
				new Rearm(),
				NextActivity);
		}
	}
}
