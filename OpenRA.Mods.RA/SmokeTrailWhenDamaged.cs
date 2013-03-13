#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SmokeTrailWhenDamagedInfo : ITraitInfo
	{
		public readonly int[] Offset = { 0, 0 };
		public readonly int Interval = 3;

		public object Create(ActorInitializer init) { return new SmokeTrailWhenDamaged(init.self, this); }
	}

	class SmokeTrailWhenDamaged : ITick
	{
		Turret smokeTurret;
		PPos position;
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
				var move = self.Trait<IMove>();
				if (move.Altitude > 0 && self.GetDamageState() >= DamageState.Heavy)
				{
					var facing = self.Trait<IFacing>();
					var altitude = new PVecInt(0, move.Altitude);
					position = (self.CenterLocation - (PVecInt)smokeTurret.PxPosition(self, facing).ToInt2());

					if (self.World.RenderedShroud.IsVisible(position.ToCPos()))
						self.World.AddFrameEndTask(
							w => w.Add(new Smoke(w, position - altitude, "smokey")));
				}

				ticks = interval;
			}
		}
	}
}
