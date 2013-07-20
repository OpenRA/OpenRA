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
				if (!limitedAmmo.GiveAmmo()) return NextActivity;

				var hostBuilding = self.World.FindActorsInBox(self.CenterPosition, self.CenterPosition)
					.FirstOrDefault(a => a.HasTrait<RenderBuilding>());

				if (hostBuilding != null)
					hostBuilding.Trait<RenderBuilding>().PlayCustomAnim(hostBuilding, "active");

				remainingTicks = limitedAmmo.ReloadTimePerAmmo();
			}

			return this;
		}
	}
}
