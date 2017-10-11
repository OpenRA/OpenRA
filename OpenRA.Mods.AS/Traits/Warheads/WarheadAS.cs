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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("AS warhead extension class." +
		"These warheads check for the Air TargetType when detonated inair!")]
	public abstract class WarheadAS : Warhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Extra search radius beyond maximum spread. If set to zero (default), it will automatically scale to the largest health shape.",
			"Custom overrides should not be necessary under normal circumstances.")]
		public WDist VictimScanRadius = WDist.Zero;

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
			foreach (var victim in world.FindActorsInCircle(pos, VictimScanRadius))
			{
				if (checkTargetType && !IsValidAgainst(victim, firedBy))
					continue;

				var healthInfo = victim.Info.TraitInfoOrDefault<HealthInfo>();
				if (healthInfo == null)
					continue;

				// If the impact position is within any HitShape, we have a direct hit
				var activeShapes = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled);
				if (activeShapes.Any(i => i.Info.Type.DistanceFromEdge(pos, victim).Length <= 0))
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
					validImpact = IsValidTarget(new string[] { "Air" });
					break;
				case ImpactType.Ground:
					var tileInfo = world.Map.GetTerrainInfo(targetTile);
					validImpact = IsValidTarget(tileInfo.TargetTypes);
					break;
			}

			return validImpact;
		}

		void IRulesetLoaded<WeaponInfo>.RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (VictimScanRadius == WDist.Zero)
				VictimScanRadius = Util.MinimumRequiredVictimScanRadius(rules);
		}
	}
}
