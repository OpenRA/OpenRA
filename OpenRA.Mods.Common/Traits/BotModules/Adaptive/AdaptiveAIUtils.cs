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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public static class AdaptiveAIUtils
	{
		public static bool IsVisibleEnemy(Actor a, Player bot)
		{
			return a != null && !a.IsDead && a.IsInWorld
				&& a.AppearsHostileTo(bot.PlayerActor)
				&& a.CanBeViewedByPlayer(bot);
		}

		public static bool IsKnownEnemyFrozen(FrozenActor fa, Player bot)
		{
			return fa != null && fa.IsValid
				&& bot.RelationshipWith(fa.Owner) == PlayerRelationship.Enemy;
		}

		public static IEnumerable<Actor> VisibleEnemiesInCircle(World world, Player bot, WPos center, WDist radius)
		{
			return world.FindActorsInCircle(center, radius)
				.Where(a => IsVisibleEnemy(a, bot));
		}

		public static bool AllowsSuperweapons(string techLevel)
		{
			return techLevel == "unrestricted";
		}
	}
}
