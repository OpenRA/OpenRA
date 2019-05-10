#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Fires weapon at the carrying warhead's target.")]
	public class FireWeaponAtTargetWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference, FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Range from which to choose a random horizontal launch direction yaw relative to target direction.")]
		public readonly WAngle[] RandomYawRange = { };

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
		}

		public override void DoImpact(Target target, Target guidedTarget, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!guidedTarget.IsValidFor(firedBy))
				return;

			if (!weapon.IsValidAgainst(guidedTarget, firedBy.World, firedBy))
				return;

			var yaw = RandomYawRange.Length > 0 ? new WAngle(firedBy.World.SharedRandom.Next(RandomYawRange[0].Angle, RandomYawRange[1].Angle)) : WAngle.Zero;
			var legacyFacing = yaw.Angle / 4;

			var args = new ProjectileArgs
			{
				Weapon = weapon,
				Facing = (guidedTarget.CenterPosition - target.CenterPosition).Yaw.Facing + legacyFacing,

				DamageModifiers = damageModifiers.ToArray(),
				InaccuracyModifiers = new int[0],
				RangeModifiers = new int[0],

				Source = target.CenterPosition,
				CurrentSource = () => target.CenterPosition,
				SourceActor = firedBy,
				PassiveTarget = guidedTarget.CenterPosition,
				GuidedTarget = guidedTarget
			};

			if (args.Weapon.Projectile != null)
			{
				var projectile = args.Weapon.Projectile.Create(args);
				if (projectile != null)
					firedBy.World.AddFrameEndTask(w => w.Add(projectile));

				if (args.Weapon.Report != null && args.Weapon.Report.Any())
					Game.Sound.Play(SoundType.World, args.Weapon.Report, firedBy.World, target.CenterPosition);
			}
		}
	}
}
