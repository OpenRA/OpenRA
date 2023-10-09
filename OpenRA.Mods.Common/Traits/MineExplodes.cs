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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor explodes when killed and the kill goes to the " + nameof(Minelayer) + " actor.")]
	public class MineExplodesInfo : ConditionalTraitInfo
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Default weapon to use for explosion if ammo/payload is loaded.")]
		public readonly string Weapon = null;

		[Desc("DeathType(s) that trigger the explosion. Leave empty to always trigger an explosion.")]
		public readonly BitSet<DamageType> DeathTypes = default;

		[Desc("Offset of the explosion from the center of the exploding actor (or cell).")]
		public readonly WVec Offset = WVec.Zero;

		public WeaponInfo WeaponInfo { get; private set; }

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

			if (!Info.DeathTypes.IsEmpty && !e.Damage.DamageTypes.Overlaps(Info.DeathTypes))
				return;

			var weapon = Info.WeaponInfo;
			if (weapon == null)
				return;

			if (weapon.Report != null && weapon.Report.Any())
				Game.Sound.Play(SoundType.World, weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);

			var attacker = parent == null || parent.IsDead ? self : parent;

			var args = new ProjectileArgs
			{
				Weapon = weapon,
				Facing = WAngle.Zero,
				CurrentMuzzleFacing = () => WAngle.Zero,

				DamageModifiers = !attacker.IsDead ? attacker.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier()).ToArray() : Array.Empty<int>(),

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
