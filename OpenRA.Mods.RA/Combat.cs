#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using System.Collections.Generic;

namespace OpenRA.Mods.RA
{
	public static class Combat			/* some utility bits that are shared between various things */
	{
		static string GetImpactSound(WarheadInfo warhead, bool isWater)
		{
			if (isWater && warhead.WaterImpactSound != null)
				return warhead.WaterImpactSound;

			if (warhead.ImpactSound != null)
				return warhead.ImpactSound;

			return null;
		}

		public static void DoImpact(WPos pos, WarheadInfo warhead, WeaponInfo weapon, Actor firedBy, float firepowerModifier)
		{
			var world = firedBy.World;
			var targetTile = pos.ToCPos();

			if (!world.Map.IsInMap(targetTile))
				return;

			var isWater = pos.Z <= 0 && world.GetTerrainInfo(targetTile).IsWater;
			var explosionType = isWater ? warhead.WaterExplosion : warhead.Explosion;
			var explosionTypePalette = isWater ? warhead.WaterExplosionPalette : warhead.ExplosionPalette;

			if (explosionType != null)
				world.AddFrameEndTask(w => w.Add(new Explosion(w, pos, explosionType, explosionTypePalette)));

			Sound.Play(GetImpactSound(warhead, isWater), pos);

			var smudgeLayers = world.WorldActor.TraitsImplementing<SmudgeLayer>().ToDictionary(x => x.Info.Type);

			if (warhead.Size[0] > 0)
			{
				var resLayer = world.WorldActor.Trait<ResourceLayer>();
				var allCells = world.FindTilesInCircle(targetTile, warhead.Size[0]).ToList();

				// `smudgeCells` might want to just be an outer shell of the cells:
				IEnumerable<CPos> smudgeCells = allCells;
				if (warhead.Size.Length == 2)
					smudgeCells = smudgeCells.Except(world.FindTilesInCircle(targetTile, warhead.Size[1]));

				// Draw the smudges:
				foreach (var sc in smudgeCells)
				{
					var smudgeType = world.GetTerrainInfo(sc).AcceptsSmudgeType.FirstOrDefault(t => warhead.SmudgeType.Contains(t));
					if (smudgeType == null) continue;

					SmudgeLayer smudgeLayer;
					if (!smudgeLayers.TryGetValue(smudgeType, out smudgeLayer))
						throw new NotImplementedException("Unknown smudge type `{0}`".F(smudgeType));

					smudgeLayer.AddSmudge(sc);
					if (warhead.Ore)
						resLayer.Destroy(sc);
				}

				// Destroy all resources in range, not just the outer shell:
				foreach (var cell in allCells)
				{
					if (warhead.Ore)
						resLayer.Destroy(cell);
				}
			}
			else
			{
				var smudgeType = world.GetTerrainInfo(targetTile).AcceptsSmudgeType.FirstOrDefault(t => warhead.SmudgeType.Contains(t));
				if (smudgeType != null)
				{
					SmudgeLayer smudgeLayer;
					if (!smudgeLayers.TryGetValue(smudgeType, out smudgeLayer))
						throw new NotImplementedException("Unknown smudge type `{0}`".F(smudgeType));

					smudgeLayer.AddSmudge(targetTile);
				}
			}

			if (warhead.Ore)
				world.WorldActor.Trait<ResourceLayer>().Destroy(targetTile);

			switch (warhead.DamageModel)
			{
				case DamageModel.Normal:
					{
						var maxSpread = new WRange((int)(warhead.Spread.Range * (float)Math.Log(Math.Abs(warhead.Damage), 2)));
						var hitActors = world.FindActorsInCircle(pos, maxSpread);

						foreach (var victim in hitActors)
						{
							var damage = (int)GetDamageToInflict(pos, victim, warhead, weapon, firepowerModifier, true);
							victim.InflictDamage(firedBy, damage, warhead);
						}
					} break;

				case DamageModel.PerCell:
					{
						foreach (var t in world.FindTilesInCircle(targetTile, warhead.Size[0]))
							foreach (var unit in world.ActorMap.GetUnitsAt(t))
							{
								var damage = (int)GetDamageToInflict(pos, unit, warhead, weapon, firepowerModifier, false);
								unit.InflictDamage(firedBy, damage, warhead);
							}
					} break;

				case DamageModel.HealthPercentage:
					{
						var range = new WRange(warhead.Size[0] * 1024);
						var hitActors = world.FindActorsInCircle(pos, range);

						foreach (var victim in hitActors)
						{
							var damage = GetDamageToInflict(pos, victim, warhead, weapon, firepowerModifier, false);
							if (damage != 0) // will be 0 if the target doesn't have HealthInfo
							{
								var healthInfo = victim.Info.Traits.Get<HealthInfo>();
								damage = (float)(damage / 100 * healthInfo.HP);
							}
							victim.InflictDamage(firedBy, (int)damage, warhead);
						}
					} break;
			}
		}

