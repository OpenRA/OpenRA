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

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class FireClusterWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference, FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("The range of the cells where the weapon should be fired.",
			"A single value means radius, two values mean rectangle.")]
		public readonly int[] Range = new int[] { 1 };

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			weapon = rules.Weapons[Weapon.ToLowerInvariant()];
		}

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var map = firedBy.World.Map;
			var targetCells = Range.Length == 1
				? map.FindTilesInCircle(map.CellContaining(target.CenterPosition), Range[0])
				: map.FindTilesInRectangle(map.CellContaining(target.CenterPosition), Range[0], Range[1]);

			foreach (var cell in targetCells)
			{
				var tc = Target.FromCell(firedBy.World, cell);

				if (!weapon.IsValidAgainst(tc, firedBy.World, firedBy))
					continue;

				var args = new ProjectileArgs
				{
					Weapon = weapon,
					Facing = (map.CenterOfCell(cell) - target.CenterPosition).Yaw.Facing,

					DamageModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier()).ToArray() : new int[0],

					InaccuracyModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray() : new int[0],

					RangeModifiers = !firedBy.IsDead ? firedBy.TraitsImplementing<IRangeModifier>()
						.Select(a => a.GetRangeModifier()).ToArray() : new int[0],

					Source = target.CenterPosition,
					SourceActor = firedBy,
					PassiveTarget = map.CenterOfCell(cell),
					GuidedTarget = tc
				};

				if (args.Weapon.Projectile != null)
				{
					var projectile = args.Weapon.Projectile.Create(args);
					if (projectile != null)
						firedBy.World.AddFrameEndTask(w => w.Add(projectile));

					if (args.Weapon.Report != null && args.Weapon.Report.Any())
						Game.Sound.Play(args.Weapon.Report.Random(firedBy.World.SharedRandom), target.CenterPosition);
				}
			}
		}

		public override bool IsValidAgainst(Actor victim, Actor firedBy)
		{
			return weapon.IsValidAgainst(victim, firedBy);
		}

		public new bool IsValidAgainst(FrozenActor victim, Actor firedBy)
		{
			return weapon.IsValidAgainst(victim, firedBy);
		}
	}
}
