#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Only hits when an actor is targeted directly.")]
	public class ActorDamageWarhead : DamageWarhead
	{
		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (target.Type != TargetType.Actor)
				return;

			var victim = target.Actor;
			if (!IsValidAgainst(victim, firedBy))
				return;

			var damage = Util.ApplyPercentageModifiers(Damage, damageModifiers.Append(DamageVersus(victim)));
			victim.InflictDamage(firedBy, new Damage(damage, DamageTypes));
		}

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			// missed the target
		}
	}
}