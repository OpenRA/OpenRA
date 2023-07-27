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
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Hunt : Activity
	{
		readonly IEnumerable<Actor> targets;
		readonly IMove move;

		public Hunt(Actor self)
		{
			move = self.Trait<IMove>();
			var attack = self.Trait<AttackBase>();
			targets = self.World.ActorsHavingTrait<Huntable>().Where(
				a => self != a && !a.IsDead && a.IsInWorld && a.AppearsHostileTo(self)
				&& a.IsTargetableBy(self) && attack.HasAnyValidWeapons(Target.FromActor(a)));
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			var targetActor = targets.ClosestTo(self);
			if (targetActor == null)
				return false;

			// We want to keep 2 cells of distance from the target to prevent the pathfinder from thinking the target position is blocked.
			QueueChild(new AttackMoveActivity(self, () => move.MoveWithinRange(Target.FromCell(self.World, targetActor.Location), WDist.FromCells(2))));
			QueueChild(new Wait(25));
			return false;
		}
	}
}
