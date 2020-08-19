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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	public class LeapAttack : Activity, IActivityNotifyStanceChanged
	{
		readonly AttackLeapInfo info;
		readonly AttackLeap attack;
		readonly Mobile mobile;
		readonly bool allowMovement;
		readonly bool forceAttack;
		readonly Color? targetLineColor;

		Target target;
		Target lastVisibleTarget;
		bool useLastVisibleTarget;
		WDist lastVisibleMinRange;
		WDist lastVisibleMaxRange;
		BitSet<TargetableType> lastVisibleTargetTypes;
		Player lastVisibleOwner;

		public LeapAttack(Actor self, in Target target, bool allowMovement, bool forceAttack, AttackLeap attack, AttackLeapInfo info, Color? targetLineColor = null)
		{
			this.target = target;
			this.targetLineColor = targetLineColor;
			this.info = info;
			this.attack = attack;
			this.allowMovement = allowMovement;
			this.forceAttack = forceAttack;
			mobile = self.Trait<Mobile>();

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
			    || target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
			{
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
				lastVisibleMinRange = attack.GetMinimumRangeVersusTarget(target);
				lastVisibleMaxRange = attack.GetMaximumRangeVersusTarget(target);

				if (target.Type == TargetType.Actor)
				{
					lastVisibleOwner = target.Actor.Owner;
					lastVisibleTargetTypes = target.Actor.GetEnabledTargetTypes();
				}
				else if (target.Type == TargetType.FrozenActor)
				{
					lastVisibleOwner = target.FrozenActor.Owner;
					lastVisibleTargetTypes = target.FrozenActor.TargetTypes;
				}
			}
		}

		protected override void OnFirstRun(Actor self)
		{
			attack.IsAiming = true;
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling)
				return true;

			target = target.Recalculate(self.Owner, out var targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
			{
				lastVisibleTarget = Target.FromTargetPositions(target);
				lastVisibleMinRange = attack.GetMinimumRangeVersusTarget(target);
				lastVisibleMaxRange = attack.GetMaximumRangeVersusTarget(target);
				lastVisibleOwner = target.Actor.Owner;
				lastVisibleTargetTypes = target.Actor.GetEnabledTargetTypes();
			}

			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				return true;

			var pos = self.CenterPosition;
			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

			if (!checkTarget.IsInRange(pos, lastVisibleMaxRange) || checkTarget.IsInRange(pos, lastVisibleMinRange))
			{
				if (!allowMovement || lastVisibleMaxRange == WDist.Zero || lastVisibleMaxRange < lastVisibleMinRange)
					return true;

				QueueChild(mobile.MoveWithinRange(target, lastVisibleMinRange, lastVisibleMaxRange, checkTarget.CenterPosition, Color.Red));
				return false;
			}

			// Ready to leap, but target isn't visible
			if (targetIsHiddenActor || target.Type != TargetType.Actor)
				return true;

			// Target is not valid
			if (!target.IsValidFor(self) || !attack.HasAnyValidWeapons(target))
				return true;

			var edible = target.Actor.TraitOrDefault<EdibleByLeap>();
			if (edible == null || !edible.CanLeap(self))
				return true;

			// Can't leap yet
			if (attack.Armaments.All(a => a.IsReloading))
				return false;

			// Use CenterOfSubCell with ToSubCell instead of target.Centerposition
			// to avoid continuous facing adjustments as the target moves
			var targetMobile = target.Actor.TraitOrDefault<Mobile>();
			var targetSubcell = targetMobile != null ? targetMobile.ToSubCell : SubCell.Any;

			var destination = self.World.Map.CenterOfSubCell(target.Actor.Location, targetSubcell);
			var origin = self.World.Map.CenterOfSubCell(self.Location, mobile.FromSubCell);
			var desiredFacing = (destination - origin).Yaw;
			if (mobile.Facing != desiredFacing)
			{
				QueueChild(new Turn(self, desiredFacing));
				return false;
			}

			QueueChild(new Leap(self, target, mobile, targetMobile, info.Speed.Length, attack, edible));

			// Re-queue the child activities to kill the target if it didn't die in one go
			return false;
		}

		protected override void OnLastRun(Actor self)
		{
			attack.IsAiming = false;
		}

		void IActivityNotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
		{
			// Cancel non-forced targets when switching to a more restrictive stance if they are no longer valid for auto-targeting
			if (newStance > oldStance || forceAttack)
				return;

			// If lastVisibleTarget is invalid we could never view the target in the first place, so we just drop it here too
			if (!lastVisibleTarget.IsValidFor(self) || !autoTarget.HasValidTargetPriority(self, lastVisibleOwner, lastVisibleTargetTypes))
				target = Target.Invalid;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
				yield return new TargetLineNode(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value);
		}
	}
}
