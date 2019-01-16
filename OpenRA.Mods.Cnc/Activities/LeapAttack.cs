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
using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	public class LeapAttack : Activity
	{
		readonly AttackLeapInfo info;
		readonly AttackLeap attack;
		readonly Mobile mobile;
		readonly bool allowMovement;

		Target target;
		Target lastVisibleTarget;
		bool useLastVisibleTarget;
		WDist lastVisibleMinRange;
		WDist lastVisibleMaxRange;

		public LeapAttack(Actor self, Target target, bool allowMovement, AttackLeap attack, AttackLeapInfo info)
		{
			this.target = target;
			this.info = info;
			this.attack = attack;
			this.allowMovement = allowMovement;
			mobile = self.Trait<Mobile>();

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
			    || target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
			{
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
				lastVisibleMinRange = attack.GetMinimumRangeVersusTarget(target);
				lastVisibleMaxRange = attack.GetMaximumRangeVersusTarget(target);
			}
		}

		protected override void OnFirstRun(Actor self)
		{
			attack.IsAiming = true;
		}

		public override Activity Tick(Actor self)
		{
			// Run this even if the target became invalid to avoid visual glitches
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			if (IsCanceled)
				return NextActivity;

			bool targetIsHiddenActor;
			target = target.Recalculate(self.Owner, out targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
			{
				lastVisibleTarget = Target.FromTargetPositions(target);
				lastVisibleMinRange = attack.GetMinimumRangeVersusTarget(target);
				lastVisibleMaxRange = attack.GetMaximumRangeVersusTarget(target);
			}

			var oldUseLastVisibleTarget = useLastVisibleTarget;
			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Update target lines if required
			if (useLastVisibleTarget != oldUseLastVisibleTarget)
				self.SetTargetLine(useLastVisibleTarget ? lastVisibleTarget : target, Color.Red, false);

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				return NextActivity;

			var pos = self.CenterPosition;
			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

			if (!checkTarget.IsInRange(pos, lastVisibleMaxRange) || checkTarget.IsInRange(pos, lastVisibleMinRange))
			{
				if (!allowMovement)
					return NextActivity;

				QueueChild(mobile.MoveWithinRange(target, lastVisibleMinRange, lastVisibleMaxRange, checkTarget.CenterPosition, Color.Red));
				return this;
			}

			// Ready to leap, but target isn't visible
			if (targetIsHiddenActor || target.Type != TargetType.Actor)
				return NextActivity;

			// Target is not valid
			if (!target.IsValidFor(self) || !attack.HasAnyValidWeapons(target))
				return NextActivity;

			var edible = target.Actor.TraitOrDefault<EdibleByLeap>();
			if (edible == null || !edible.CanLeap(self))
				return NextActivity;

			// Can't leap yet
			if (attack.Armaments.All(a => a.IsReloading))
				return this;

			// Use CenterOfSubCell with ToSubCell instead of target.Centerposition
			// to avoid continuous facing adjustments as the target moves
			var targetMobile = target.Actor.TraitOrDefault<Mobile>();
			var targetSubcell = targetMobile != null ? targetMobile.ToSubCell : SubCell.Any;

			var destination = self.World.Map.CenterOfSubCell(target.Actor.Location, targetSubcell);
			var origin = self.World.Map.CenterOfSubCell(self.Location, mobile.FromSubCell);
			var desiredFacing = (destination - origin).Yaw.Facing;
			if (mobile.Facing != desiredFacing)
			{
				QueueChild(new Turn(self, desiredFacing));
				return this;
			}

			QueueChild(new Leap(self, target, mobile, targetMobile, info.Speed.Length, attack, edible));

			// Re-queue the child activities to kill the target if it didn't die in one go
			return this;
		}

		protected override void OnLastRun(Actor self)
		{
			attack.IsAiming = false;
		}
	}
}
