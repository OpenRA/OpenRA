#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Apply damage based on the target's health.")]
	public class HealthPercentageDamageWarhead : TargetDamageWarhead
	{
		protected override void InflictDamage(Actor victim, Actor firedBy, HitShape shape, WarheadArgs args)
		{
			var healthInfo = victim.Info.TraitInfo<HealthInfo>();
			var damage = Util.ApplyPercentageModifiers(healthInfo.HP, args.DamageModifiers.Append(Damage, DamageVersus(victim, shape, args)));
			victim.InflictDamage(firedBy, new Damage(damage, DamageTypes));
		}
	}
}
