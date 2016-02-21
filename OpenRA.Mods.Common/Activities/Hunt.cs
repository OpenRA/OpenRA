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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Hunt : Activity
	{
		readonly IEnumerable<Actor> targets;

		public Hunt(Actor self)
		{
			var attack = self.Trait<AttackBase>();
			targets = self.World.ActorsHavingTrait<Huntable>().Where(
				a => self != a && !a.IsDead && a.IsInWorld && a.AppearsHostileTo(self)
				&& a.IsTargetableBy(self) && attack.HasAnyValidWeapons(Target.FromActor(a)));
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var target = targets.ClosestTo(self);
			if (target == null)
				return this;

			return ActivityUtils.SequenceActivities(
				new AttackMoveActivity(self, new Move(self, target.Location, WDist.FromCells(2))),
				new Wait(25),
				this);
		}
	}
}