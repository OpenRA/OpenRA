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

using System;
using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class AttackMoveActivity : Activity
	{
		readonly Func<Activity> getInner;
		public readonly bool IsAssaultMove;
		Activity inner;
		Activity attack;
		AutoTarget autoTarget;
		bool moving;

		public AttackMoveActivity(Actor self, Func<Activity> getInner, bool assaultMoving = false)
		{
			this.getInner = getInner;
			autoTarget = self.TraitOrDefault<AutoTarget>();
			moving = false;
			IsAssaultMove = assaultMoving;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceling)
			{
				if (attack != null)
				{
					attack = ActivityUtils.RunActivity(self, attack);
					return this;
				}

				if (inner != null)
				{
					inner = ActivityUtils.RunActivity(self, inner);
					return this;
				}

				return NextActivity;
			}

			if (attack == null && autoTarget != null)
			{
				var target = autoTarget.ScanForTarget(self, true, true);
				if (target.Type != TargetType.Invalid)
				{
					if (inner != null)
						inner.Cancel(self);

					var attackBases = autoTarget.ActiveAttackBases;
					foreach (var ab in attackBases)
					{
						if (attack == null)
							attack = ab.GetAttackActivity(self, target, true, false);
						else
							attack = ActivityUtils.SequenceActivities(self, attack, ab.GetAttackActivity(self, target, true, false));
						ab.OnQueueAttackActivity(self, target, false, true, false);
					}

					moving = false;
				}
			}

			if (attack == null && inner == null)
			{
				if (moving)
					return NextActivity;

				inner = getInner();
				moving = true;
			}

			if (inner == null)
				attack = ActivityUtils.RunActivity(self, attack);

			inner = ActivityUtils.RunActivity(self, inner);
			return this;
		}

		public override void Cancel(Actor self, bool keepQueue = false)
		{
			if (!IsCanceling && inner != null)
				inner.Cancel(self);

			if (!IsCanceling && attack != null)
				attack.Cancel(self);

			base.Cancel(self, keepQueue);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (inner != null)
				return inner.GetTargets(self);

			return Target.None;
		}
	}
}
