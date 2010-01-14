using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.GameRules;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	static class Exts
	{
		public static bool HasModifier(this Modifiers k, Modifiers mod)
		{
			return (k & mod) == mod;
		}

		public static IEnumerable<T> SymmetricDifference<T>(this IEnumerable<T> xs, IEnumerable<T> ys)
		{
			// this is probably a shockingly-slow way to do this, but it's concise.
			return xs.Except(ys).Concat(ys.Except(xs));
		}

		public static float Product(this IEnumerable<float> xs)
		{
			return xs.Aggregate(1f, (a, x) => a * x);
		}

		public static WeaponInfo GetPrimaryWeapon(this Actor self)
		{
			var info = self.Info.Traits.GetOrDefault<AttackBaseInfo>();
			if (info == null) return null;
			
			var weapon = info.PrimaryWeapon;
			if (weapon == null) return null;

			return Rules.WeaponInfo[weapon];
		}

		public static WeaponInfo GetSecondaryWeapon(this Actor self)
		{
			var info = self.Info.Traits.GetOrDefault<AttackBaseInfo>();
			if (info == null) return null;

			var weapon = info.SecondaryWeapon;
			if (weapon == null) return null;

			return Rules.WeaponInfo[weapon];
		}

		public static int GetMaxHP(this Actor self)
		{
			var oai = self.Info.Traits.GetOrDefault<OwnedActorInfo>();
			if (oai == null) return 0;
			return oai.HP;
		}
	}
}
