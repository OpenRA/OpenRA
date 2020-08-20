#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		readonly bool isAssaultMove;
		readonly AutoTarget autoTarget;
		readonly AttackMove attackMove;

		bool runningInnerActivity = false;
		int token = Actor.InvalidConditionToken;
		Target target = Target.Invalid;

		public AttackMoveActivity(Actor self, Func<Activity> getInner, bool assaultMoving = false)
		{
			this.getInner = getInner;
			autoTarget = self.TraitOrDefault<AutoTarget>();
			attackMove = self.TraitOrDefault<AttackMove>();
			isAssaultMove = assaultMoving;
			ChildHasPriority = false;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (attackMove == null)
				return;

			if (isAssaultMove)
				token = self.GrantCondition(attackMove.Info.AssaultMoveCondition);
			else
				token = self.GrantCondition(attackMove.Info.AttackMoveCondition);
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return TickChild(self);

			// We are currently not attacking, so scan for new targets.
			if (autoTarget != null && (ChildActivity == null || runningInnerActivity))
			{
				// ScanForTarget already limits the scanning rate for performance so we don't need to do that here.
				target = autoTarget.ScanForTarget(self, false, true);

				// Cancel the current inner activity and queue attack activities if we find a new target.
				if (target.Type != TargetType.Invalid)
				{
					runningInnerActivity = false;
					ChildActivity?.Cancel(self);

					foreach (var ab in autoTarget.ActiveAttackBases)
						QueueChild(ab.GetAttackActivity(self, AttackSource.AttackMove, target, false, false));
				}

				// Continue with the inner activity (or queue a new one) when there are no targets.
				if (ChildActivity == null)
				{
					runningInnerActivity = true;
					QueueChild(getInner());
				}
			}

			// If the inner activity finished, we have reached our destination and there are no more enemies on our path.
			return TickChild(self) && runningInnerActivity;
		}

		protected override void OnLastRun(Actor self)
		{
			if (token != Actor.InvalidConditionToken)
				token = self.RevokeCondition(token);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (ChildActivity != null)
				return ChildActivity.GetTargets(self);

			return Target.None;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			foreach (var n in getInner().TargetLineNodes(self))
				yield return n;

			yield break;
		}
	}
}
