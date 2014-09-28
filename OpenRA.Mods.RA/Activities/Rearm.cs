#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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
		string sound = null;

		public Rearm(Actor self, string sound = null)
		{
			limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null)
				ticksPerPip = limitedAmmo.ReloadTimePerAmmo();
			remainingTicks = ticksPerPip;
			this.sound = sound;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || limitedAmmo == null)
				return NextActivity;

			if (--remainingTicks == 0)
			{
				var hostBuilding = self.World.ActorMap.GetUnitsAt(self.Location)
					.FirstOrDefault(a => a.HasTrait<RenderBuilding>());

				if (hostBuilding == null || !hostBuilding.IsInWorld)
					return NextActivity;

				if (!limitedAmmo.GiveAmmo())
					return NextActivity;

				hostBuilding.Trait<RenderBuilding>().PlayCustomAnim(hostBuilding, "active");
				if (sound != null)
					Sound.Play(sound, self.CenterPosition);

				remainingTicks = limitedAmmo.ReloadTimePerAmmo();
			}

			return this;
		}
	}
}
