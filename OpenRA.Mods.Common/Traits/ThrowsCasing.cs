#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Throw a casing when armament attack")]
	public class ThrowsCasingInfo : ConditionalTraitInfo
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well. Used as casing throwing.")]
		public readonly string Weapon = null;

		[Desc("Armaments that throw casings.")]
		public readonly string[] Armaments = { "primary", "secondary" };

		[Desc("The chance to throw casings.")]
		public readonly int Chance = 100;

		[Desc("The casings throw from this turret.")]
		public readonly string Turret = null;

		[Desc("Casing spawn position relative to turret or body, (forward, right, up) triples.")]
		public readonly WVec[] LocalOffset = null;

		[Desc("Casing target position relative to turret or body, (forward, right, up) triples.")]
		public readonly WVec[] TargetOffset = null;

		[Desc("Casing target position will be modified to ground level.")]
		public readonly bool CasingHitGroundLevel = true;

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new ThrowsCasing(this); }
		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!string.IsNullOrEmpty(Weapon))
			{
				var weaponToLower = Weapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
					throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));
				WeaponInfo = weapon;
			}

			if (LocalOffset == null)
				throw new YamlException("LocalOffset must have at least 1 value!");

			if (TargetOffset == null)
				throw new YamlException("TargetOffset must have at least 1 value!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class ThrowsCasing : ConditionalTrait<ThrowsCasingInfo>, INotifyAttack
	{
		BodyOrientation coords;
		Turreted turret;

		IEnumerable<int> rangeModifiers;
		IEnumerable<int> damageModifiers;
		IEnumerable<int> inaccuracyModifiers;

		public ThrowsCasing(ThrowsCasingInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			turret = self.TraitsImplementing<Turreted>().FirstOrDefault(t => t.Name == Info.Turret);
			coords = self.Trait<BodyOrientation>();
			rangeModifiers = self.TraitsImplementing<IRangeModifier>().ToArray().Select(m => m.GetRangeModifier());
			damageModifiers = self.TraitsImplementing<IFirepowerModifier>().ToArray().Select(m => m.GetFirepowerModifier());
			inaccuracyModifiers = self.TraitsImplementing<IInaccuracyModifier>().ToArray().Select(m => m.GetInaccuracyModifier());
			base.Created(self);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (IsTraitDisabled || !self.IsInWorld)
				return;

			if (a == null || !Info.Armaments.Contains(a.Info.Name))
				return;

			ThrowCasing(self, barrel);
		}

		void ThrowCasing(Actor self, Barrel barrel)
		{
			if (self.World.SharedRandom.Next(100) > Info.Chance)
				return;

			var weapon = Info.WeaponInfo;
			if (weapon == null)
				return;

			var offsetIndex = barrel.BarrelIndex;

			var offset = Info.LocalOffset[offsetIndex < Info.LocalOffset.Length ? offsetIndex : 0];
			var targetoffset = Info.TargetOffset[offsetIndex < Info.TargetOffset.Length ? offsetIndex : 0];

			Func<WPos> casingSpawnPositionFunc = () => self.CenterPosition + Util.CalculateFireEffectOffset(self, coords, turret, offset);
			var casingSpawnPosition = casingSpawnPositionFunc();

			var casingHitPosition = self.CenterPosition + Util.CalculateFireEffectOffset(self, coords, turret, targetoffset);
			casingHitPosition = Info.CasingHitGroundLevel ? casingHitPosition - new WVec(0, 0, self.World.Map.DistanceAboveTerrain(casingHitPosition).Length) : casingHitPosition;

			Func<WAngle> casingFireFacing = () => (casingHitPosition - casingSpawnPosition).Yaw;

			if (weapon.Report != null && weapon.Report.Any())
				Game.Sound.Play(SoundType.World, weapon.Report, self.World, casingSpawnPosition);

			// TODO: Consider the bursts in the future?
			var args = new ProjectileArgs
			{
				Weapon = weapon,
				Facing = casingFireFacing(),
				CurrentMuzzleFacing = casingFireFacing,
				DamageModifiers = damageModifiers.ToArray(),
				InaccuracyModifiers = inaccuracyModifiers.ToArray(),
				RangeModifiers = rangeModifiers.ToArray(),
				Source = casingSpawnPosition,
				CurrentSource = casingSpawnPositionFunc,
				SourceActor = self,
				PassiveTarget = casingHitPosition,
				GuidedTarget = Target.FromPos(casingHitPosition)
			};

			if (args.Weapon.Projectile != null)
			{
				var projectile = weapon.Projectile.Create(args);
				self.World.AddFrameEndTask(w => w.Add(projectile));
			}
		}
	}
}
