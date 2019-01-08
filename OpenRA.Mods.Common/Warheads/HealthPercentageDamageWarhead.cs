#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class HealthPercentageDamageWarhead : TargetDamageWarhead
	{
		protected override void InflictDamage(Actor victim, Actor firedBy, HitShapeInfo hitshapeInfo, IEnumerable<int> damageModifiers)
		{
			var healthInfo = victim.Info.TraitInfo<HealthInfo>();
			var damage = Util.ApplyPercentageModifiers(healthInfo.HP, damageModifiers.Append(Damage, DamageVersus(victim, hitshapeInfo)));
			victim.InflictDamage(firedBy, new Damage(damage, DamageTypes));
		}
	}
}
