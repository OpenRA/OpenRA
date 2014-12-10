#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA
{
	public class GrantConditionWarhead : Warhead
	{
		[Desc("The conditions to apply.")]
		public readonly string[] Conditions = { };

		[Desc("Duration of the condition level (in ticks). Set to 0 for a permanent condition level.")]
		public readonly int Duration = 0;

		public readonly WRange Range = WRange.FromCells(1);

		public override void DoImpact(Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			var actors = target.Type == TargetType.Actor ? new [] { target.Actor } :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in actors)
			{
				var um = a.TraitOrDefault<ConditionManager>();
				if (um == null)
					continue;

				foreach (var u in Conditions)
				{
					if (!um.AcceptsConditionType(a, u))
						continue;

					if (Duration > 0)
						um.GrantTimedCondition(a, u, Duration);
					else
						um.GrantCondition(a, u, this);
				}
			}
		}
	}
}
