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
using OpenRA.FileFormats;

namespace OpenRA
{
	static class Combat			/* some utility bits that are shared between various things */
	{
		static string GetImpactSound(WarheadInfo warhead, bool isWater)
		{
			if (isWater && warhead.WaterImpactSound != null)
				return warhead.WaterImpactSound + ".aud";

			if (warhead.ImpactSound != null)
				return warhead.ImpactSound + ".aud";

			return null;
		}

		public static void DoImpact(WarheadInfo warhead, ProjectileArgs args, int2 visualLocation)
		{
			var world = args.firedBy.World;
			var targetTile = ((1f / Game.CellSize) * args.dest.ToFloat2()).ToInt2();
			var isWater = world.GetTerrainType(targetTile) == TerrainType.Water;

			if (warhead.Explosion != 0)
				world.AddFrameEndTask(
					w => w.Add(new Explosion(w, visualLocation, warhead.Explosion, isWater)));

			Sound.Play(GetImpactSound(warhead, isWater));

			if (!isWater) world.Map.AddSmudge(targetTile, warhead);
			if (warhead.Ore)
				world.WorldActor.traits.Get<ResourceLayer>().Destroy(targetTile);

			var firepowerModifier = args.firedBy.traits
				.WithInterface<IFirepowerModifier>()
				.Select(a => a.GetFirepowerModifier())
				.Product();

			var maxSpread = warhead.Spread * (float)Math.Log(Math.Abs(warhead.Damage), 2);
			var hitActors = world.FindUnitsInCircle(args.dest, maxSpread);

			foreach (var victim in hitActors)
				victim.InflictDamage(args.firedBy,
					(int)GetDamageToInflict(victim, args, warhead, firepowerModifier), warhead);
		}

		public static void DoImpacts(ProjectileArgs args, int2 visualLocation)
		{
			foreach (var warhead in args.weapon.Warheads)
			{
				Action a = () => DoImpact(warhead, args, visualLocation);
				if (warhead.Delay > 0)
					args.firedBy.World.AddFrameEndTask(
						w => w.Add(new DelayedAction(warhead.Delay, a)));
				else
					a();
			}
		}

		static float GetDamageToInflict(Actor target, ProjectileArgs args, WarheadInfo warhead, float modifier)
		{
			var distance = (target.CenterLocation - args.dest).Length;
			var rawDamage = warhead.Damage * modifier * (float)Math.Exp(-distance / warhead.Spread);
			var multiplier = warhead.EffectivenessAgainst(target.Info.Traits.Get<OwnedActorInfo>().Armor);
			return rawDamage * multiplier;
		}

		public static bool WeaponValidForTarget(NewWeaponInfo weapon, Actor target)
		{
			return true;		// massive hack, and very wrong.

			/*var projectile = Rules.ProjectileInfo[weapon.Projectile];
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

			return projectile.AG;*/
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
