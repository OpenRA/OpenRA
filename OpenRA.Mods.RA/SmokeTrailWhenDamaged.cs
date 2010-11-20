#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SmokeTrailWhenDamagedInfo : TraitInfo<SmokeTrailWhenDamaged> { }

	class SmokeTrailWhenDamaged : ITick
	{
		public void Tick(Actor self)
		{
			if (self.GetDamageState() >= DamageState.Heavy)
				self.World.AddFrameEndTask(
					w => { if (!self.Destroyed) w.Add(
						new Smoke(w, self.CenterLocation.ToInt2() 
							- new int2(0, self.Trait<IMove>().Altitude), 
							"smokey")); });
		}
	}
}
