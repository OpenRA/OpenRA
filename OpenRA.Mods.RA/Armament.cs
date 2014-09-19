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
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class Barrel
	{
		public WVec Offset;
		public WAngle Yaw;
	}

	[Desc("Allows you to attach weapons to the unit (use @IdentifierSuffix for > 1)")]
	public class ArmamentInfo : UpgradableTraitInfo, ITraitInfo, Requires<AttackBaseInfo>
	{
		public readonly string Name = "primary";

		[WeaponReference]
		[Desc("Has to be defined here and in weapons.yaml.")]
		public readonly string Weapon = null;
		[Desc("Turret this armament is assigned to.")]
		public readonly string Turret = "primary";
		[Desc("Time (in frames) until the weapon can fire again.")] 
		public readonly int FireDelay = 0;

		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec[] LocalOffset = {};
		[Desc("Muzzle yaw relative to turret or body.")]
		public readonly WAngle[] LocalYaw = {};
		[Desc("Move the turret (or barrel) backwards when firing.")]
		public readonly WRange Recoil = WRange.Zero;
		[Desc("Recoil recovery per-frame")]
		public readonly WRange RecoilRecovery = new WRange(9);

		[Desc("Muzzle flash sequence to render")]
		public readonly string MuzzleSequence = null;

		[Desc("Palette to render Muzzle flash sequence in")]
		public readonly string MuzzlePalette = "effect";

		[Desc("Use multiple muzzle images if non-zero")]
		public readonly int MuzzleSplitFacings = 0;

		[Desc("Has this amount of limited ammo if non-zero")]
		public readonly int LimitedAmmo = 0;
		[Desc("Defaults to value in LimitedAmmo.")]
		public readonly int AmmoPipCount = -1;
		[Desc("Pip type to display for loaded ammo.")]
		public readonly PipType AmmoPipType = PipType.Green;
		[Desc("Pip type to display for empty ammo.")]
		public readonly PipType AmmoPipTypeEmpty = PipType.Transparent;
		[Desc("Time to reload limited ammo measured in ticks.", 
			"Applies to rearming at structure as well as actor reloading itself.")]
		public readonly int AmmoReloadTicks = 25 * 2;
		[Desc("How much ammo is reloaded after a certain period (for self-reloading actors).")]
		public readonly int AmmoReloadCount = 0;
		[Desc("Does armament reload its limited ammo itself.")]
		public readonly bool ReloadsAmmo = false;
		[Desc("Whether or not reload counter should be reset when ammo has been fired (for self-reloading actors).")]
		public readonly bool ResetAmmoReloadOnFire = false;

		public object Create(ActorInitializer init) { return new Armament(init.self, this); }
	}

	public class Armament : UpgradableTrait<ArmamentInfo>, ITick, IExplodeModifier, INotifyAttack, IPips, ISync
	{
		public readonly WeaponInfo Weapon;
		public readonly Barrel[] Barrels;
		[Sync] public int Ammo;
		[Sync] int remainingTicks;
		[Sync] int previousAmmo;

		public readonly Actor self;
		Lazy<Turreted> Turret;
		Lazy<IBodyOrientation> Coords;
		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public WRange Recoil;
		public int FireDelay { get; private set; }
		public int Burst { get; private set; }

		public Armament(Actor self, ArmamentInfo info)
			: base(info)
		{
			this.self = self;

			Ammo = info.LimitedAmmo;

			// We can't resolve these until runtime
			Turret = Exts.Lazy(() => self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == info.Turret));
			Coords = Exts.Lazy(() => self.Trait<IBodyOrientation>());

			Weapon = self.World.Map.Rules.Weapons[info.Weapon.ToLowerInvariant()];
			Burst = Weapon.Burst;

			var barrels = new List<Barrel>();
			for (var i = 0; i < info.LocalOffset.Length; i++)
			{
				barrels.Add(new Barrel
				{
					Offset = info.LocalOffset[i],
					Yaw = info.LocalYaw.Length > i ? info.LocalYaw[i] : WAngle.Zero
				});
			}

			if (barrels.Count == 0)
				barrels.Add(new Barrel { Offset = WVec.Zero, Yaw = WAngle.Zero });

			Barrels = barrels.ToArray();

			if (info.ReloadsAmmo)
			{
				remainingTicks = info.AmmoReloadTicks;
				
				// Skip armaments that don't define limited ammo
				if (info.LimitedAmmo <= 0)
					return;

				previousAmmo = GetAmmoCount();
			}
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (FireDelay > 0)
				--FireDelay;

			if (Info.ReloadsAmmo)
			{
				if (!FullAmmo() && --remainingTicks == 0)
				{
					remainingTicks = Info.AmmoReloadTicks;

					for (var i = 0; i < Info.AmmoReloadCount; i++)
						GiveAmmo();

					previousAmmo = GetAmmoCount();
				}

				// Resets the tick counter if ammo was fired.
				if (Info.ResetAmmoReloadOnFire && GetAmmoCount() < previousAmmo)
				{
					remainingTicks = Info.AmmoReloadTicks;
					previousAmmo = GetAmmoCount();
				}
			}

			Recoil = new WRange(Math.Max(0, Recoil.Range - Info.RecoilRecovery.Range));

			for (var i = 0; i < delayedActions.Count; i++)
			{
				var x = delayedActions[i];
				if (--x.First <= 0)
					x.Second();
				delayedActions[i] = x;
			}

			delayedActions.RemoveAll(a => a.First <= 0);
		}

		void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add(Pair.New(t, a));
			else
				a();
		}

		public bool FullAmmo() { return Ammo == Info.LimitedAmmo; }
		public bool HasAmmo() { return Ammo > 0; }
		public bool GiveAmmo()
		{
			if (Ammo >= Info.LimitedAmmo) return false;
			++Ammo;
			return true;
		}

		public bool TakeAmmo()
		{
			if (Ammo <= 0) return false;
			--Ammo;
			return true;
		}

		public int ReloadTimePerAmmo() { return Info.AmmoReloadTicks; }

		public void Attacking(Actor self, Target target, Armament a, Barrel barrel) { TakeAmmo(); }

		public int GetAmmoCount() { return Ammo; }

		public IEnumerable<PipType> GetPips(Actor self)
		{
			var pips = Info.AmmoPipCount > -1 ? Info.AmmoPipCount : Info.LimitedAmmo;
			return Exts.MakeArray(pips,
				i => (Ammo * pips) / Info.LimitedAmmo > i ? Info.AmmoPipType : Info.AmmoPipTypeEmpty);
		}

		// Note: facing is only used by the legacy positioning code
		// The world coordinate model uses Actor.Orientation
		public Barrel CheckFire(Actor self, IFacing facing, Target target)
		{
			if (IsReloading)
				return null;

			if (Info.LimitedAmmo > 0 && !HasAmmo())
				return null;

			if (!target.IsInRange(self.CenterPosition, Weapon.Range))
				return null;

			if (Weapon.MinRange != WRange.Zero && target.IsInRange(self.CenterPosition, Weapon.MinRange))
				return null;

			if (!Weapon.IsValidAgainst(target, self.World, self))
				return null;

			var barrel = Barrels[Burst % Barrels.Length];
			var muzzlePosition = self.CenterPosition + MuzzleOffset(self, barrel);
			var legacyFacing = MuzzleOrientation(self, barrel).Yaw.Angle / 4;

			var args = new ProjectileArgs
			{
				Weapon = Weapon,
				Facing = legacyFacing,

				DamageModifiers = self.TraitsImplementing<IFirepowerModifier>()
					.Select(a => a.GetFirepowerModifier()),

				InaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>()
					.Select(a => a.GetInaccuracyModifier()),

				Source = muzzlePosition,
				SourceActor = self,
				PassiveTarget = target.CenterPosition,
				GuidedTarget = target
			};

			ScheduleDelayedAction(Info.FireDelay, () =>
			{
				if (args.Weapon.Projectile != null)
				{
					var projectile = args.Weapon.Projectile.Create(args);
					if (projectile != null)
						self.World.Add(projectile);

					if (args.Weapon.Report != null && args.Weapon.Report.Any())
						Sound.Play(args.Weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
				}
			});

			foreach (var na in self.TraitsImplementing<INotifyAttack>())
				na.Attacking(self, target, this, barrel);

			Recoil = Info.Recoil;

			if (--Burst > 0)
				FireDelay = Weapon.BurstDelay;
			else
			{
				var modifiers = self.TraitsImplementing<IReloadModifier>()
					.Select(m => m.GetReloadModifier());
				FireDelay = Util.ApplyPercentageModifiers(Weapon.ReloadDelay, modifiers);
				Burst = Weapon.Burst;
			}

			return barrel;
		}

		public bool IsReloading { get { return FireDelay > 0 || IsTraitDisabled; } }
		public bool ShouldExplode(Actor self) { return !IsReloading; }

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

		public Actor Actor { get { return self; } }
	}
}
