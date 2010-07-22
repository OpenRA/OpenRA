#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public static class Combat			/* some utility bits that are shared between various things */
	{
		static string GetImpactSound(WarheadInfo warhead, bool isWater)
		{
			if (isWater && warhead.WaterImpactSound != null)
				return warhead.WaterImpactSound + ".aud";

			if (warhead.ImpactSound != null)
				return warhead.ImpactSound + ".aud";

			return null;
		}

		public static void DoImpact(WarheadInfo warhead, ProjectileArgs args)
		{
			var world = args.firedBy.World;
			var targetTile = Util.CellContaining(args.dest);

			if (!world.Map.IsInMap(targetTile))
				return;

			var isWater = world.GetTerrainInfo(targetTile).IsWater;
			var explosionType = isWater ? warhead.WaterExplosion : warhead.Explosion;

			if (explosionType != null)
				world.AddFrameEndTask(
					w => w.Add(new Explosion(w, args.dest, explosionType, isWater)));

			Sound.Play(GetImpactSound(warhead, isWater), args.dest);
			
			if (warhead.SmudgeType != null)
			{
				var smudgeLayer = world.WorldActor.traits.WithInterface<SmudgeLayer>()
					.FirstOrDefault(x => x.Info.Type == warhead.SmudgeType);
				if (smudgeLayer == null)
					throw new NotImplementedException("Unknown smudge type `{0}`".F(warhead.SmudgeType));

				if (warhead.Size[0] > 0)
				{
					var smudgeCells = world.FindTilesInCircle(targetTile, warhead.Size[0])
						.Except(world.FindTilesInCircle(targetTile, warhead.Size[1]));

					foreach (var sc in smudgeCells)
						smudgeLayer.AddSmudge(sc);
				}
				else
					smudgeLayer.AddSmudge(targetTile);
			}
			
			if (warhead.Ore)
				world.WorldActor.traits.Get<ResourceLayer>().Destroy(targetTile);

			var firepowerModifier = args.firedBy.traits
				.WithInterface<IFirepowerModifier>()
				.Select(a => a.GetFirepowerModifier())
				.Product();

			switch (warhead.DamageModel)
			{
				case DamageModel.Normal:
					{
						var maxSpread = warhead.Spread * (float)Math.Log(Math.Abs(warhead.Damage), 2);
						var hitActors = world.FindUnitsInCircle(args.dest, maxSpread);

						foreach (var victim in hitActors)
						{
							var damage = (int)GetDamageToInflict(victim, args, warhead, firepowerModifier);
							victim.InflictDamage(args.firedBy, damage, warhead);
						}
					} break;

				case DamageModel.PerCell:
					{
						foreach (var t in world.FindTilesInCircle(targetTile, warhead.Size[0]))
							foreach (var unit in world.FindUnits(Game.CellSize * t, Game.CellSize * (t + new float2(1,1))))
								unit.InflictDamage(args.firedBy,
									(int)(warhead.Damage * warhead.EffectivenessAgainst(
									unit.Info.Traits.Get<OwnedActorInfo>().Armor)), warhead);
					} break;
			}
		}

		public static void DoImpacts(ProjectileArgs args)
		{
			foreach (var warhead in args.weapon.Warheads)
			{
				Action a = () => DoImpact(warhead, args);
				if (warhead.Delay > 0)
					args.firedBy.World.AddFrameEndTask(
						w => w.Add(new DelayedAction(warhead.Delay, a)));
				else
					a();
			}
		}

		public static void DoExplosion(Actor attacker, string weapontype, Target _target, int altitude)
		{
			var args = new ProjectileArgs
			{
				src = Util.CellContaining(_target.CenterLocation),
				dest = Util.CellContaining(_target.CenterLocation),
				srcAltitude = altitude,
				destAltitude = altitude,
				firedBy = attacker,
				target = _target,
				weapon = Rules.Weapons[ weapontype.ToLowerInvariant() ],
				facing = 0
			};

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
			if (!WeaponValidForTarget(args.weapon, Target.FromActor(target))) return 0f;

			var selectable = target.Info.Traits.GetOrDefault<SelectableInfo>();
			var radius = selectable != null ? selectable.Radius : 0;
			var distance = (int)Math.Max(0, (target.CenterLocation - args.dest).Length - radius);
			var falloff = (float)GetDamageFalloff(distance / warhead.Spread);
			var rawDamage = (float)(warhead.Damage * modifier * falloff);
			var multiplier = (float)warhead.EffectivenessAgainst(target.Info.Traits.Get<OwnedActorInfo>().Armor);

			return (float)(rawDamage * multiplier);
		}

		public static bool WeaponValidForTarget(WeaponInfo weapon, Target target)
		{
			// todo: fix this properly.
			if (!target.IsValid) return false;
			if (!target.IsActor) return weapon.ValidTargets.Contains("Ground");		// hack!

			var ownedInfo = target.Actor.Info.Traits.GetOrDefault<OwnedActorInfo>();

			if (!weapon.ValidTargets.Contains(ownedInfo.TargetType))
				return false;

			if (weapon.Warheads.All( w => w.EffectivenessAgainst(ownedInfo.Armor) <= 0))
				return false;

			if (weapon.Underwater && !ownedInfo.WaterBound)
				return false;

			return true;
		}

		public static bool HasAnyValidWeapons(Actor self, Target target)
		{
			var info = self.Info.Traits.Get<AttackBaseInfo>();
			if (info.PrimaryWeapon != null &&
				WeaponValidForTarget(self.GetPrimaryWeapon(), target)) return true;
			if (info.SecondaryWeapon != null &&
				WeaponValidForTarget(self.GetSecondaryWeapon(), target)) return true;

			return false;
		}

		public static float GetMaximumRange(Actor self)
		{
			return new[] { self.GetPrimaryWeapon(), self.GetSecondaryWeapon() }
				.Where(w => w != null).Max(w => w.Range);
		}

		public static WeaponInfo GetPrimaryWeapon(this Actor self)
		{
			var info = self.Info.Traits.GetOrDefault<AttackBaseInfo>();
			if (info == null) return null;
			
			var weapon = info.PrimaryWeapon;
			if (weapon == null) return null;

			return Rules.Weapons[weapon.ToLowerInvariant()];
		}

		public static WeaponInfo GetSecondaryWeapon(this Actor self)
		{
			var info = self.Info.Traits.GetOrDefault<AttackBaseInfo>();
			if (info == null) return null;

			var weapon = info.SecondaryWeapon;
			if (weapon == null) return null;

			return Rules.Weapons[weapon.ToLowerInvariant()];
		}

		static float2 GetRecoil(Actor self, float recoil)
		{
			var abInfo = self.Info.Traits.GetOrDefault<AttackBaseInfo>();
			if (abInfo == null || abInfo.Recoil == 0) return float2.Zero;
			var rut = self.traits.GetOrDefault<RenderUnitTurreted>();
			if (rut == null) return float2.Zero;

			var facing = self.traits.Get<Turreted>().turretFacing;
			return Util.RotateVectorByFacing(new float2(0, recoil * self.Info.Traits.Get<AttackBaseInfo>().Recoil), facing, .7f);
		}

		public static float2 GetTurretPosition(Actor self, Unit unit, int[] offset, float recoil)
		{
			if( unit == null ) return offset.AbsOffset();	/* things that don't have a rotating base don't need the turrets repositioned */

			var ru = self.traits.GetOrDefault<RenderUnit>();
			var numDirs = (ru != null) ? ru.anim.CurrentSequence.Facings : 8;
			var bodyFacing = unit.Facing;
			var quantizedFacing = Util.QuantizeFacing(bodyFacing, numDirs) * (256 / numDirs);

			return (Util.RotateVectorByFacing(offset.RelOffset(), quantizedFacing, .7f) + GetRecoil(self, recoil))
				+ offset.AbsOffset();
		}
	}
}
