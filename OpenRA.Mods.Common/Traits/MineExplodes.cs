#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Flags]
	public enum GetFirePowerFrom { Self, Parent, All }

	[Desc("This actor explodes when killed and the kill goes to the " + nameof(Minelayer) + " actor.")]
	public class MineExplodesInfo : ConditionalTraitInfo
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Default weapon to use for explosion when not sweeped.")]
		public readonly string Weapon = null;

		[WeaponReference]
		[Desc("Default weapon to use for explosion when sweeped.")]
		public readonly string SweepedWeapon = null;

		[Desc("Killer ActorType(s) that consider the mine sweeper to trigger SweepedWeapon instead of Weapon.")]
		public readonly HashSet<string> SweeperTypes = new();

		[Desc("Offset of the explosion from the center of the exploding actor (or cell).")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Get " + nameof(IFirepowerModifier) + " from specific actor(s) when explodes. Possible values are Self, Parent, All")]
		public readonly GetFirePowerFrom GetFirePowerFrom = GetFirePowerFrom.All;

		public WeaponInfo WeaponInfo { get; private set; }
		public WeaponInfo SweepedWeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new MineExplodes(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (!string.IsNullOrEmpty(Weapon))
			{
				var weaponToLower = Weapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");
				WeaponInfo = weapon;
			}

			if (!string.IsNullOrEmpty(SweepedWeapon))
			{
				var sweepedWeaponToLower = SweepedWeapon.ToLowerInvariant();
				if (!rules.Weapons.TryGetValue(sweepedWeaponToLower, out var sweepedWeapon))
					throw new YamlException($"Weapons Ruleset does not contain an entry '{sweepedWeaponToLower}'");
				SweepedWeaponInfo = sweepedWeapon;
			}

			base.RulesetLoaded(rules, ai);
		}
	}

	public class MineExplodes : ConditionalTrait<MineExplodesInfo>, INotifyKilled
	{
		Actor parent;

		public MineExplodes(ActorInitializer init, MineExplodesInfo info)
			: base(info)
		{
			var pa = init.GetOrDefault<ParentActorInit>()?.Value;
			if (pa != null)
				init.World.AddFrameEndTask(_ => parent = pa.Actor(init.World).Value);
		}

		void INotifyKilled.Killed(Actor self, AttackInfo e)
		{
			if (IsTraitDisabled || !self.IsInWorld)
				return;

			var weapon = Info.WeaponInfo;

			if (Info.SweeperTypes.Contains(e.Attacker.Info.Name))
			{
				if (Info.SweepedWeaponInfo == null)
					return;
				else
					weapon = Info.SweepedWeaponInfo;
			}

			if (weapon == null)
				return;

			if (weapon.Report != null && weapon.Report.Length > 0)
				Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);

			var attacker = self;
			var firePowerModifier = new List<int>();
			if (parent != null && !parent.IsDead)
			{
				attacker = parent;
				if (Info.GetFirePowerFrom != GetFirePowerFrom.Parent)
					firePowerModifier.AddRange(self.TraitsImplementing<IFirepowerModifier>().Select(a => a.GetFirepowerModifier()));
				if (Info.GetFirePowerFrom != GetFirePowerFrom.Self)
					firePowerModifier.AddRange(parent.TraitsImplementing<IFirepowerModifier>().Select(a => a.GetFirepowerModifier()));
			}

			var args = new ProjectileArgs
			{
				Weapon = weapon,
				Facing = WAngle.Zero,
				CurrentMuzzleFacing = () => WAngle.Zero,

				DamageModifiers = firePowerModifier.ToArray(),

				InaccuracyModifiers = Array.Empty<int>(),

				RangeModifiers = Array.Empty<int>(),

				Source = self.CenterPosition,
				CurrentSource = () => self.CenterPosition,
				SourceActor = attacker,
				PassiveTarget = self.CenterPosition
			};

			// Use .FromPos since this actor is killed. Cannot use Target.FromActor
			weapon.Impact(Target.FromPos(self.CenterPosition), new WarheadArgs(args));
		}
	}
}
