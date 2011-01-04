#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public class ShakeOnDeathInfo : ITraitInfo
	{
		public readonly int Intensity = 10;
		public object Create(ActorInitializer init) { return new ShakeOnDeath(this); }
	}

	public class ShakeOnDeath : INotifyDamage
	{
		readonly ShakeOnDeathInfo Info;
		public ShakeOnDeath(ShakeOnDeathInfo info)
		{
			this.Info = info;
		}
		
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				self.World.WorldActor.Trait<ScreenShaker>().AddEffect(Info.Intensity, self.CenterLocation, 1);
		}
	}
}
