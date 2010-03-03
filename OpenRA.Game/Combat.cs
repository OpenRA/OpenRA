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

using System;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA
{
	static class Combat			/* some utility bits that are shared between various things */
	{
		public static void DoImpact(int2 loc, int2 visualLoc,
			WeaponInfo weapon, ProjectileInfo projectile, WarheadInfo warhead, Actor firedBy)
		{
			var world = firedBy.World;

			var targetTile = ((1f / Game.CellSize) * loc.ToFloat2()).ToInt2();

			var isWater = world.IsWater(targetTile);
			var hitWater = world.IsCellBuildable(targetTile, UnitMovementType.Float);

			if (warhead.Explosion != 0)
				world.AddFrameEndTask(
					w => w.Add(new Explosion(w, visualLoc, warhead.Explosion, hitWater)));

			var impactSound = warhead.ImpactSound;
			if (hitWater && warhead.WaterImpactSound != null)
				impactSound = warhead.WaterImpactSound;
			if (impactSound != null) Sound.Play(impactSound + ".aud");

			if (!isWater) world.Map.AddSmudge(targetTile, warhead);
			if (warhead.Ore)
				world.WorldActor.traits.Get<ResourceLayer>().Destroy(targetTile);

			var firepowerModifier = firedBy.traits
				.WithInterface<IFirepowerModifier>()
				.Select(a => a.GetFirepowerModifier())
				.Product();

			var maxSpread = GetMaximumSpread(weapon, warhead, firepowerModifier);
			var hitActors = world.FindUnitsInCircle(loc, maxSpread);
			
			foreach (var victim in hitActors)
				victim.InflictDamage(firedBy, 
					(int)GetDamageToInflict(victim, loc, weapon, warhead, firepowerModifier), warhead);
		}

		static float GetMaximumSpread(WeaponInfo weapon, WarheadInfo warhead, float modifier)
		{
			return (int)(warhead.Spread * Math.Log(Math.Abs(weapon.Damage * modifier), 2));
		}

		static float GetDamageToInflict(Actor target, int2 loc, WeaponInfo weapon, WarheadInfo warhead, float modifier)
		{
			if (!WeaponValidForTarget(weapon, target))
				return 0f;
			
			var distance = (target.CenterLocation - loc).Length*1/24f;
			var rawDamage = weapon.Damage * modifier * (float)Math.Exp(-distance / warhead.Spread);
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
