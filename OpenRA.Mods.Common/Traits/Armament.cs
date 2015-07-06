#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class Barrel
	{
		public WVec Offset;
		public WAngle Yaw;
	}

	[Desc("Allows you to attach weapons to the unit (use @IdentifierSuffix for > 1)")]
	public class ArmamentInfo : UpgradableTraitInfo, Requires<AttackBaseInfo>
	{
		public readonly string Name = "primary";

		[WeaponReference]
		[Desc("Has to be defined here and in weapons.yaml.")]
		public readonly string Weapon = null;

		[Desc("Which limited ammo pool (if present) should this armament be assigned to.")]
		public readonly string AmmoPoolName = "primary";

		[Desc("Which turret (if present) should this armament be assigned to.")]
		public readonly string Turret = "primary";

		[Desc("Time (in frames) until the weapon can fire again.")]
		public readonly int FireDelay = 0;

		[Desc("Muzzle position relative to turret or body. (forward, right, up) triples")]
		public readonly WVec[] LocalOffset = { };

		[Desc("Muzzle yaw relative to turret or body.")]
		public readonly WAngle[] LocalYaw = { };

		[Desc("Move the turret backwards when firing.")]
		public readonly WDist Recoil = WDist.Zero;

		[Desc("Recoil recovery per-frame")]
		public readonly WDist RecoilRecovery = new WDist(9);

		[Desc("Muzzle flash sequence to render")]
		public readonly string MuzzleSequence = null;

		[Desc("Palette to render Muzzle flash sequence in")]
		public readonly string MuzzlePalette = "effect";

		[Desc("Use multiple muzzle images if non-zero")]
		public readonly int MuzzleSplitFacings = 0;

		public override object Create(ActorInitializer init) { return new Armament(init.Self, this); }
	}

	public class Armament : UpgradableTrait<ArmamentInfo>, ITick, IExplodeModifier
	{
		public readonly WeaponInfo Weapon;
		public readonly Barrel[] Barrels;

		readonly Actor self;
		Lazy<Turreted> turret;
		Lazy<IBodyOrientation> coords;
		Lazy<AmmoPool> ammoPool;
		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public WDist Recoil;
		public int FireDelay { get; private set; }
		public int Burst { get; private set; }

		public Armament(Actor self, ArmamentInfo info)
			: base(info)
		{
			this.self = self;

			// We can't resolve these until runtime
			turret = Exts.Lazy(() => self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == info.Turret));
			coords = Exts.Lazy(() => self.Trait<IBodyOrientation>());
			ammoPool = Exts.Lazy(() => self.TraitsImplementing<AmmoPool>().FirstOrDefault(la => la.Info.Name == info.AmmoPoolName));

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
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (FireDelay > 0)
				--FireDelay;

			Recoil = new WDist(Math.Max(0, Recoil.Range - Info.RecoilRecovery.Range));

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

		// Note: facing is only used by the legacy positioning code
		// The world coordinate model uses Actor.Orientation
		public Barrel CheckFire(Actor self, IFacing facing, Target target)
		{
			if (IsReloading)
				return null;

			if (ammoPool.Value != null && !ammoPool.Value.HasAmmo())
				return null;

			if (!target.IsInRange(self.CenterPosition, Weapon.Range))
				return null;

			if (Weapon.MinRange != WDist.Zero && target.IsInRange(self.CenterPosition, Weapon.MinRange))
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
					.Select(a => a.GetFirepowerModifier()).ToArray(),

				InaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>()
					.Select(a => a.GetInaccuracyModifier()).ToArray(),

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
			var bodyOrientation = coords.Value.QuantizeOrientation(self, self.Orientation);
			var localOffset = b.Offset + new WVec(-Recoil, WDist.Zero, WDist.Zero);
			if (turret.Value != null)
			{
				var turretOrientation = coords.Value.QuantizeOrientation(self, turret.Value.LocalOrientation(self));
				localOffset = localOffset.Rotate(turretOrientation);
				localOffset += turret.Value.Offset;
			}

			return coords.Value.LocalToWorld(localOffset.Rotate(bodyOrientation));
		}

		public WRot MuzzleOrientation(Actor self, Barrel b)
		{
			var orientation = self.Orientation + WRot.FromYaw(b.Yaw);
			if (turret.Value != null)
				orientation += turret.Value.LocalOrientation(self);
			return orientation;
		}

		public Actor Actor { get { return self; } }
	}
}
