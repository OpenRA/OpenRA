#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ReloadsInfo : ITraitInfo, Requires<LimitedAmmoInfo>
	{
		public readonly int Count = 0;
		public readonly int Period = 50;

		public object Create(ActorInitializer init) { return new Reloads(init.self, this); }
	}

	public class Reloads : ITick
	{
		[Sync] int remainingTicks;
		ReloadsInfo Info;
		LimitedAmmo la;

		public Reloads(Actor self, ReloadsInfo info)
		{
			Info = info;
			remainingTicks = info.Period;
			la = self.Trait<LimitedAmmo>();
		}

		public void Tick(Actor self)
		{
			if (--remainingTicks == 0)
			{
				remainingTicks = Info.Period;
				for (var i = 0; i < Info.Count; i++)
					la.GiveAmmo();
			}
		}
	}
}
