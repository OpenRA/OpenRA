#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	[Desc("Grants promotion to actors.")]
	public class PromotionWarhead : WarheadAS
	{
		[Desc("Range of targets to be promoted.")]
		public readonly WDist Range = new WDist(2048);

		[Desc("Levels of promotion granted.")]
		public readonly int Levels = 1;

		[Desc("Suppress levelup effects?")]
		public readonly bool SuppressEffects = false;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			if (!IsValidImpact(target.CenterPosition, firedBy))
				return;

			var availableActors = firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in availableActors)
			{
				if (!IsValidAgainst(a, firedBy))
					return;

				var xp = a.TraitOrDefault<GainsExperience>();
				if (xp == null)
					return;

				xp.GiveLevels(Levels, SuppressEffects);
			}
		}
	}
}
