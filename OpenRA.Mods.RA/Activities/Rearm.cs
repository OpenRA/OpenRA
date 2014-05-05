#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Rearm : Activity
	{
		readonly LimitedAmmo limitedAmmo;
		int ticksPerPip = 25 * 2;
		int remainingTicks = 25 * 2;

		public Rearm(Actor self)
		{
			limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null)
				ticksPerPip = limitedAmmo.ReloadTimePerAmmo();
			remainingTicks = ticksPerPip;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || limitedAmmo == null) return NextActivity;

			if (--remainingTicks == 0)
			{
				var hostBuilding = self.World.ActorMap.GetUnitsAt(self.Location)
					.FirstOrDefault(a => a.HasTrait<RenderBuilding>());

				if (!limitedAmmo.GiveAmmo())
				{
					var helicopter = self.TraitOrDefault<Helicopter>();
					if (helicopter != null)
					{
						if (helicopter.Info.RepairBuildings.Contains(hostBuilding.Info.Name) && self.HasTrait<Health>())
						{
							if (self.Trait<Health>().DamageState != DamageState.Undamaged)
								return NextActivity;
						}

						return helicopter.TakeOff(hostBuilding);
					}

					return NextActivity;
				}

				if (hostBuilding != null)
					hostBuilding.Trait<RenderBuilding>().PlayCustomAnim(hostBuilding, "active");

				remainingTicks = limitedAmmo.ReloadTimePerAmmo();
			}

			return this;
		}
	}
}
