#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class TargetDamageWarhead : DamageWarhead
	{
		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			// Damages a single actor, rather than a position. Only supported by InstantHit for now.
			// TODO: Add support for 'area of damage'
			if (target.Type == TargetType.Actor)
				DoImpact(target.Actor, firedBy, damageModifiers);
		}

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			// For now this only displays debug overlay
			// TODO: Add support for 'area of effect' / multiple targets
			var world = firedBy.World;
			var debugOverlayRange = new[] { WDist.Zero, new WDist(128) };

			var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
				world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, debugOverlayRange, DebugOverlayColor);
		}

		public override void DoImpact(Actor victim, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!IsValidAgainst(victim, firedBy))
				return;

			// Cannot be damaged without a Health trait
			if (!victim.Info.HasTraitInfo<HealthInfo>())
				return;

			var closestActiveShape = victim.TraitsImplementing<HitShape>().Where(Exts.IsTraitEnabled)
				.MinByOrDefault(t => t.Info.Type.DistanceFromEdge(victim.CenterPosition, victim));

			// Cannot be damaged without an active HitShape
			if (closestActiveShape == null)
				return;

			var damage = Util.ApplyPercentageModifiers(Damage, damageModifiers.Append(DamageVersus(victim, closestActiveShape.Info)));
			victim.InflictDamage(firedBy, new Damage(damage, DamageTypes));

			var world = firedBy.World;
			if (world.LocalPlayer != null)
			{
				var debugOverlayRange = new[] { WDist.Zero, new WDist(128) };

				var debugVis = world.WorldActor.TraitOrDefault<DebugVisualizations>();
				if (debugVis != null && debugVis.CombatGeometry)
					world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(victim.CenterPosition, debugOverlayRange, DebugOverlayColor);
			}
		}
	}
}
