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

		public Rearm(Actor self)
		{
			ammoPools = self.TraitsImplementing<AmmoPool>().Where(p => !p.AutoReloads).ToArray();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var needsReloading = false;

			foreach (var pool in ammoPools)
			{
				if (pool.FullAmmo())
					continue;

				needsReloading = true;

				Reload(self, pool);
			}

			return needsReloading ? this : NextActivity;
		}

		void Reload(Actor self, AmmoPool ammoPool)
		{
			if (--ammoPool.RemainingTicks == 0)
			{
				// HACK to check if we are on the helipad/airfield/etc.
				var hostBuilding = self.World.ActorMap.GetActorsAt(self.Location)
					.FirstOrDefault(a => a.Info.HasTraitInfo<BuildingInfo>());

				if (hostBuilding == null || !hostBuilding.IsInWorld)
					return;

				foreach (var host in hostBuilding.TraitsImplementing<INotifyRearm>())
					host.Rearming(hostBuilding, self);

				ammoPool.RemainingTicks = ammoPool.Info.ReloadDelay;
				if (!string.IsNullOrEmpty(ammoPool.Info.RearmSound))
					Game.Sound.PlayToPlayer(SoundType.World, self.Owner, ammoPool.Info.RearmSound, self.CenterPosition);

				ammoPool.GiveAmmo(self, ammoPool.Info.ReloadCount);
			}
		}
	}
}
