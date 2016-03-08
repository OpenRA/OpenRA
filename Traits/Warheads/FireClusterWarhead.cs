#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	public class FireClusterWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference, FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Ignore the weapon's target settings and always fire this weapon?",
			"Enabling this allows legacy WW behaviour.")]
		public readonly bool ForceFire = false;

		[Desc("The range of the cells where the weapon should be fired.")]
		public readonly int Range = 1;

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			weapon = rules.Weapons[Weapon.ToLowerInvariant()];
		}

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var map = firedBy.World.Map;
			var targetCells = map.FindTilesInCircle(map.CellContaining(target.CenterPosition), Range);

			foreach (var cell in targetCells)
			{
				var tc = Target.FromCell(firedBy.World, cell);

				if (!weapon.IsValidAgainst(tc, firedBy.World, firedBy) && ForceFire)
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
