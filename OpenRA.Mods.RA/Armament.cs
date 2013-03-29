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
	public enum CoordinateModel { Legacy, World };

	public class Barrel
	{
		// Legacy coordinates
		public PVecInt TurretSpaceOffset;	// position in turret space
		public PVecInt ScreenSpaceOffset;	// screen-space hack to make things line up good.
		public int Facing;					// deviation from turret facing

		// World coordinates
		public WVec Offset;
		public WAngle Yaw;
	}

	[Desc("Allows you to attach weapons to the unit (use @IdentifierSuffix for > 1)")]
	public class ArmamentInfo : ITraitInfo, Requires<AttackBaseInfo>
	{
		[WeaponReference]
		[Desc("Has to be defined here and in weapons.yaml.")]
		public readonly string Weapon = null;
		public readonly string Turret = "primary";
		[Desc("Move the turret backwards when firing.")] 
		public readonly int LegacyRecoil = 0;
		[Desc("Time (in frames) until the weapon can fire again.")] 
		public readonly int FireDelay = 0;

		public readonly float LegacyRecoilRecovery = 0.2f;
		public readonly int[] LegacyLocalOffset = { };

		public CoordinateModel OffsetModel = CoordinateModel.Legacy;
		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WRange[] LocalOffset = {};
		[Desc("Muzzle yaw relative to turret or body.")]
		public readonly WAngle[] LocalYaw = {};
		[Desc("Move the turret backwards when firing.")]
		public readonly WRange Recoil = WRange.Zero;
		[Desc("Recoil recovery per-frame")]
		public readonly WRange RecoilRecovery = new WRange(9);

		public object Create(ActorInitializer init)
		{
			// Auto-detect coordinate type
			if (LocalOffset.Length > 0 && OffsetModel == CoordinateModel.Legacy)
				OffsetModel = CoordinateModel.World;

			return new Armament(init.self, this);
		}
	}

	public class Armament : ITick
	{
		public readonly ArmamentInfo Info;
		public readonly WeaponInfo Weapon;
		public readonly Barrel[] Barrels;
		Lazy<Turreted> Turret;
		Lazy<ILocalCoordinatesModel> Coords;

		public WRange Recoil;
		public float LegacyRecoil { get; private set; }
		public int FireDelay { get; private set; }
		public int Burst { get; private set; }

		public Armament(Actor self, ArmamentInfo info)
		{
			Info = info;

			// We can't resolve these until runtime
			Turret = Lazy.New(() => self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == info.Turret));
			Coords = Lazy.New(() => self.Trait<ILocalCoordinatesModel>());

			Weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()];
			Burst = Weapon.Burst;

			var barrels = new List<Barrel>();

			if (Info.OffsetModel == CoordinateModel.Legacy)
			{
				for (var i = 0; i < info.LocalOffset.Length / 5; i++)
					barrels.Add(new Barrel
					{
						TurretSpaceOffset = new PVecInt(info.LegacyLocalOffset[5 * i], info.LegacyLocalOffset[5 * i + 1]),
						ScreenSpaceOffset = new PVecInt(info.LegacyLocalOffset[5 * i + 2], info.LegacyLocalOffset[5 * i + 3]),
						Facing = info.LegacyLocalOffset[5 * i + 4],
					});

				// if no barrels specified, the default is "turret position; turret facing".
				if (barrels.Count == 0)
					barrels.Add(new Barrel { TurretSpaceOffset = PVecInt.Zero, ScreenSpaceOffset = PVecInt.Zero, Facing = 0 });
			}
			else
			{
				if (info.LocalOffset.Length % 3 != 0)
					throw new InvalidOperationException("Invalid LocalOffset array length");

				for (var i = 0; i < info.LocalOffset.Length / 3; i++)
				{
					barrels.Add(new Barrel
					{
						Offset = new WVec(info.LocalOffset[3*i], info.LocalOffset[3*i + 1], info.LocalOffset[3*i + 2]),
						Yaw = info.LocalYaw.Length > i ? info.LocalYaw[i] : WAngle.Zero
					});
				}
				if (barrels.Count == 0)
					barrels.Add(new Barrel { Offset = WVec.Zero, Yaw = WAngle.Zero });
			}
			Barrels = barrels.ToArray();
		}

		public void Tick(Actor self)
		{
			if (FireDelay > 0)
				--FireDelay;
			LegacyRecoil = Math.Max(0f, LegacyRecoil - Info.LegacyRecoilRecovery);
			Recoil = new WRange(Math.Max(0, Recoil.Range - Info.RecoilRecovery.Range));
		}

		// Note: facing is only used by the legacy positioning code
		// The world coordinate model uses Actor.Orientation
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

			var legacyMuzzlePosition = self.CenterLocation + (PVecInt)MuzzlePxOffset(self, facing, barrel).ToInt2();
			var legacyMuzzleAltitude = move != null ? move.Altitude : 0;
			var legacyFacing = barrel.Facing + (Turret.Value != null ? Turret.Value.turretFacing :
				facing != null ? facing.Facing : Util.GetFacing(target.CenterLocation - self.CenterLocation, 0));

			if (Info.OffsetModel == CoordinateModel.World)
			{
				var muzzlePosition = self.CenterPosition + MuzzleOffset(self, barrel);
				legacyMuzzlePosition = PPos.FromWPos(muzzlePosition);
				legacyMuzzleAltitude = Game.CellSize*muzzlePosition.Z/1024;

				legacyFacing = MuzzleOrientation(self, barrel).Yaw.Angle / 4;
			}

			var args = new ProjectileArgs
			{
				weapon = Weapon,
				firedBy = self,
				target = target,
				src = legacyMuzzlePosition,
				srcAltitude = legacyMuzzleAltitude,

				dest = target.CenterLocation,
				destAltitude = destMove != null ? destMove.Altitude : 0,

				facing = legacyFacing,

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

			LegacyRecoil = Info.LegacyRecoil;
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

		// Note: facing is only used by the legacy positioning code
		public PVecFloat MuzzlePxOffset(Actor self, IFacing facing, Barrel b)
		{
			// Hack for external code unaware of world coordinates
			if (Info.OffsetModel == CoordinateModel.World)
				return (PVecFloat)PPos.FromWPosHackZ(WPos.Zero + MuzzleOffset(self, b)).ToFloat2();

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

		public WVec MuzzleOffset(Actor self, Barrel b)
		{
			if (Info.OffsetModel != CoordinateModel.World)
				throw new InvalidOperationException("Armament.MuzzlePosition requires a world coordinate offset");

			var bodyOrientation = Coords.Value.QuantizeOrientation(self, self.Orientation);
			var localOffset = b.Offset + new WVec(-Recoil, WRange.Zero, WRange.Zero);
			if (Turret.Value != null)
			{
				var turretOrientation = Coords.Value.QuantizeOrientation(self, Turret.Value.LocalOrientation(self));
				localOffset = localOffset.Rotate(turretOrientation);
				localOffset += Turret.Value.Offset;
			}

			return Coords.Value.LocalToWorld(localOffset.Rotate(bodyOrientation));
		}

		public WRot MuzzleOrientation(Actor self, Barrel b)
		{
			var orientation = self.Orientation + WRot.FromYaw(b.Yaw);
			if (Turret.Value != null)
				orientation += Turret.Value.LocalOrientation(self);
			return orientation;
		}
	}
}
