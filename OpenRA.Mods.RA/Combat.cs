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

namespace OpenRA.Mods.RA
{
	// some utility bits that are shared between various things
	// TODO: Investigate the impact of moving this to WeaponInfo. Currently some crate actions also call DoExplosion.
	public static class Combat
	{
		public static void DoImpacts(WPos pos, Actor firedBy, WeaponInfo weapon, float damageModifier)
		{
			foreach (var wh in weapon.Warheads)
			{
				Action a;

				a = () => wh.DoImpact(pos, weapon, firedBy, damageModifier);
				if (wh.DelayTicks > 0)
					firedBy.World.AddFrameEndTask(
						w => w.Add(new DelayedAction(wh.DelayTicks, a)));
				else
					a();
			}
		}

		public static void DoExplosion(Actor attacker, string weapontype, WPos pos)
		{
			var weapon = attacker.World.Map.Rules.Weapons[weapontype.ToLowerInvariant()];
			if (weapon.Report != null && weapon.Report.Any())
				Sound.Play(weapon.Report.Random(attacker.World.SharedRandom), pos);

			DoImpacts(pos, attacker, weapon, 1f);
		}
	}
}
