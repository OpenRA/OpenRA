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
using OpenRA.Mods.AS.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Warheads
{
	public class GrantExternalConditionASWarhead : Warhead
	{
		[FieldLoader.Require]
		[Desc("The condition to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string Condition = null;

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		public readonly WDist Range = WDist.FromCells(1);

		[Desc("Maximum amount of this condition from the same firer.")]
		public readonly int MaxStacks = 1;

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in actors)
			{
				if (!IsValidAgainst(a, firedBy))
					continue;

				var ecm = a.TraitOrDefault<ExternalConditionStackManager>();
				if (ecm != null)
				{
					ecm.GrantExternalCondition(Condition, firedBy, Duration, MaxStacks);
					continue;
				}

				Log.Write("debug", "Warning! Actor " + firedBy + " fired a weapon which would apply an external condition " +
				"with stack controls to" + a +", but " + a + " lacks the ExternalConditionStackManager trait!");

				var cm = a.TraitOrDefault<ConditionManager>();

				// Condition token is ignored because we never revoke this condition.
				if (cm != null && cm.AcceptsExternalCondition(a, Condition, Duration > 0))
					cm.GrantCondition(a, Condition, true, Duration);
			}
		}
	}
}
