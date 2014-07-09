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
	public class AbsoluteSpreadDamageWarhead : DamageWarhead
	{
		[Desc("Maximum spread of the associated SpreadFactor.")]
		public readonly WRange[] Spread = { new WRange(43) };

		[Desc("What factor to multiply the Damage by for this spread range.", "Each factor specified must have an associated Spread defined.")]
		public readonly float[] SpreadFactor = { 1f };

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

			for (var i = 0; i < Spread.Length; i++)
			{
				var currentSpread = Spread[i];
				var currentFactor = SpreadFactor[i];
				var previousSpread = WRange.Zero;
				if (i > 0)
					previousSpread = Spread[i - 1];
				if (currentFactor <= 0f)
					continue;

				var hitActors = world.FindActorsInCircle(pos, currentSpread);
				if (previousSpread.Range > 0)
					hitActors.Except(world.FindActorsInCircle(pos, previousSpread));

				foreach (var victim in hitActors)
					if (IsValidAgainst(victim, firedBy))
					{
						var damage = GetDamageToInflict(victim, firedBy, firepowerModifier * currentFactor);
						victim.InflictDamage(firedBy, damage, this);
					}
			}
		}

		public void DoImpact(Actor victim, Actor firedBy, float firepowerModifier)
		{
			if (IsValidAgainst(victim, firedBy))
			{
				var currentFactor = SpreadFactor[0];
				var damage = (int)GetDamageToInflict(victim, firedBy, firepowerModifier * currentFactor);
				victim.InflictDamage(firedBy, damage, this);
			}
		}

		public int GetDamageToInflict(Actor target, Actor firedBy, float modifier)
		{
			var healthInfo = target.Info.Traits.GetOrDefault<HealthInfo>();
			if (healthInfo == null)
				return 0;

			var rawDamage = (float)Damage;

			return (int)(rawDamage * modifier * EffectivenessAgainst(target.Info));
		}
	}
}
