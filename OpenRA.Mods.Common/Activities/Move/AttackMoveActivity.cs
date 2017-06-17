#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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

		public override bool Cancel(Actor self, bool keepQueue = false)
		{
			if (!IsCanceled && inner != null && !inner.Cancel(self))
				return false;

			return base.Cancel(self, keepQueue);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (inner != null)
				return inner.GetTargets(self);

			return Target.None;
		}

		public override TargetLineNode? TargetLineNode(Actor self)
		{
			// Attack move is a special case.
			// It is natural to show combat while attacking something and
			// when it is not, it should show "move".
			if (inner != null)
			{
				if (inner is Turn)
					return inner.NextActivity.TargetLineNode(self);
				else
					return inner.RootActivity.TargetLineNode(self);
			}

			return null;
		}
	}
}
