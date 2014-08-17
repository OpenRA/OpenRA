#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class HealthPercentageDamageWarhead : DamageWarhead
	{
		[Desc("Size of the area. Damage will be applied to this area.", "If two spreads are defined, the area of effect is a ring, where the second value is the inner radius.")]
		public readonly WRange[] Spread = { new WRange(43), WRange.Zero };

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var world = firedBy.World;
			var range = Spread[0];
			var hitActors = world.FindActorsInCircle(pos, range);
			if (Spread.Length > 1 && Spread[1].Range > 0)
				hitActors.Except(world.FindActorsInCircle(pos, Spread[1]));

			foreach (var victim in hitActors)
				DoImpact(victim, firedBy, damageModifiers);
		}

		public override void DoImpact(Actor victim, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var healthInfo = victim.Info.Traits.GetOrDefault<HealthInfo>();
			if (healthInfo == null)
				return;

			// Damage is measured as a percentage of the target health
			var damage = Util.ApplyPercentageModifiers(healthInfo.HP, damageModifiers.Append(Damage, DamageVersus(victim.Info)));
			victim.InflictDamage(firedBy, damage, this);
		}
	}
}
