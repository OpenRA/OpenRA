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
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class Barrel
	{
		public int2 Position;				// position in turret space
		public int Facing;					// deviation from turret facing
	}

	public class Turret
	{
		public float Recoil = 0.0f;			// remaining recoil fraction
		public int2 UnitSpacePosition;		// where, in the unit's local space.
		public int2 ScreenSpacePosition;	// screen-space hack to make things line up good.

		public Turret(int[] offset)
		{
			ScreenSpacePosition = offset.AbsOffset().ToInt2();
			UnitSpacePosition = offset.RelOffset().ToInt2();
		}
	}

	public class Weapon
	{
		public WeaponInfo Info;
		public int FireDelay = 0;			// time (in frames) until the weapon can fire again
		public int Burst = 0;				// burst counter

		public Barrel[] Barrels;			// where projectiles are spawned, in local turret space.
		public Turret Turret;				// where this weapon is mounted -- possibly shared

		public Weapon(string weaponName, Turret turret, int[] localOffset)
		{
			Info = Rules.Weapons[weaponName.ToLowerInvariant()];
			Burst = Info.Burst;
			Turret = turret;

			var barrels = new List<Barrel>();
			for (var i = 0; i < localOffset.Length / 3; i++)
				barrels.Add(new Barrel
				{
					Position = new int2(localOffset[3 * i], localOffset[3 * i + 1]),
					Facing = localOffset[3 * i + 2]
				});

			// if no barrels specified, the default is "turret position; turret facing".
			if (barrels.Count == 0)
				barrels.Add(new Barrel { Position = int2.Zero, Facing = 0 });

			Barrels = barrels.ToArray();
		}

		public bool IsReloading { get { return FireDelay > 0; } }

		public void Tick()
		{
			if (FireDelay > 0) --FireDelay;
			Turret.Recoil = Math.Max(0f, Turret.Recoil - .2f);
		}

		public bool IsValidAgainst(World world, Target target)
		{
			if( target.IsActor )
				return Combat.WeaponValidForTarget( Info, target.Actor );
			else
				return Combat.WeaponValidForTarget( Info, world, Util.CellContaining( target.CenterLocation ) );
		}

		public void FiredShot()
		{
			Turret.Recoil = 1;

			if (--Burst > 0)
				FireDelay = Info.BurstDelay;
			else
			{
				FireDelay = Info.ROF;
				Burst = Info.Burst;
			}
		}

		public void CheckFire(Actor self, AttackBase attack, IMove move, IFacing facing, Target target)
		{		
			if (FireDelay > 0) return;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return;

			if( !Combat.IsInRange( self.CenterLocation, Info.Range, target ) )
				return;
			if( Combat.IsInRange( self.CenterLocation, Info.MinRange, target ) )
				return;
			
			if (!IsValidAgainst(self.World, target)) return;

			var barrel = Barrels[Burst % Barrels.Length];
			var destMove = target.IsActor ? target.Actor.TraitOrDefault<IMove>() : null;

			var args = new ProjectileArgs
			{
				weapon = Info,

				firedBy = self,
				target = target,

				src = (self.CenterLocation
					+ Combat.GetTurretPosition(self, facing, Turret)
					+ Combat.GetBarrelPosition(self, facing, Turret, barrel)).ToInt2(),
				srcAltitude = move != null ? move.Altitude : 0,
				dest = target.CenterLocation.ToInt2(),
				destAltitude = destMove != null ? destMove.Altitude : 0,
				
				facing = barrel.Facing + 
					(self.HasTrait<Turreted>() ? self.Trait<Turreted>().turretFacing :
					facing != null ? facing.Facing : Util.GetFacing(target.CenterLocation - self.CenterLocation, 0)),

				firepowerModifier = self.TraitsImplementing<IFirepowerModifier>()
					 .Select(a => a.GetFirepowerModifier())
					 .Product()
			};
			
			attack.ScheduleDelayedAction( attack.FireDelay( self, self.Info.Traits.Get<AttackBaseInfo>() ), () =>
			{
				if (args.weapon.Projectile != null)
				{
					var projectile = args.weapon.Projectile.Create(args);
					if (projectile != null)
						self.World.Add(projectile);

					if (!string.IsNullOrEmpty(args.weapon.Report))
						Sound.Play(args.weapon.Report + ".aud", self.CenterLocation);
				}
			});

			foreach (var na in self.TraitsImplementing<INotifyAttack>())
				na.Attacking(self);

			FiredShot();
		}
	}
}
