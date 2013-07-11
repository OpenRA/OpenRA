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

		public static void DoImpact(WarheadInfo warhead, ProjectileArgs args)
		{
			var world = args.firedBy.World;
			var targetTile = args.dest.ToCPos();

			if (!world.Map.IsInMap(targetTile))
				return;

			var isWater = args.destAltitude == 0 && world.GetTerrainInfo(targetTile).IsWater;
			var explosionType = isWater ? warhead.WaterExplosion : warhead.Explosion;

			var dest = args.dest.ToWPos(args.destAltitude);
			if (explosionType != null)
				world.AddFrameEndTask(
					w => w.Add(new Explosion(w, dest, explosionType)));

			Sound.Play(GetImpactSound(warhead, isWater), dest);

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
						var hitActors = world.FindUnitsInCircle(args.dest, (int)maxSpread);

						foreach (var victim in hitActors)
						{
							var damage = (int)GetDamageToInflict(victim, args, warhead, args.firepowerModifier);
							victim.InflictDamage(args.firedBy, damage, warhead);
						}
					} break;

				case DamageModel.PerCell:
					{
						foreach (var t in world.FindTilesInCircle(targetTile, warhead.Size[0]))
							foreach (var unit in world.FindUnits(t.ToPPos(), (t + new CVec(1,1)).ToPPos()))
								unit.InflictDamage(args.firedBy,
									(int)(warhead.Damage * warhead.EffectivenessAgainst(unit)), warhead);
					} break;
			}
		}

		public static void DoImpacts(ProjectileArgs args)
		{
			foreach (var warhead in args.weapon.Warheads)
			{
				// NOTE(jsd): Fixed access to modified closure bug!
				var warheadClosed = warhead;

				Action a = () => DoImpact(warheadClosed, args);
				if (warhead.Delay > 0)
					args.firedBy.World.AddFrameEndTask(
						w => w.Add(new DelayedAction(warheadClosed.Delay, a)));
				else
					a();
			}
		}

		public static void DoExplosion(Actor attacker, string weapontype, WPos pos)
		{
			var pxPos = PPos.FromWPos(pos);
			var altitude = pos.Z * Game.CellSize / 1024;
			var args = new ProjectileArgs
			{
				src = pxPos,
				dest = pxPos,
				srcAltitude = altitude,
				destAltitude = altitude,
				firedBy = attacker,
				target = Target.FromPos(pos),
				weapon = Rules.Weapons[weapontype.ToLowerInvariant()],
				facing = 0
			};

			if (args.weapon.Report != null && args.weapon.Report.Any())
				Sound.Play(args.weapon.Report.Random(attacker.World.SharedRandom), pos);

			DoImpacts(args);
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

		static float GetDamageToInflict(Actor target, ProjectileArgs args, WarheadInfo warhead, float modifier)
		{
			// don't hit air units with splash from ground explosions, etc
			if (!args.weapon.IsValidAgainst(target))
				return 0f;

			var health = target.Info.Traits.GetOrDefault<HealthInfo>();
			if( health == null ) return 0f;

			var distance = (int)Math.Max(0, (target.CenterLocation - args.dest).Length - health.Radius);
			var falloff = (float)GetDamageFalloff(distance / warhead.Spread);
			var rawDamage = (float)(warhead.Damage * modifier * falloff);
			var multiplier = (float)warhead.EffectivenessAgainst(target);

			return (float)(rawDamage * multiplier);
		}
	}
}
