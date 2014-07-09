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
using OpenRA.Traits;

namespace OpenRA.GameRules
{
	public class HealthPercentageDamageWarhead : DamageWarhead
	{
		[Desc("Size of the area. Damage will be applied to this area.", "If two spreads are defined, the area of effect is a ring, where the second value is the inner radius.")]
		public readonly WRange[] Spread = { new WRange(43), WRange.Zero };

		public override void DoImpact(Target target, Actor firedBy, float firepowerModifier)
		{
			// Used by traits that damage a single actor, rather than a position
			if (target.Type == TargetType.Actor)
				DoImpact(target.Actor, firedBy, firepowerModifier);
			else
				DoImpact(target.CenterPosition, firedBy, firepowerModifier);
		}

		public void DoImpact(WPos pos, Actor firedBy, float firepowerModifier)
		{
			var world = firedBy.World;
			var range = Spread[0];
			var hitActors = world.FindActorsInCircle(pos, range);
			if (Spread.Length > 1 && Spread[1].Range > 0)
				hitActors.Except(world.FindActorsInCircle(pos, Spread[1]));

			foreach (var victim in hitActors)
				DoImpact(victim, firedBy, firepowerModifier);
		}

		public void DoImpact(Actor victim, Actor firedBy, float firepowerModifier)
		{
			if (IsValidAgainst(victim, firedBy))
			{
				var damage = GetDamageToInflict(victim, firedBy, firepowerModifier);
				if (damage != 0) // will be 0 if the target doesn't have HealthInfo
				{
					var healthInfo = victim.Info.Traits.Get<HealthInfo>();
					damage = (float)(damage / 100 * healthInfo.HP);
				}

				victim.InflictDamage(firedBy, (int)damage, this);
			}
		}

		public float GetDamageToInflict(Actor target, Actor firedBy, float modifier)
		{
			var healthInfo = target.Info.Traits.GetOrDefault<HealthInfo>();
			if (healthInfo == null)
				return 0;

			var rawDamage = (float)Damage;

			return rawDamage * modifier * EffectivenessAgainst(target.Info);
		}
	}
}
