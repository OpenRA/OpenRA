#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.RA.Render;
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

			var isWater = pos.Z == 0 && world.GetTerrainInfo(targetTile).IsWater;
			var explosionType = isWater ? warhead.WaterExplosion : warhead.Explosion;

			if (explosionType != null)
				world.AddFrameEndTask(w => w.Add(new Explosion(w, pos, explosionType)));

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
						var maxSpread = warhead.Spread * (float)Math.Log(Math.Abs(warhead.Damage), 2);
						var range = new WRange((int)maxSpread * 1024 / Game.CellSize);
						var hitActors = world.FindActorsInCircle(pos, range);

						foreach (var victim in hitActors)
						{
							var damage = (int)GetDamageToInflict(pos, victim, warhead, weapon, firepowerModifier);
							victim.InflictDamage(firedBy, damage, warhead);
						}
					} break;

				case DamageModel.PerCell:
					{
						foreach (var t in world.FindTilesInCircle(targetTile, warhead.Size[0]))
							foreach (var unit in world.FindActorsInBox(t, t))
								unit.InflictDamage(firedBy,
									(int)(warhead.Damage * warhead.EffectivenessAgainst(unit.Info)), warhead);
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
			var weapon = Rules.Weapons[weapontype.ToLowerInvariant()];
			if (weapon.Report != null && weapon.Report.Any())
				Sound.Play(weapon.Report.Random(attacker.World.SharedRandom), pos);

			DoImpacts(pos, attacker, weapon, 1f);
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

		static float GetDamageToInflict(WPos pos, Actor target, WarheadInfo warhead, WeaponInfo weapon, float modifier)
		{
			// don't hit air units with splash from ground explosions, etc
			if (!weapon.IsValidAgainst(target))
				return 0f;

			var health = target.Info.Traits.GetOrDefault<HealthInfo>();
			if( health == null ) return 0f;

			var distance = (int)Math.Max(0, (target.CenterPosition - pos).Length * Game.CellSize / 1024 - health.Radius);
			var falloff = (float)GetDamageFalloff(distance / warhead.Spread);
			var rawDamage = (float)(warhead.Damage * modifier * falloff);
			var multiplier = (float)warhead.EffectivenessAgainst(target.Info);

			return (float)(rawDamage * multiplier);
		}
	}
}
