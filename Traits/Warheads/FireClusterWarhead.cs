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
	public class FireClusterWarhead : Warhead
	{
		[WeaponReference, FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("Ignore the weapon's target settings and always fire this weapon?",
			"Enabling this allows legacy WW behaviour.")]
		public readonly bool ForceFire = false;

		[Desc("The range of the cells where the weapon should be fired.")]
		public readonly int Range = 1;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var map = firedBy.World.Map;
			var targetCells = map.FindTilesInCircle(map.CellContaining(target.CenterPosition), Range);
			var weapon = map.Rules.Weapons[Weapon.ToLowerInvariant()];

			foreach (var cell in targetCells)
			{
				var tc = Target.FromCell(firedBy.World, cell);

				if (!weapon.IsValidAgainst(tc, firedBy.World, firedBy) && ForceFire)
					continue;

				var args = new ProjectileArgs
				{
					Weapon = weapon,
					Facing = Util.GetFacing(map.CenterOfCell(cell) - target.CenterPosition, 0),

					DamageModifiers = firedBy.TraitsImplementing<IFirepowerModifier>()
						.Select(a => a.GetFirepowerModifier()).ToArray(),

					InaccuracyModifiers = firedBy.TraitsImplementing<IInaccuracyModifier>()
						.Select(a => a.GetInaccuracyModifier()).ToArray(),

					RangeModifiers = firedBy.TraitsImplementing<IRangeModifier>()
						.Select(a => a.GetRangeModifier()).ToArray(),

					Source = target.CenterPosition,
					SourceActor = firedBy,
					PassiveTarget = map.CenterOfCell(cell),
					GuidedTarget = tc
				};

				if (args.Weapon.Projectile != null)
				{
					var projectile = args.Weapon.Projectile.Create(args);
					if (projectile != null)
						firedBy.World.Add(projectile);

					if (args.Weapon.Report != null && args.Weapon.Report.Any())
						Game.Sound.Play(args.Weapon.Report.Random(firedBy.World.SharedRandom), target.CenterPosition);
				}
			}
		}
	}
}