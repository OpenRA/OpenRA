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
		readonly bool isAssaultMove;
		AutoTarget autoTarget;
		ConditionManager conditionManager;
		AttackMove attackMove;
		int token = ConditionManager.InvalidConditionToken;

		public AttackMoveActivity(Actor self, Func<Activity> getInner, bool assaultMoving = false)
		{
			this.getInner = getInner;
			autoTarget = self.TraitOrDefault<AutoTarget>();
			conditionManager = self.TraitOrDefault<ConditionManager>();
			attackMove = self.TraitOrDefault<AttackMove>();
			isAssaultMove = assaultMoving;
			ChildHasPriority = false;
		}

		protected override void OnFirstRun(Actor self)
		{
			// Start moving.
			QueueChild(getInner());

			if (conditionManager == null || attackMove == null)
				return;

			if (!isAssaultMove && !string.IsNullOrEmpty(attackMove.Info.AttackMoveCondition))
				token = conditionManager.GrantCondition(self, attackMove.Info.AttackMoveCondition);
			else if (isAssaultMove && !string.IsNullOrEmpty(attackMove.Info.AssaultMoveCondition))
				token = conditionManager.GrantCondition(self, attackMove.Info.AssaultMoveCondition);
		}

		public override bool Tick(Actor self)
		{
			// We are not currently attacking a target, so scan for new targets.
			if (!IsCanceling && ChildActivity != null && ChildActivity.NextActivity == null && autoTarget != null)
			{
				// ScanForTarget already limits the scanning rate for performance so we don't need to do that here.
				var target = autoTarget.ScanForTarget(self, false, true);
				if (target.Type != TargetType.Invalid)
				{
					// We have found a target so cancel the current move activity and queue attack activities.
					ChildActivity.Cancel(self);
					var attackBases = autoTarget.ActiveAttackBases;
					foreach (var ab in attackBases)
						QueueChild(ab.GetAttackActivity(self, target, false, false));

					// Make sure to continue moving when the attack activities have finished.
					QueueChild(getInner());
				}
			}

			// The last queued childactivity is guaranteed to be the inner move, so if the childactivity
			// queue is empty it means we have reached our destination and there are no more enemies on our path.
			return TickChild(self);
		}

		protected override void OnLastRun(Actor self)
		{
			if (conditionManager != null && token != ConditionManager.InvalidConditionToken)
				token = conditionManager.RevokeCondition(self, token);
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
