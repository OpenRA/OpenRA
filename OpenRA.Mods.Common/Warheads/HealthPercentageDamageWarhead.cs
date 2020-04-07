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

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	public class HealthPercentageDamageWarhead : TargetDamageWarhead
	{
		protected override void InflictDamage(Actor victim, Actor firedBy, HitShape shape, IEnumerable<int> damageModifiers)
		{
			if (shape.Health == null)
				return;

			var damage = Util.ApplyPercentageModifiers(shape.Health.Info.HP, damageModifiers.Append(Damage, DamageVersus(victim, shape)));
			shape.Health.InflictDamage(victim, firedBy, new Damage(damage, DamageTypes), false);
		}
	}
}
