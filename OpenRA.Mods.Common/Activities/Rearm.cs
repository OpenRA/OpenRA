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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;

namespace OpenRA.Mods.Common.Activities
{
	public class Rearm : Activity
	{
		readonly AmmoPool[] ammoPools;
		readonly Dictionary<AmmoPool, int> ammoPoolsReloadTimes;

		public Rearm(Actor self)
		{
			ammoPools = self.TraitsImplementing<AmmoPool>().Where(p => !p.Info.SelfReloads).ToArray();

			if (ammoPools == null)
				return;

			ammoPoolsReloadTimes = ammoPools.ToDictionary(x => x, y => y.Info.ReloadDelay);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || ammoPoolsReloadTimes == null)
				return NextActivity;

			var needsReloading = false;

			foreach (var pool in ammoPools)
			{
				if (pool.FullAmmo())
					continue;

				needsReloading = true;

				if (--ammoPoolsReloadTimes[pool] > 0)
					continue;

				// HACK to check if we are on the helipad/airfield/etc.
				var hostBuilding = self.World.ActorMap.GetActorsAt(self.Location)
					.FirstOrDefault(a => a.Info.HasTraitInfo<BuildingInfo>());

				if (hostBuilding == null || !hostBuilding.IsInWorld)
					return NextActivity;

				if (!pool.GiveAmmo())
					continue;

				foreach (var host in hostBuilding.TraitsImplementing<INotifyRearm>())
					host.Rearming(hostBuilding, self);

				var sound = pool.Info.RearmSound;
				if (sound != null)
					Game.Sound.PlayToPlayer(self.Owner, sound, self.CenterPosition);

				ammoPoolsReloadTimes[pool] = pool.Info.ReloadDelay;
			}

			return needsReloading ? this : NextActivity;
		}
	}
}
