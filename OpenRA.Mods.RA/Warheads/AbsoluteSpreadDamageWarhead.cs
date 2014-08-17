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
	public class AbsoluteSpreadDamageWarhead : DamageWarhead
	{
		[Desc("Maximum spread of the associated SpreadFactor.")]
		public readonly WRange[] Spread = { new WRange(43) };

		[Desc("What factor to multiply the Damage by for this spread range.", "Each factor specified must have an associated Spread defined.")]
		public readonly float[] SpreadFactor = { 1f };

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
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

				// TODO: Keep currentFactor as int from the start
				foreach (var victim in hitActors)
					DoImpact(victim, firedBy, damageModifiers.Append((int)(currentFactor * 100)));
			}
		}
	}
}
