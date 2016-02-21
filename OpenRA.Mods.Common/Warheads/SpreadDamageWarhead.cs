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
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class SpreadDamageWarhead : DamageWarhead, IRulesetLoaded<WeaponInfo>
	{
		[Desc("Range between falloff steps.")]
		public readonly WDist Spread = new WDist(43);

		[Desc("Extra search radius beyond maximum spread. Required to ensure damage to actors with large health radius.")]
		public readonly WDist TargetExtraSearchRadius = new WDist(1536);

		[Desc("Damage percentage at each range step")]
		public readonly int[] Falloff = { 100, 37, 14, 5, 0 };

		[Desc("Ranges at which each Falloff step is defined. Overrides Spread.")]
		public WDist[] Range = null;

		public void RulesetLoaded(Ruleset rules, WeaponInfo info)
		{
			if (Range != null)
			{
				if (Range.Length != 1 && Range.Length != Falloff.Length)
					throw new YamlException("Number of range values must be 1 or equal to the number of Falloff values.");

				for (var i = 0; i < Range.Length - 1; i++)
					if (Range[i] > Range[i + 1])
						throw new YamlException("Range values must be specified in an increasing order.");
			}
			else
				Range = Exts.MakeArray(Falloff.Length, i => i * Spread);
		}

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var world = firedBy.World;

			if (world.LocalPlayer != null)
			{
				var devMode = world.LocalPlayer.PlayerActor.TraitOrDefault<DeveloperMode>();
				if (devMode != null && devMode.ShowCombatGeometry)
					world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, Range, DebugOverlayColor);
			}

			// This only finds actors where the center is within the search radius,
			// so we need to search beyond the maximum spread to account for actors with large health radius
			var hitActors = world.FindActorsInCircle(pos, Range[Range.Length - 1] + TargetExtraSearchRadius);

			foreach (var victim in hitActors)
			{
				var healthInfo = victim.Info.TraitInfoOrDefault<HealthInfo>();
				if (healthInfo == null)
					continue;

				var distance = healthInfo.Shape.DistanceFromEdge(pos, victim);
				var localModifiers = damageModifiers.Append(GetDamageFalloff(distance.Length));

				DoImpact(victim, firedBy, localModifiers);
			}
		}

		int GetDamageFalloff(int distance)
		{
			var inner = Range[0].Length;
			for (var i = 1; i < Range.Length; i++)
			{
				var outer = Range[i].Length;
				if (outer > distance)
					return int2.Lerp(Falloff[i - 1], Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}
	}
}
