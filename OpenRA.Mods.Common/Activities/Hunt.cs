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

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (IsCanceling)
				return NextActivity;

			var target = targets.ClosestTo(self);
			if (target == null)
				return this;

			QueueChild(self, new AttackMoveActivity(self, () => move.MoveTo(target.Location, 2)), true);
			QueueChild(self, new Wait(25));
			return this;
		}
	}
}
