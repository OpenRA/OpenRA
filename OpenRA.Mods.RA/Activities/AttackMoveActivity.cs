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
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class AttackMoveActivity : Activity
	{
		const int ScanInterval = 7;

		int scanTicks;
		bool hasMoved;
		Activity inner;
		AutoTarget autoTarget;

		public AttackMoveActivity(Actor self, Activity inner)
		{
			this.inner = inner;
			autoTarget = self.TraitOrDefault<AutoTarget>();
			hasMoved = false;
		}

		public override Activity Tick(Actor self)
		{
			if (autoTarget != null)
			{
				// If the actor hasn't moved since the activity was issued
				if (!hasMoved)
					autoTarget.ResetScanTimer();

				if (--scanTicks <= 0)
				{
					var attackActivity = autoTarget.ScanAndAttack(self);
					if (attackActivity != null)
					{
						if (!hasMoved)
							return attackActivity;

						self.QueueActivity(false, attackActivity);
					}
					scanTicks = ScanInterval;
				}
			}

			hasMoved = true;

			if (inner == null)
				return NextActivity;

			inner = Util.RunActivity(self, inner);

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
