#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
	public class ArmamentInfo : UpgradableTraitInfo, IRulesetLoaded, Requires<AttackBaseInfo>
	{
		public readonly string Name = "primary";

		[WeaponReference, FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
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
		[PaletteReference] public readonly string MuzzlePalette = "effect";

		[Desc("Use multiple muzzle images if non-zero")]
		public readonly int MuzzleSplitFacings = 0;

		public WeaponInfo WeaponInfo { get; private set; }
		public WDist ModifiedRange { get; private set; }

		public readonly Stance TargetStances = Stance.Enemy;
		public readonly Stance ForceTargetStances = Stance.Enemy | Stance.Neutral | Stance.Ally;

		// TODO: instead of having multiple Armaments and unique AttackBase,
		// an actor should be able to have multiple AttackBases with
		// a single corresponding Armament each
		public readonly string Cursor = "attack";

		// TODO: same as above
		public readonly string OutsideRangeCursor = "attackoutsiderange";

		public override object Create(ActorInitializer init) { return new Armament(init.Self, this); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			WeaponInfo weaponInfo;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weaponInfo;
			ModifiedRange = new WDist(Util.ApplyPercentageModifiers(
				WeaponInfo.Range.Length,
				ai.TraitInfos<IRangeModifierInfo>().Select(m => m.GetRangeModifierDefault())));
		}
	}

	public class Armament : UpgradableTrait<ArmamentInfo>, INotifyCreated, ITick, IExplodeModifier
	{
		public readonly WeaponInfo Weapon;
		public readonly Barrel[] Barrels;

		readonly Actor self;
		Turreted turret;
		AmmoPool ammoPool;
		BodyOrientation coords;
		IEnumerable<int> rangeModifiers;
		List<Pair<int, Action>> delayedActions = new List<Pair<int, Action>>();

		public WDist Recoil;
		public int FireDelay { get; protected set; }
		public int Burst { get; protected set; }

		public Armament(Actor self, ArmamentInfo info)
			: base(info)
		{
			this.self = self;

			Weapon = info.WeaponInfo;
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

		public virtual WDist MaxRange()
		{
			return new WDist(Util.ApplyPercentageModifiers(Weapon.Range.Length, rangeModifiers));
		}

		public virtual void Created(Actor self)
		{
			turret = self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == Info.Turret);
			ammoPool = self.TraitsImplementing<AmmoPool>().FirstOrDefault(la => la.Info.Name == Info.AmmoPoolName);
			coords = self.Trait<BodyOrientation>();
			rangeModifiers = self.TraitsImplementing<IRangeModifier>().ToArray().Select(m => m.GetRangeModifier());
		}

		public virtual void Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (FireDelay > 0)
				--FireDelay;

			Recoil = new WDist(Math.Max(0, Recoil.Length - Info.RecoilRecovery.Length));

			for (var i = 0; i < delayedActions.Count; i++)
			{
				var x = delayedActions[i];
				if (--x.First <= 0)
					x.Second();
				delayedActions[i] = x;
			}

			delayedActions.RemoveAll(a => a.First <= 0);
		}

		protected void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add(Pair.New(t, a));
			else
				a();
		}

		// Note: facing is only used by the legacy positioning code
		// The world coordinate model uses Actor.Orientation
		public virtual Barrel CheckFire(Actor self, IFacing facing, Target target)
		{
			if (IsReloading)
				return null;

			if (ammoPool != null && !ammoPool.HasAmmo())
				return null;

			if (turret != null && !turret.HasAchievedDesiredFacing)
				return null;

			if (!target.IsInRange(self.CenterPosition, MaxRange()))
				return null;

			if (Weapon.MinRange != WDist.Zero && target.IsInRange(self.CenterPosition, Weapon.MinRange))
				return null;

			if (!Weapon.IsValidAgainst(target, self.World, self))
				return null;

			var barrel = Barrels[Burst % Barrels.Length];
			Func<WPos> muzzlePosition = () => self.CenterPosition + MuzzleOffset(self, barrel);
			var legacyFacing = MuzzleOrientation(self, barrel).Yaw.Angle / 4;

			var args = new ProjectileArgs
			{
				Weapon = Weapon,
				Facing = legacyFacing,

				DamageModifiers = self.TraitsImplementing<IFirepowerModifier>()
					.Select(a => a.GetFirepowerModifier()).ToArray(),

				InaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>()
					.Select(a => a.GetInaccuracyModifier()).ToArray(),

				RangeModifiers = self.TraitsImplementing<IRangeModifier>()
					.Select(a => a.GetRangeModifier()).ToArray(),

				Source = muzzlePosition(),
				CurrentSource = muzzlePosition,
				SourceActor = self,
				PassiveTarget = target.CenterPosition,
				GuidedTarget = target
			};

			foreach (var na in self.TraitsImplementing<INotifyAttack>())
				na.PreparingAttack(self, target, this, barrel);

			ScheduleDelayedAction(Info.FireDelay, () =>
			{
				if (args.Weapon.Projectile != null)
				{
					var projectile = args.Weapon.Projectile.Create(args);
					if (projectile != null)
						self.World.Add(projectile);

					if (args.Weapon.Report != null && args.Weapon.Report.Any())
						Game.Sound.Play(args.Weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);

					foreach (var na in self.TraitsImplementing<INotifyAttack>())
						na.Attacking(self, target, this, barrel);

					Recoil = Info.Recoil;
				}
			});

			if (--Burst > 0)
				FireDelay = Weapon.BurstDelay;
			else
			{
				var modifiers = self.TraitsImplementing<IReloadModifier>()
					.Select(m => m.GetReloadModifier());
				FireDelay = Util.ApplyPercentageModifiers(Weapon.ReloadDelay, modifiers);
				Burst = Weapon.Burst;

				foreach (var nbc in self.TraitsImplementing<INotifyBurstComplete>())
					nbc.FiredBurst(self, target, this);
			}

			return barrel;
		}

		public virtual bool OutOfAmmo { get { return ammoPool != null && !ammoPool.Info.SelfReloads && !ammoPool.HasAmmo(); } }
		public virtual bool IsReloading { get { return FireDelay > 0 || IsTraitDisabled; } }
		public virtual bool AllowExplode { get { return !IsReloading; } }
		bool IExplodeModifier.ShouldExplode(Actor self) { return AllowExplode; }

		public virtual WVec MuzzleOffset(Actor self, Barrel b)
		{
			var bodyOrientation = coords.QuantizeOrientation(self, self.Orientation);
			var localOffset = b.Offset + new WVec(-Recoil, WDist.Zero, WDist.Zero);
			if (turret != null)
			{
				// WorldOrientation is quantized to satisfy the *Fudges.
				// Need to then convert back to a pseudo-local coordinate space, apply offsets,
				// then rotate back at the end
				var turretOrientation = turret.WorldOrientation(self) - bodyOrientation;
				localOffset = localOffset.Rotate(turretOrientation);
				localOffset += turret.Offset;
			}

			return coords.LocalToWorld(localOffset.Rotate(bodyOrientation));
		}

		public virtual WRot MuzzleOrientation(Actor self, Barrel b)
		{
			var orientation = turret != null ? turret.WorldOrientation(self) :
				coords.QuantizeOrientation(self, self.Orientation);

			return orientation + WRot.FromYaw(b.Yaw);
		}

		public Actor Actor { get { return self; } }
	}
}