		public static void DoImpacts(WPos pos, Actor firedBy, WeaponInfo weapon, float damageModifier)
		{
			foreach (var wh in weapon.Warheads)
			{
				var warhead = wh;
				Action a = () => DoImpact(pos, warhead, weapon, firedBy, damageModifier);

				if (warhead.Delay > 0)
					firedBy.World.AddFrameEndTask(
						w => w.Add(new DelayedAction(warhead.Delay, a)));
				else
					a();
			}
		}

		public static void DoExplosion(Actor attacker, string weapontype, WPos pos)
		{
			var weapon = attacker.World.Map.Rules.Weapons[weapontype.ToLowerInvariant()];
			if (weapon.Report != null && weapon.Report.Any())
				Sound.Play(weapon.Report.Random(attacker.World.SharedRandom), pos);

			DoImpacts(pos, attacker, weapon, 1f);
		}

		public static WRange FindSmudgeRange(IEnumerable<WarheadInfo> warheads)
		{
			int smudge = 0;
			foreach (var wh in warheads)
				smudge = Math.Max(wh.Size[0], smudge);
			return WRange.FromCells(smudge);
		}

		public static WRange FindDamageRange(IEnumerable<WarheadInfo> warheads, double damagePercentage)
		{
			var radii = new HashSet<int>() { 0 };
			var steps = Enumerable.Range(1, falloff.Length - 1);
			foreach (var wh in warheads)
				steps.Do(j => radii.Add(j * wh.Spread.Range));

			// Damage inflicted by the atom bomb, as a function of distance from the target position,
			// is a monotonically decreasing piecewise linear function. Let's call this function phi.
			// Dictionary<int, double> damage will be populated with pairs (radius, damage) where
			// radius is the distance at which there is a kink in the function phi and
			// damage is the value of phi at that distance.
			var damage = new Dictionary<int, double>();
			radii.Do(radius => damage[radius] = 0);
			foreach (var wh in warheads)
				steps.Do(j => radii.Where(radius => ((j - 1) * wh.Spread.Range <= radius) && (radius < j * wh.Spread.Range))
					.Do(radius => damage[radius] += wh.Damage *
						(Combat.falloff[j] * (radius - (j - 1) * wh.Spread.Range) + Combat.falloff[j - 1] * (j * wh.Spread.Range - radius)) /
						Convert.ToDouble(wh.Spread.Range)
					));

			// In order to find the exact distance at which phi = damagePercentage * damage.Values.Max(),
			// find the two kinks in phi at distances radiusUp and radiusDown such that
			// phi(radiusDown) <= damagePercentage * damage.Values.Max() <= phi(radiusUp)
			var radiusUp = damage.Where(radius => radius.Value >= damagePercentage * damage.Values.Max())
				.ToDictionary(radius => radius.Key).Keys.Max();
			var radiusDown = damage.Keys.Where(radius => radius > radiusUp).Min();

			// Since phi is linear between radiusDown and radiusUp,
			// the location where phi = damagePercentage * damage.Values.Max() is easily computable.
			return new WRange(Convert.ToInt32(
					(damagePercentage * damage.Values.Max() * (radiusDown - radiusUp) - damage[radiusUp] * radiusDown + damage[radiusDown] * radiusUp)
					/ (damage[radiusDown] - damage[radiusUp])
				));
		}

		static readonly float[] falloff =
		{
			1f, 0.3678795f, 0.1353353f, 0.04978707f,
			0.01831564f, 0.006737947f, 0.002478752f, 0.000911882f
		};

		static float GetDamageFalloff(float x)
		{
			var u = (int)x;
			if (u >= falloff.Length - 1) return 0;
			var t = x - u;
			return (falloff[u] * (1 - t)) + (falloff[u + 1] * t);
		}

		static float GetDamageToInflict(WPos pos, Actor target, WarheadInfo warhead, WeaponInfo weapon, float modifier, bool withFalloff)
		{
			// don't hit air units with splash from ground explosions, etc
			if (!weapon.IsValidAgainst(target))
				return 0;

			var healthInfo = target.Info.Traits.GetOrDefault<HealthInfo>();
			if (healthInfo == null)
				return 0;

			var rawDamage = (float)warhead.Damage;
			if (withFalloff)
			{
				var distance = Math.Max(0, (target.CenterPosition - pos).Length - healthInfo.Radius.Range);
				var falloff = (float)GetDamageFalloff(distance * 1f / warhead.Spread.Range);
				rawDamage = (float)(falloff * rawDamage);
			}
			return (float)(rawDamage * modifier * (float)warhead.EffectivenessAgainst(target.Info));
		}
	}
}
