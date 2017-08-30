#region Copyright & License Information
/*
 * Written by Boolbada of OP Mod.
 * Follows GPLv3 License as the OpenRA engine:
 *
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Yupgi_alert.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Warheads
{
	[Desc("Can this warhead lift the actor that has Tractable trait and move it next to self by force?")]
	public class TractorWarhead : DamageWarhead
	{
		[Desc("Let his be -1, 0, 1, or anything else to modify the traction speed.")]
		public readonly int CruiseSpeedMultiplier = 1;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (target.Type == TargetType.Actor)
				DoImpact(target.Actor, firedBy, damageModifiers);
		}

		public override void DoImpact(WPos pos, Actor firedBy, IEnumerable<int> damageModifiers) { }

		public override void DoImpact(Actor victim, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var targetTractable = victim.TraitOrDefault<Tractable>();
			if (targetTractable == null)
				return;

			targetTractable.Tract(victim, firedBy, CruiseSpeedMultiplier);
		}
	}
}
