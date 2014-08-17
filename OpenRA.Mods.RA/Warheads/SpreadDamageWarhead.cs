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
	public class SpreadDamageWarhead : DamageWarhead
	{
		[Desc("For Normal DamageModel: Distance from the explosion center at which damage is 1/2.")]
		public readonly WRange Spread = new WRange(43);

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var world = firedBy.World;
			var maxSpread = new WRange((int)(Spread.Range * (float)Math.Log(Math.Abs(Damage), 2)));
			var hitActors = world.FindActorsInCircle(pos, maxSpread);

			foreach (var victim in hitActors)
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				var localModifiers = damageModifiers;
				var healthInfo = victim.Info.Traits.GetOrDefault<HealthInfo>();
				if (healthInfo != null)
				{
					var distance = Math.Max(0, (victim.CenterPosition - pos).Length - healthInfo.Radius.Range);
					localModifiers = localModifiers.Append((int)(100 * GetDamageFalloff(distance * 1f / Spread.Range)));
				}

				DoImpact(victim, firedBy, localModifiers);
			}
		}

		static readonly float[] falloff =
		{
			1f, 0.3678795f, 0.1353353f, 0.04978707f,
			0.01831564f, 0.006737947f, 0.002478752f, 0.000911882f
		};

		static float GetDamageFalloff(float x)
		{
			var u = (int)x;
			if (u >= falloff.Length - 1) return 0;
			var t = x - u;
			return (falloff[u] * (1 - t)) + (falloff[u + 1] * t);
		}
	}
}
