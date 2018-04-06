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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	public class FireClusterWarhead : WarheadAS, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference, FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[FieldLoader.Require]
		[Desc("Size of the cluster footprint")]
		public readonly CVec Dimensions = CVec.Zero;

		[FieldLoader.Require]
		[Desc("Cluster footprint. Cells marked as x will be attacked.")]
		public readonly string Footprint = string.Empty;

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
		}

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!target.IsValidFor(firedBy))
				return;

			var map = firedBy.World.Map;

			var targetCell = map.CellContaining(target.CenterPosition);

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var targetCells = CellsMatching(targetCell);

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
						Game.Sound.Play(SoundType.World, args.Weapon.Report.Random(firedBy.World.SharedRandom), target.CenterPosition);
				}
			}
		}

		IEnumerable<CPos> CellsMatching(CPos location)
		{
			var index = 0;
			var footprint = Footprint.Where(c => !char.IsWhiteSpace(c)).ToArray();
			var x = location.X - (Dimensions.X - 1) / 2;
			var y = location.Y - (Dimensions.Y - 1) / 2;
			for (var j = 0; j < Dimensions.Y; j++)
				for (var i = 0; i < Dimensions.X; i++)
					if (footprint[index++] == 'x')
						yield return new CPos(x + i, y + j);
		}
	}
}
