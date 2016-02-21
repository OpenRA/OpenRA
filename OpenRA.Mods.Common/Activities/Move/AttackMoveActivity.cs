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
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class AttackMoveActivity : Activity
	{
		const int ScanInterval = 7;

		Activity inner;
		int scanTicks;
		AutoTarget autoTarget;

		public AttackMoveActivity(Actor self, Activity inner)
		{
			this.inner = inner;
			autoTarget = self.TraitOrDefault<AutoTarget>();
		}

		public override Activity Tick(Actor self)
		{
			if (autoTarget != null && --scanTicks <= 0)
			{
				autoTarget.ScanAndAttack(self, true);
				scanTicks = ScanInterval;
			}

			if (inner == null)
				return NextActivity;

			inner = ActivityUtils.RunActivity(self, inner);

			return this;
		}

		public override void Cancel(Actor self)
		{
			if (inner != null)
				inner.Cancel(self);

			base.Cancel(self);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (inner != null)
				return inner.GetTargets(self);

			return Target.None;
		}
	}
}
