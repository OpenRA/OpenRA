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
		public WVec Offset;
		public WAngle Yaw;
	}

	[Desc("Allows you to attach weapons to the unit (use @IdentifierSuffix for > 1)")]
	public class ArmamentInfo : ITraitInfo, Requires<AttackBaseInfo>
	{
		public readonly string Name = "primary";

		[WeaponReference]
		[Desc("Has to be defined here and in weapons.yaml.")]
		public readonly string Weapon = null;
		public readonly string Turret = "primary";
		[Desc("Time (in frames) until the weapon can fire again.")] 
		public readonly int FireDelay = 0;

		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WRange[] LocalOffset = {};
		[Desc("Muzzle yaw relative to turret or body.")]
		public readonly WAngle[] LocalYaw = {};
		[Desc("Move the turret backwards when firing.")]
		public readonly WRange Recoil = WRange.Zero;
		[Desc("Recoil recovery per-frame")]
		public readonly WRange RecoilRecovery = new WRange(9);

		public object Create(ActorInitializer init) { return new Armament(init.self, this); }
	}

	public class Armament : ITick
	{
		public readonly ArmamentInfo Info;
		public readonly WeaponInfo Weapon;
		public readonly Barrel[] Barrels;
		Lazy<Turreted> Turret;
		Lazy<IBodyOrientation> Coords;

		public WRange Recoil;
		public int FireDelay { get; private set; }
		public int Burst { get; private set; }

		public Armament(Actor self, ArmamentInfo info)
		{
			Info = info;

			// We can't resolve these until runtime
			Turret = Lazy.New(() => self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == info.Turret));
			Coords = Lazy.New(() => self.Trait<IBodyOrientation>());

			Weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()];
			Burst = Weapon.Burst;

			if (info.LocalOffset.Length % 3 != 0)
				throw new InvalidOperationException("Invalid LocalOffset array length");

			var barrels = new List<Barrel>();
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

			Barrels = barrels.ToArray();
		}

		public void Tick(Actor self)
		{
			if (FireDelay > 0)
				--FireDelay;
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

			var muzzlePosition = self.CenterPosition + MuzzleOffset(self, barrel);
			var legacyMuzzlePosition = PPos.FromWPos(muzzlePosition);
			var legacyMuzzleAltitude = Game.CellSize*muzzlePosition.Z/1024;
			var legacyFacing = MuzzleOrientation(self, barrel).Yaw.Angle / 4;

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
						Sound.Play(args.weapon.Report.Random(self.World.SharedRandom), self.CenterLocation);
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

		public WVec MuzzleOffset(Actor self, Barrel b)
		{
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
