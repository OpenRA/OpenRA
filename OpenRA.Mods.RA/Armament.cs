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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class Barrel
	{
		public PVecInt TurretSpaceOffset;	// position in turret space
		public PVecInt ScreenSpaceOffset;	// screen-space hack to make things line up good.
		public int Facing;					// deviation from turret facing
	}

	[Desc("Allows you to attach weapons to the unit (use @IdentifierSuffix for > 1)")]
	public class ArmamentInfo : ITraitInfo, Requires<AttackBaseInfo>
	{
		[WeaponReference]
		[Desc("Has to be defined here and in weapons.yaml.")]
		public readonly string Weapon = null;
		public readonly string Turret = "primary";
		[Desc("Move the turret backwards when firing.")] 
		public readonly int Recoil = 0;
		[Desc("Time (in frames) until the weapon can fire again.")] 
		public readonly int FireDelay = 0;

		public readonly float RecoilRecovery = 0.2f;
		public readonly int[] LocalOffset = { };

		public object Create(ActorInitializer init) { return new Armament(init.self, this); }
	}

	public class Armament : ITick
	{
		public readonly ArmamentInfo Info;
		public readonly WeaponInfo Weapon;
		public readonly Barrel[] Barrels;
		Lazy<Turreted> Turret;

		public float Recoil { get; private set; }
		public int FireDelay { get; private set; }
		public int Burst { get; private set; }

		public Armament(Actor self, ArmamentInfo info)
		{
			Info = info;

			// We can't soft-depend on TraitInfo, so we have to wait
			// until runtime to cache this
			Turret = Lazy.New(() => self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.info.Turret == info.Turret));

			Weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()];
			Burst = Weapon.Burst;

			var barrels = new List<Barrel>();
			for (var i = 0; i < info.LocalOffset.Length / 5; i++)
				barrels.Add(new Barrel
				{
					TurretSpaceOffset = new PVecInt(info.LocalOffset[5 * i], info.LocalOffset[5 * i + 1]),
					ScreenSpaceOffset = new PVecInt(info.LocalOffset[5 * i + 2], info.LocalOffset[5 * i + 3]),
					Facing = info.LocalOffset[5 * i + 4],
				});

			// if no barrels specified, the default is "turret position; turret facing".
			if (barrels.Count == 0)
				barrels.Add(new Barrel { TurretSpaceOffset = PVecInt.Zero, ScreenSpaceOffset = PVecInt.Zero, Facing = 0 });

			Barrels = barrels.ToArray();
		}

		public void Tick(Actor self)
		{
			if (FireDelay > 0)
				--FireDelay;
			Recoil = Math.Max(0f, Recoil - Info.RecoilRecovery);
		}

		public void CheckFire(Actor self, AttackBase attack, IMove move, IFacing facing, Target target)
		{
			if (FireDelay > 0) return;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return;

			if (!Combat.IsInRange(self.CenterLocation, Weapon.Range, target)) return;
			if (Combat.IsInRange(self.CenterLocation, Weapon.MinRange, target)) return;
			if (!IsValidAgainst(self.World, target)) return;

			var barrel = Barrels[Burst % Barrels.Length];
			var destMove = target.IsActor ? target.Actor.TraitOrDefault<IMove>() : null;

			var args = new ProjectileArgs
			{
				weapon = Weapon,
				firedBy = self,
				target = target,

				src = (self.CenterLocation + (PVecInt)MuzzlePxPosition(self, facing, barrel).ToInt2()),
				srcAltitude = move != null ? move.Altitude : 0,
				dest = target.CenterLocation,
				destAltitude = destMove != null ? destMove.Altitude : 0,

				facing = barrel.Facing +
				(Turret.Value != null ? Turret.Value.turretFacing :
					facing != null ? facing.Facing : Util.GetFacing(target.CenterLocation - self.CenterLocation, 0)),

				firepowerModifier = self.TraitsImplementing<IFirepowerModifier>()
					.Select(a => a.GetFirepowerModifier())
					.Product()
			};

			attack.ScheduleDelayedAction(Info.FireDelay, () =>
			{
				if (args.weapon.Projectile != null)
				{
					var projectile = args.weapon.Projectile.Create(args);
					if (projectile != null)
						self.World.Add(projectile);

					if (args.weapon.Report != null && args.weapon.Report.Any())
						Sound.Play(args.weapon.Report.Random(self.World.SharedRandom) + ".aud", self.CenterLocation);
				}
			});

			foreach (var na in self.TraitsImplementing<INotifyAttack>())
				na.Attacking(self, target);

			Recoil = Info.Recoil;

			if (--Burst > 0)
				FireDelay = Weapon.BurstDelay;
			else
			{
				FireDelay = Weapon.ROF;
				Burst = Weapon.Burst;
			}
		}

		public bool IsValidAgainst(World world, Target target)
		{
			if (target.IsActor)
				return Combat.WeaponValidForTarget(Weapon, target.Actor);
			else
				return Combat.WeaponValidForTarget(Weapon, world, target.CenterLocation.ToCPos());
		}

		public bool IsReloading { get { return FireDelay > 0; } }

		PVecFloat GetUnitspaceBarrelOffset(Actor self, IFacing facing, Barrel b)
		{
			if (Turret.Value == null && facing == null)
				return PVecFloat.Zero;

			var turretFacing = Turret.Value != null ? Turret.Value.turretFacing : facing.Facing;
			return (PVecFloat)Util.RotateVectorByFacing(b.TurretSpaceOffset.ToFloat2(), turretFacing, .7f);
		}

		public PVecFloat MuzzlePxPosition(Actor self, IFacing facing, Barrel b)
		{
			PVecFloat pos = b.ScreenSpaceOffset;

			// local facing offset doesn't make sense for actors that don't rotate
			if (Turret.Value == null && facing == null)
				return pos;

			if (Turret.Value != null)
				pos += Turret.Value.PxPosition(self, facing);

			// Add local unitspace/turretspace offset
			var f = Turret.Value != null ? Turret.Value.turretFacing : facing.Facing;

			// This is going away, so no point adding unnecessary usings
			var ru = self.TraitOrDefault<RenderUnit>();
			var numDirs = (ru != null) ? ru.anim.CurrentSequence.Facings : 8;
			var quantizedFacing = Util.QuantizeFacing(f, numDirs) * (256 / numDirs);

			pos += (PVecFloat)Util.RotateVectorByFacing(b.TurretSpaceOffset.ToFloat2(), quantizedFacing, .7f);
			return pos;
		}

		public PVecFloat RecoilPxOffset(Actor self, int facing)
		{
			var localRecoil = new float2(0, Recoil);
			return (PVecFloat)Util.RotateVectorByFacing(localRecoil, facing, .7f);
		}
	}
}
