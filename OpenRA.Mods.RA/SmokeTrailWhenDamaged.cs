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
	class SmokeTrailWhenDamagedInfo : ITraitInfo
	{
		public readonly int[] Offset = { 0, 0 };
		public readonly int Interval = 1;

		public object Create(ActorInitializer init) { return new SmokeTrailWhenDamaged(init.self, this); }
	}

	class SmokeTrailWhenDamaged : ITick
	{
		Turret smokeTurret;
		int2 position;
		int interval;
		int ticks;

		public SmokeTrailWhenDamaged(Actor self, SmokeTrailWhenDamagedInfo info)
		{
			smokeTurret = new Turret(info.Offset);
			interval = info.Interval;
		}

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				if (self.Trait<IMove>().Altitude > 0)
				{
					var facing = self.Trait<IFacing>();
					var altitude = new float2(0, self.Trait<IMove>().Altitude);
					position = (self.CenterLocation - Combat.GetTurretPosition(self, facing, smokeTurret) - altitude).ToInt2();

					if (self.GetDamageState() >= DamageState.Heavy)
						self.World.AddFrameEndTask(
							w => w.Add(new Smoke(w, position, "smokey")));
				}

				ticks = interval;
			}
		}
	}
}
