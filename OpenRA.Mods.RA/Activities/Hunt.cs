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
using OpenRA.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Hunt : Activity
	{
		readonly IEnumerable<Actor> targets;

		public Hunt(Actor self)
		{
			var attack = self.Trait<AttackBase>();
			targets = self.World.Actors.Where(a => self != a && !a.IsDead && a.IsInWorld && a.AppearsHostileTo(self)
				&& a.HasTrait<Huntable>() && IsTargetable(a, self) && attack.HasAnyValidWeapons(Target.FromActor(a)));
		}

		bool IsTargetable(Actor self, Actor viewer)
		{
			var targetable = self.TraitOrDefault<ITargetable>();
			return targetable != null && targetable.TargetableBy(self, viewer);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;
			
			var target = targets.ClosestTo(self);
			if (target == null)
				return this;

			return Util.SequenceActivities(
				new AttackMoveActivity(self, new Move(self, target.Location, WRange.FromCells(2))),
				new Wait(25),
				this);
		}
	}
}