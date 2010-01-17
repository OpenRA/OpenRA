using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.GameRules;
using OpenRa.Effects;
using OpenRa.Traits;

namespace OpenRa
{
	static class Combat			/* some utility bits that are shared between various things */
	{
		public static void DoImpact(int2 loc, int2 visualLoc,
			WeaponInfo weapon, ProjectileInfo projectile, WarheadInfo warhead, Actor firedBy)
		{
			var targetTile = ((1f / Game.CellSize) * loc.ToFloat2()).ToInt2();

			var isWater = Game.world.IsWater(targetTile);
			var hitWater = Game.world.IsCellBuildable(targetTile, UnitMovementType.Float);

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
			var hitActors = Game.world.FindUnitsInCircle(loc, maxSpread);
			
			foreach (var victim in hitActors)
				victim.InflictDamage(firedBy, (int)GetDamageToInflict(victim, loc, weapon, warhead), warhead);
		}

		static float GetMaximumSpread(WeaponInfo weapon, WarheadInfo warhead)
		{
			return (int)(warhead.Spread * Math.Log(Math.Abs(weapon.Damage), 2));
		}

		static float GetDamageToInflict(Actor target, int2 loc, WeaponInfo weapon, WarheadInfo warhead)
		{
			if (!WeaponValidForTarget(weapon, target))
				return 0f;
			
			var distance = (target.CenterLocation - loc).Length*1/24f;
			var rawDamage = weapon.Damage * (float)Math.Exp(-distance / warhead.Spread);
			var multiplier = warhead.EffectivenessAgainst(target.Info.Traits.Get<OwnedActorInfo>().Armor);
			return rawDamage * multiplier;
		}

		public static bool WeaponValidForTarget(WeaponInfo weapon, Actor target)
		{
			var projectile = Rules.ProjectileInfo[weapon.Projectile];
			var warhead = Rules.WarheadInfo[weapon.Warhead];
			var unit = target.traits.GetOrDefault<Unit>();

			if (warhead.EffectivenessAgainst(target.Info.Traits.Get<OwnedActorInfo>().Armor) <= 0)
				return false;

			if (target.traits.Contains<Submarine>())
				return projectile.ASW;

			if (unit != null && unit.Altitude > 0)
				return projectile.AA;

			if (projectile.UnderWater && !target.Info.Traits.Get<OwnedActorInfo>().WaterBound)
				return false;

			return projectile.AG;
		}

		public static bool HasAnyValidWeapons(Actor self, Actor target)
		{
			var info = self.Info.Traits.Get<AttackBaseInfo>();
			if (info.PrimaryWeapon != null &&
				WeaponValidForTarget(self.GetPrimaryWeapon(), target)) return true;
			if (info.SecondaryWeapon != null &&
				WeaponValidForTarget(self.GetSecondaryWeapon(), target)) return true;

			return false;
		}
	}
}
