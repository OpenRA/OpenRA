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
		readonly Minelayer minelayer;
		int ticksPerPip = 25 * 2;
		int remainingTicks = 25 * 2;
		string sound = null;

		public Rearm(Actor self, string sound = null)
		{
			this.sound = sound;
			foreach (var arm in self.TraitsImplementing<Armament>())
			{
				if (arm.Info.LimitedAmmo != 0)
					ticksPerPip = arm.ReloadTimePerAmmo();

				remainingTicks = ticksPerPip;
			}
			minelayer = self.TraitOrDefault<Minelayer>();
			if (minelayer != null)
			{
				ticksPerPip = minelayer.ReloadTimePerPayload();
				remainingTicks = ticksPerPip;
			}
		}

		public override Activity Tick(Actor self)
		{
			foreach (var arm in self.TraitsImplementing<Armament>())
			{
				if (IsCanceled || arm.Info.LimitedAmmo <= 0) 
					return NextActivity;

				if (--remainingTicks == 0)
				{
					var hostBuilding = self.World.ActorMap.GetUnitsAt(self.Location)
						.FirstOrDefault(a => a.HasTrait<RenderBuilding>());

					if (hostBuilding == null || !hostBuilding.IsInWorld)
						return NextActivity;

					if (!arm.GiveAmmo())
						return NextActivity;

					hostBuilding.Trait<RenderBuilding>().PlayCustomAnim(hostBuilding, "active");
					if (sound != null)
						Sound.Play(sound, self.CenterPosition);

					remainingTicks = arm.ReloadTimePerAmmo();
				}
				return this;
			}

			if (minelayer != null)
			{
				if (IsCanceled || minelayer.Info.Payload <= 0) 
					return NextActivity;

				if (--remainingTicks == 0)
				{
					var hostBuilding = self.World.ActorMap.GetUnitsAt(self.Location)
						.FirstOrDefault(a => a.HasTrait<RenderBuilding>());

					if (hostBuilding == null || !hostBuilding.IsInWorld)
						return NextActivity;

					if (!minelayer.GivePayload())
						return NextActivity;

					hostBuilding.Trait<RenderBuilding>().PlayCustomAnim(hostBuilding, "active");
					if (sound != null)
						Sound.Play(sound, self.CenterPosition);

					remainingTicks = minelayer.ReloadTimePerPayload();
				}
				return this;
			}

			return this;
		}
	}
}
