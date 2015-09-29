#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		readonly IMove move;
		readonly IEnumerable<Actor> targets;

		public Hunt(Actor self)
		{
			move = self.Trait<IMove>();
			var attack = self.Trait<AttackBase>();
			targets = self.World.Actors.Where(a => self != a && !a.IsDead && a.IsInWorld && a.AppearsHostileTo(self)
				&& a.Info.HasTraitInfo<HuntableInfo>() && IsTargetable(a, self) && attack.HasAnyValidWeapons(Target.FromActor(a)));
		}

		bool IsTargetable(Actor self, Actor viewer)
		{
			return self.TraitsImplementing<ITargetable>().Any(t => t.IsTraitEnabled() && t.TargetableBy(self, viewer));
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var target = targets.ClosestTo(self);
			if (target == null)
				return this;

			return Util.SequenceActivities(
				new AttackMoveActivity(self, move.MoveTo(target.Location, 2)),
				new Wait(25),
				this);
		}
	}
}