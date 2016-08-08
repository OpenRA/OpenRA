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
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	public class FireClusterWarhead : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[WeaponReference, FieldLoader.Require]
		[Desc("Has to be defined in weapons.yaml as well.")]
		public readonly string Weapon = null;

		[Desc("The range of the cells where the weapon should be fired.")]
		public readonly int Range = 1;

		[Desc("Search radius around impact for 'direct hit' check.")]
		public readonly WDist TargetSearchRadius = new WDist(2048);

		WeaponInfo weapon;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (!rules.Weapons.TryGetValue(Weapon.ToLowerInvariant(), out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(Weapon.ToLowerInvariant()));
		}

		public ImpactType GetImpactType(World world, CPos cell, WPos pos, Actor firedBy)
		{
			// Missiles need a margin because they sometimes explode a little above ground
			// due to their explosion check triggering slightly too early (because of CloseEnough).
			// TODO: Base ImpactType on target altitude instead of explosion altitude.
			var airMargin = new WDist(128);

			// Matching target actor
			if (GetDirectHit(world, cell, pos, firedBy, true))
				return ImpactType.TargetHit;

			var dat = world.Map.DistanceAboveTerrain(pos);

			if (dat.Length > airMargin.Length)
				return ImpactType.Air;

			return ImpactType.Ground;
		}

		public bool GetDirectHit(World world, CPos cell, WPos pos, Actor firedBy, bool checkTargetType = false)
		{
			foreach (var victim in world.FindActorsInCircle(pos, TargetSearchRadius))
			{
				if (checkTargetType && !IsValidAgainst(victim, firedBy))
					continue;

				var healthInfo = victim.Info.TraitInfoOrDefault<HealthInfo>();
				if (healthInfo == null)
					continue;

				// If the impact position is within any actor's HitShape, we have a direct hit
				if (healthInfo.Shape.DistanceFromEdge(pos, victim).Length <= 0)
					return true;
			}

			return false;
		}

		public bool IsValidImpact(WPos pos, Actor firedBy)
		{
			var world = firedBy.World;
			var targetTile = world.Map.CellContaining(pos);
			if (!world.Map.Contains(targetTile))
				return false;

			var impactType = GetImpactType(world, targetTile, pos, firedBy);
			var validImpact = false;
			switch (impactType)
			{
				case ImpactType.TargetHit:
					validImpact = true;
					break;
				case ImpactType.Air:
					validImpact = IsValidTarget(new string[]{"Air"});
					break;
				case ImpactType.Ground:
					var tileInfo = world.Map.GetTerrainInfo(targetTile);
					validImpact = IsValidTarget(tileInfo.TargetTypes);
					break;
			}

			return validImpact;
		}

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var map = firedBy.World.Map;

			var targetCell = map.CellContaining(target.CenterPosition);

			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var targetCells = map.FindTilesInCircle(targetCell, Range);

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
	}
}
