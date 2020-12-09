#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class ArmamentInfo : PausableConditionalTraitInfo, Requires<AttackBaseInfo>
	{
		public readonly string Name = "primary";

		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Which turret (if present) should this armament be assigned to.")]
		public readonly string Turret = "primary";

		[Desc("Time (in frames) until the weapon can fire again.")]
		public readonly int FireDelay = 0;

		[Desc("Muzzle position relative to turret or body, (forward, right, up) triples.",
			"If weapon Burst = 1, it cycles through all listed offsets, otherwise the offset corresponding to current burst is used.")]
		public readonly WVec[] LocalOffset = { };

		[Desc("Muzzle yaw relative to turret or body.")]
		public readonly WAngle[] LocalYaw = { };

		[Desc("Move the turret backwards when firing.")]
		public readonly WDist Recoil = WDist.Zero;

		[Desc("Recoil recovery per-frame")]
		public readonly WDist RecoilRecovery = new WDist(9);

		[SequenceReference]
		[Desc("Muzzle flash sequence to render")]
		public readonly string MuzzleSequence = null;

		[PaletteReference]
		[Desc("Palette to render Muzzle flash sequence in")]
		public readonly string MuzzlePalette = "effect";

		[GrantedConditionReference]
		[Desc("Condition to grant while reloading.")]
		public readonly string ReloadingCondition = null;

		public WeaponInfo WeaponInfo { get; private set; }
		public WDist ModifiedRange { get; private set; }

		public readonly PlayerRelationship TargetRelationships = PlayerRelationship.Enemy;
		public readonly PlayerRelationship ForceTargetRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral | PlayerRelationship.Ally;

		// TODO: instead of having multiple Armaments and unique AttackBase,
		// an actor should be able to have multiple AttackBases with
		// a single corresponding Armament each
		[Desc("Cursor to display when hovering over a valid target.")]
		public readonly string Cursor = "attack";

		// TODO: same as above
		[Desc("Cursor to display when hovering over a valid target that is outside of range.")]
		public readonly string OutsideRangeCursor = "attackoutsiderange";

		public override object Create(ActorInitializer init) { return new Armament(init.Self, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weaponInfo))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weaponInfo;
			ModifiedRange = new WDist(Util.ApplyPercentageModifiers(
				WeaponInfo.Range.Length,
				ai.TraitInfos<IRangeModifierInfo>().Select(m => m.GetRangeModifierDefault())));

			if (WeaponInfo.Burst > 1 && WeaponInfo.BurstDelays.Length > 1 && (WeaponInfo.BurstDelays.Length != WeaponInfo.Burst - 1))
				throw new YamlException("Weapon '{0}' has an invalid number of BurstDelays, must be single entry or Burst - 1.".F(weaponToLower));

			base.RulesetLoaded(rules, ai);
		}
	}

	public class Armament : PausableConditionalTrait<ArmamentInfo>, ITick
	{
		public readonly WeaponInfo Weapon;
		public readonly Barrel[] Barrels;

		readonly Actor self;
		Turreted turret;
		BodyOrientation coords;
		INotifyBurstComplete[] notifyBurstComplete;
		INotifyAttack[] notifyAttacks;

		int conditionToken = Actor.InvalidConditionToken;

		IEnumerable<int> rangeModifiers;
		IEnumerable<int> reloadModifiers;
		IEnumerable<int> damageModifiers;
		IEnumerable<int> inaccuracyModifiers;

		int ticksSinceLastShot;
		int currentBarrel;
		int barrelCount;

		List<(int Ticks, Action Func)> delayedActions = new List<(int, Action)>();

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

			barrelCount = barrels.Count;

			Barrels = barrels.ToArray();
		}

		public virtual WDist MaxRange()
		{
			return new WDist(Util.ApplyPercentageModifiers(Weapon.Range.Length, rangeModifiers.ToArray()));
		}

		protected override void Created(Actor self)
		{
			turret = self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == Info.Turret);
			coords = self.Trait<BodyOrientation>();
			notifyBurstComplete = self.TraitsImplementing<INotifyBurstComplete>().ToArray();
			notifyAttacks = self.TraitsImplementing<INotifyAttack>().ToArray();

			rangeModifiers = self.TraitsImplementing<IRangeModifier>().ToArray().Select(m => m.GetRangeModifier());
			reloadModifiers = self.TraitsImplementing<IReloadModifier>().ToArray().Select(m => m.GetReloadModifier());
			damageModifiers = self.TraitsImplementing<IFirepowerModifier>().ToArray().Select(m => m.GetFirepowerModifier());
			inaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>().ToArray().Select(m => m.GetInaccuracyModifier());

			base.Created(self);
		}

		void UpdateCondition(Actor self)
		{
			if (string.IsNullOrEmpty(Info.ReloadingCondition))
				return;

			var enabled = !IsTraitDisabled && IsReloading;

			if (enabled && conditionToken == Actor.InvalidConditionToken)
				conditionToken = self.GrantCondition(Info.ReloadingCondition);
			else if (!enabled && conditionToken != Actor.InvalidConditionToken)
				conditionToken = self.RevokeCondition(conditionToken);
		}

		protected virtual void Tick(Actor self)
		{
			// We need to disable conditions if IsTraitDisabled is true, so we have to update conditions before the return below.
			UpdateCondition(self);

			if (IsTraitDisabled)
				return;

			if (ticksSinceLastShot < Weapon.ReloadDelay)
				++ticksSinceLastShot;

			if (FireDelay > 0)
				--FireDelay;

			Recoil = new WDist(Math.Max(0, Recoil.Length - Info.RecoilRecovery.Length));

			for (var i = 0; i < delayedActions.Count; i++)
			{
				var x = delayedActions[i];
				if (--x.Ticks <= 0)
					x.Func();
				delayedActions[i] = x;
			}

			delayedActions.RemoveAll(a => a.Ticks <= 0);
		}

		void ITick.Tick(Actor self)
		{
			// Split into a protected method to allow subclassing
			Tick(self);
		}

		protected void ScheduleDelayedAction(int t, Action a)
		{
			if (t > 0)
				delayedActions.Add((t, a));
			else
				a();
		}

		protected virtual bool CanFire(Actor self, in Target target)
		{
			if (IsReloading || IsTraitPaused)
				return false;

			if (turret != null && !turret.HasAchievedDesiredFacing)
				return false;

			if ((!target.IsInRange(self.CenterPosition, MaxRange()))
				|| (Weapon.MinRange != WDist.Zero && target.IsInRange(self.CenterPosition, Weapon.MinRange)))
				return false;

			if (!Weapon.IsValidAgainst(target, self.World, self))
				return false;

			return true;
		}

		// Note: facing is only used by the legacy positioning code
		// The world coordinate model uses Actor.Orientation
		public virtual Barrel CheckFire(Actor self, IFacing facing, in Target target)
		{
			if (!CanFire(self, target))
				return null;

			if (ticksSinceLastShot >= Weapon.ReloadDelay)
				Burst = Weapon.Burst;

			ticksSinceLastShot = 0;

			// If Weapon.Burst == 1, cycle through all LocalOffsets, otherwise use the offset corresponding to current Burst
			currentBarrel %= barrelCount;
			var barrel = Weapon.Burst == 1 ? Barrels[currentBarrel] : Barrels[Burst % Barrels.Length];
			currentBarrel++;

			FireBarrel(self, facing, target, barrel);

			UpdateBurst(self, target);

			return barrel;
		}

		protected virtual void FireBarrel(Actor self, IFacing facing, in Target target, Barrel barrel)
		{
			foreach (var na in notifyAttacks)
				na.PreparingAttack(self, target, this, barrel);

			Func<WPos> muzzlePosition = () => self.CenterPosition + MuzzleOffset(self, barrel);
			Func<WAngle> muzzleFacing = () => MuzzleOrientation(self, barrel).Yaw;
			var muzzleOrientation = WRot.FromYaw(muzzleFacing());

			var passiveTarget = Weapon.TargetActorCenter ? target.CenterPosition : target.Positions.PositionClosestTo(muzzlePosition());
			var initialOffset = Weapon.FirstBurstTargetOffset;
			if (initialOffset != WVec.Zero)
			{
				// We want this to match Armament.LocalOffset, so we need to convert it to forward, right, up
				initialOffset = new WVec(initialOffset.Y, -initialOffset.X, initialOffset.Z);
				passiveTarget += initialOffset.Rotate(muzzleOrientation);
			}

			var followingOffset = Weapon.FollowingBurstTargetOffset;
			if (followingOffset != WVec.Zero)
			{
				// We want this to match Armament.LocalOffset, so we need to convert it to forward, right, up
				followingOffset = new WVec(followingOffset.Y, -followingOffset.X, followingOffset.Z);
				passiveTarget += ((Weapon.Burst - Burst) * followingOffset).Rotate(muzzleOrientation);
			}

			var args = new ProjectileArgs
			{
				Weapon = Weapon,
				Facing = muzzleFacing(),
				CurrentMuzzleFacing = muzzleFacing,

				DamageModifiers = damageModifiers.ToArray(),

				InaccuracyModifiers = inaccuracyModifiers.ToArray(),

				RangeModifiers = rangeModifiers.ToArray(),

				Source = muzzlePosition(),
				CurrentSource = muzzlePosition,
				SourceActor = self,
				PassiveTarget = passiveTarget,
				GuidedTarget = target
			};

			// Lambdas can't use 'in' variables, so capture a copy for later
			var delayedTarget = target;
			ScheduleDelayedAction(Info.FireDelay, () =>
			{
				if (args.Weapon.Projectile != null)
				{
					var projectile = args.Weapon.Projectile.Create(args);
					if (projectile != null)
						self.World.Add(projectile);

					if (args.Weapon.Report != null && args.Weapon.Report.Any())
						Game.Sound.Play(SoundType.World, args.Weapon.Report, self.World, self.CenterPosition);

					if (Burst == args.Weapon.Burst && args.Weapon.StartBurstReport != null && args.Weapon.StartBurstReport.Any())
						Game.Sound.Play(SoundType.World, args.Weapon.StartBurstReport, self.World, self.CenterPosition);

					foreach (var na in notifyAttacks)
						na.Attacking(self, delayedTarget, this, barrel);

					Recoil = Info.Recoil;
				}
			});
		}

		protected virtual void UpdateBurst(Actor self, in Target target)
		{
			if (--Burst > 0)
			{
				if (Weapon.BurstDelays.Length == 1)
					FireDelay = Weapon.BurstDelays[0];
				else
					FireDelay = Weapon.BurstDelays[Weapon.Burst - (Burst + 1)];
			}
			else
			{
				var modifiers = reloadModifiers.ToArray();
				FireDelay = Util.ApplyPercentageModifiers(Weapon.ReloadDelay, modifiers);
				Burst = Weapon.Burst;

				if (Weapon.AfterFireSound != null && Weapon.AfterFireSound.Any())
					ScheduleDelayedAction(Weapon.AfterFireSoundDelay, () => Game.Sound.Play(SoundType.World, Weapon.AfterFireSound, self.World, self.CenterPosition));

				foreach (var nbc in notifyBurstComplete)
					nbc.FiredBurst(self, target, this);
			}
		}

		public virtual bool IsReloading { get { return FireDelay > 0 || IsTraitDisabled; } }

		public WVec MuzzleOffset(Actor self, Barrel b)
		{
			return CalculateMuzzleOffset(self, b);
		}

		protected virtual WVec CalculateMuzzleOffset(Actor self, Barrel b)
		{
			// Weapon offset in turret coordinates
			var localOffset = b.Offset + new WVec(-Recoil, WDist.Zero, WDist.Zero);

			// Turret coordinates to body coordinates
			var bodyOrientation = coords.QuantizeOrientation(self, self.Orientation);
			if (turret != null)
				localOffset = localOffset.Rotate(turret.WorldOrientation) + turret.Offset.Rotate(bodyOrientation);
			else
				localOffset = localOffset.Rotate(bodyOrientation);

			// Body coordinates to world coordinates
			return coords.LocalToWorld(localOffset);
		}

		public WRot MuzzleOrientation(Actor self, Barrel b)
		{
			return CalculateMuzzleOrientation(self, b);
		}

		protected virtual WRot CalculateMuzzleOrientation(Actor self, Barrel b)
		{
			return WRot.FromYaw(b.Yaw).Rotate(turret != null ? turret.WorldOrientation : self.Orientation);
		}

		public Actor Actor { get { return self; } }
	}
}
