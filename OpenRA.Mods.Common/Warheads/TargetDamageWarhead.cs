#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class TargetDamageWarhead : DamageWarhead
	{
		[Desc("Damage will be applied to actors in this area. A value of zero means only targeted actor will be damaged.")]
		public readonly WDist Spread = WDist.Zero;

		protected override void DoImpact(WPos pos, Actor firedBy, WarheadArgs args)
		{
			if (Spread == WDist.Zero)
				return;

			var debugVis = firedBy.World.WorldActor.TraitOrDefault<DebugVisualizations>();
			if (debugVis != null && debugVis.CombatGeometry)
				firedBy.World.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, new[] { WDist.Zero, Spread }, DebugOverlayColor);

			foreach (var victim in firedBy.World.FindActorsOnCircle(pos, Spread))
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				var closestActiveShape = victim.TraitsImplementing<HitShape>()
					.Where(Exts.IsTraitEnabled)
					.Select(s => Pair.New(s, s.DistanceFromEdge(victim, pos)))
					.MinByOrDefault(s => s.Second);

				// Cannot be damaged without an active HitShape.
				if (closestActiveShape.First == null)
					continue;

				// Cannot be damaged if HitShape is outside Spread.
				if (closestActiveShape.Second > Spread)
					continue;

				InflictDamage(victim, firedBy, closestActiveShape.First, args);
			}
		}
	}
}
