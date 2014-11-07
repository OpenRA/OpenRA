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

namespace OpenRA.Mods.Common
{
	public class SpreadDamageWarhead : DamageWarhead
	{
		[Desc("Range between falloff steps.")]
		public readonly WRange Spread = new WRange(43);

		[Desc("Ranges at which each Falloff step is defined. Overrides Spread.")]
		public WRange[] Range = null;

		[Desc("Damage percentage at each range step")]
		public readonly int[] Falloff = { 100, 37, 14, 5, 2, 1, 0 };

		public void InitializeRange()
		{
			if (Range != null)
			{
				if (Range.Length != 1 && Range.Length != Falloff.Length)
					throw new InvalidOperationException("Number of range values must be 1 or equal to the number of Falloff values.");

				for (var i = 0; i < Range.Length - 1; i++)
					if (Range[i] > Range[i + 1])
						throw new InvalidOperationException("Range values must be specified in an increasing order.");
			}
			else
				Range = Exts.MakeArray(Falloff.Length, i => i * Spread);
		}

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (Range == null)
				InitializeRange();

			var world = firedBy.World;
			var hitActors = world.FindActorsInCircle(pos, Range[Range.Length - 1]);

			foreach (var victim in hitActors)
			{
				if (!IsValidAgainst(victim, firedBy))
					continue;

				var localModifiers = damageModifiers;
				var healthInfo = victim.Info.Traits.GetOrDefault<HealthInfo>();
				if (healthInfo != null)
				{
					var distance = Math.Max(0, (victim.CenterPosition - pos).Length - healthInfo.Radius.Range);
					localModifiers = localModifiers.Append(GetDamageFalloff(distance));
				}

				DoImpact(victim, firedBy, localModifiers);
			}
		}

		int GetDamageFalloff(int distance)
		{
			var inner = Range[0].Range;
			for (var i = 1; i < Range.Length; i++)
			{
				var outer = Range[i].Range;
				if (outer > distance)
					return int2.Lerp(Falloff[i - 1], Falloff[i], distance - inner, outer - inner);

				inner = outer;
			}

			return 0;
		}
	}
}
