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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class HealthPercentageDamageWarhead : DamageWarhead
	{
		[Desc("Size of the area. Damage will be applied to this area.", "If two spreads are defined, the area of effect is a ring, where the second value is the inner radius.")]
		public readonly WDist[] Spread = { new WDist(43) };

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var world = firedBy.World;

			if (world.LocalPlayer != null)
			{
				var devMode = world.LocalPlayer.PlayerActor.TraitOrDefault<DeveloperMode>();
				if (devMode != null && devMode.ShowCombatGeometry)
					world.WorldActor.Trait<WarheadDebugOverlay>().AddImpact(pos, Spread, DebugOverlayColor);
			}

			var range = Spread[0];
			var hitActors = world.FindActorsInCircle(pos, range);
			if (Spread.Length > 1 && Spread[1].Length > 0)
				hitActors = hitActors.Except(world.FindActorsInCircle(pos, Spread[1]));

			foreach (var victim in hitActors)
				DoImpact(victim, firedBy, damageModifiers);
		}

		public override void DoImpact(Actor victim, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!IsValidAgainst(victim, firedBy))
				return;

			var healthInfo = victim.Info.TraitInfoOrDefault<HealthInfo>();
			if (healthInfo == null)
				return;

			// Damage is measured as a percentage of the target health
			var damage = Util.ApplyPercentageModifiers(healthInfo.HP, damageModifiers.Append(Damage, DamageVersus(victim)));
			victim.InflictDamage(firedBy, damage, this);
		}
	}
}
