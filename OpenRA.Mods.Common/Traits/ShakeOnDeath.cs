#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	public class ShakeOnDeathInfo : ITraitInfo
	{
		public readonly int Intensity = 10;
		public object Create(ActorInitializer init) { return new ShakeOnDeath(this); }
	}

	public class ShakeOnDeath : INotifyKilled
	{
		readonly ShakeOnDeathInfo Info;

		public ShakeOnDeath(ShakeOnDeathInfo info)
		{
			this.Info = info;
		}

		public void Killed(Actor self, AttackInfo e)
		{
			self.World.WorldActor.Trait<ScreenShaker>().AddEffect(Info.Intensity, self.CenterPosition, 1);
		}
	}
}
