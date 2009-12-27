using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using OpenRa.Game.Effects;
using OpenRa.Game.Traits;

namespace OpenRa.Game
{
	static class Combat			/* some utility bits that are shared between various things */
	{
		public static void DoImpact(int2 loc, int2 visualLoc,
			WeaponInfo weapon, ProjectileInfo projectile, WarheadInfo warhead, Actor firedBy)
		{
			var targetTile = ((1f / Game.CellSize) * loc.ToFloat2()).ToInt2();

			var isWater = Game.IsWater(targetTile);
			var hitWater = Game.IsCellBuildable(targetTile, UnitMovementType.Float);

			if (warhead.Explosion != 0)
				Game.world.AddFrameEndTask(
					w => w.Add(new Explosion(visualLoc, warhead.Explosion, hitWater)));

			var impactSound = warhead.ImpactSound;
			if (hitWater && warhead.WaterImpactSound != null)
				impactSound = warhead.WaterImpactSound;
			if (impactSound != null) Sound.Play(impactSound + ".aud");

			if (!isWater) Smudge.AddSmudge(targetTile, warhead);
			if (warhead.Ore) Ore.Destroy(targetTile.X, targetTile.Y);

			var maxSpread = GetMaximumSpread(weapon, warhead);
			var hitActors = Game.FindUnitsInCircle(loc, maxSpread);

			foreach (var victim in hitActors)
				victim.InflictDamage(firedBy, (int)GetDamageToInflict(victim, loc, weapon, warhead), warhead);
		}

		static float GetMaximumSpread(WeaponInfo weapon, WarheadInfo warhead)
		{
			return (int)(warhead.Spread * Math.Log(Math.Abs(weapon.Damage), 2));
		}

		static float GetDamageToInflict(Actor target, int2 loc, WeaponInfo weapon, WarheadInfo warhead)
		{
			/* todo: some things can't be damaged AT ALL by certain weapons! */
			var distance = (target.CenterLocation - loc).Length;
			var rawDamage = weapon.Damage * (float)Math.Exp(-distance / warhead.Spread);
			var multiplier = warhead.EffectivenessAgainst(target.Info.Armor);

			return rawDamage * multiplier;
		}

		public static bool WeaponValidForTarget(WeaponInfo weapon, Actor target)
		{
			var projectile = Rules.ProjectileInfo[weapon.Projectile];
			var unit = target.traits.GetOrDefault<Unit>();

			if (target.traits.Contains<Submarine>())
				return projectile.ASW;

			if (unit != null && unit.Altitude > 0)
				return projectile.AA;

			if (projectile.UnderWater && !target.Info.WaterBound)
				return false;

			return projectile.AG;
		}

		public static bool HasAnyValidWeapons(Actor self, Actor target)
		{
			if (self.Info.Primary != null &&
				WeaponValidForTarget(Rules.WeaponInfo[self.Info.Primary], target)) return true;
			if (self.Info.Secondary != null &&
				WeaponValidForTarget(Rules.WeaponInfo[self.Info.Secondary], target)) return true;

			return false;
		}
	}
}
