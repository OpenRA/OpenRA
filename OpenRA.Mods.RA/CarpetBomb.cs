#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CarpetBombInfo : TraitInfo<CarpetBomb>
	{
		public readonly string Weapon = null;
		public readonly int Range = 0;
	}

	class CarpetBomb : ITick			// todo: maybe integrate this better with the normal weapons system?
	{
		int2 Target;
		int dropDelay;

		public void SetTarget(int2 targetCell) { Target = targetCell; }

		public void Tick(Actor self)
		{
			var info = self.Info.Traits.Get<CarpetBombInfo>();

			if ((self.Location - Target).LengthSquared > info.Range * info.Range)
				return;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return;

			if (--dropDelay <= 0)
			{
				var weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()];
				dropDelay = weapon.ROF;

				var args = new ProjectileArgs
				{
					srcAltitude = self.traits.Get<Unit>().Altitude,
					destAltitude = 0,
					src = self.CenterLocation.ToInt2(),
					dest = self.CenterLocation.ToInt2(),
					facing = self.traits.Get<Unit>().Facing,
					firedBy = self,
					weapon = weapon
				};

				self.World.Add(args.weapon.Projectile.Create(args));

				if (!string.IsNullOrEmpty(args.weapon.Report))
					Sound.Play(args.weapon.Report + ".aud", self.CenterLocation);
			}
		}
	}
}
