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
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Rearm : CancelableActivity
	{
		int remainingTicks = ticksPerPip;

		const int ticksPerPip = 25 * 2;

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo == null) return NextActivity;

			if (--remainingTicks == 0)
			{
				if (!limitedAmmo.GiveAmmo()) return NextActivity;

				var hostBuilding = self.World.FindUnits(self.CenterLocation, self.CenterLocation)
					.FirstOrDefault(a => a.HasTrait<RenderBuilding>());

				if (hostBuilding != null)
					hostBuilding.Trait<RenderBuilding>().PlayCustomAnim(hostBuilding, "active");

				remainingTicks = ticksPerPip;
			}

			return this;
		}
	}
}
