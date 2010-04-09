using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.Traits;
using OpenRA.GameRules;

namespace OpenRA.Mods.Cnc
{
	class CarpetBombInfo : ITraitInfo
	{
		public readonly string Weapon = null;
		public readonly int Interval = 0;
		public readonly int Range = 0;

		public object Create(Actor self) { return new CarpetBomb(self); }
	}

	class CarpetBomb : ITick
	{
		int2 Target;
		int dropDelay;

		public CarpetBomb(Actor self) { }

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
				dropDelay = info.Interval;

				var args = new ProjectileArgs
				{
					srcAltitude = self.traits.Get<Unit>().Altitude,
					destAltitude = 0,
					src = self.CenterLocation.ToInt2(),
					dest = self.CenterLocation.ToInt2(),
					facing = self.traits.Get<Unit>().Facing,
					firedBy = self,
					weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()]
				};

				self.World.Add(args.weapon.Projectile.Create(args));
			}
		}
	}
}
