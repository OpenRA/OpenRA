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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	/* non-turreted attack */
	public class Attack : Activity
	{
		[Flags]
		protected enum AttackStatus { UnableToAttack, NeedsToTurn, NeedsToMove, Attacking }

		readonly AttackFrontal[] attackTraits;
		readonly RevealsShroud[] revealsShroud;
		readonly IMove move;
		readonly IFacing facing;
		readonly IPositionable positionable;
		readonly bool forceAttack;

		protected Target target;
		Target lastVisibleTarget;
		WDist lastVisibleMaximumRange;
		bool useLastVisibleTarget;
		bool wasMovingWithinRange;

		WDist minRange;
		WDist maxRange;
		AttackStatus attackStatus = AttackStatus.UnableToAttack;

		public Attack(Actor self, Target target, bool allowMovement, bool forceAttack)
		{
			this.target = target;
			this.forceAttack = forceAttack;

			attackTraits = self.TraitsImplementing<AttackFrontal>().ToArray();
			revealsShroud = self.TraitsImplementing<RevealsShroud>().ToArray();
			facing = self.Trait<IFacing>();
			positionable = self.Trait<IPositionable>();

			move = allowMovement ? self.TraitOrDefault<IMove>() : null;

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
			    || target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
			{
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
				lastVisibleMaximumRange = attackTraits.Where(x => !x.IsTraitDisabled)
					.Min(x => x.GetMaximumRangeVersusTarget(target));
			}
		}

		protected virtual Target RecalculateTarget(Actor self, out bool targetIsHiddenActor)
		{
			return target.Recalculate(self.Owner, out targetIsHiddenActor);
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			if (IsCanceling)
				return NextActivity;

			bool targetIsHiddenActor;
			target = RecalculateTarget(self, out targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
			{
				lastVisibleTarget = Target.FromTargetPositions(target);
				lastVisibleMaximumRange = attackTraits.Where(x => !x.IsTraitDisabled)
					.Min(x => x.GetMaximumRangeVersusTarget(target));
			}

			var oldUseLastVisibleTarget = useLastVisibleTarget;
			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// If we are ticking again after previously sequencing a MoveWithRange then that move must have completed
			// Either we are in range and can see the target, or we've lost track of it and should give up
			if (wasMovingWithinRange && targetIsHiddenActor)
				return NextActivity;

			// Update target lines if required
			if (useLastVisibleTarget != oldUseLastVisibleTarget)
				self.SetTargetLine(useLastVisibleTarget ? lastVisibleTarget : target, Color.Red, false);

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				return NextActivity;

			wasMovingWithinRange = false;
			var pos = self.CenterPosition;
			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

			// We don't know where the target actually is, so move to where we last saw it
			if (useLastVisibleTarget)
			{
				// We've reached the assumed position but it is not there or we can't move any further - give up
				if (checkTarget.IsInRange(pos, lastVisibleMaximumRange) || move == null || lastVisibleMaximumRange == WDist.Zero)
					return NextActivity;

				// Move towards the last known position
				wasMovingWithinRange = true;
				QueueChild(self, move.MoveWithinRange(target, WDist.Zero, lastVisibleMaximumRange, checkTarget.CenterPosition, Color.Red), true);
				return this;
			}

			attackStatus = AttackStatus.UnableToAttack;

			foreach (var attack in attackTraits.Where(x => !x.IsTraitDisabled))
			{
				var status = TickAttack(self, attack);
				attack.IsAiming = status == AttackStatus.Attacking || status == AttackStatus.NeedsToTurn;
			}

			if (attackStatus.HasFlag(AttackStatus.NeedsToMove))
				wasMovingWithinRange = true;

			if (attackStatus >= AttackStatus.NeedsToTurn)
				return this;

			return NextActivity;
		}

		protected virtual AttackStatus TickAttack(Actor self, AttackFrontal attack)
		{
			if (!target.IsValidFor(self))
				return AttackStatus.UnableToAttack;

			if (attack.Info.AttackRequiresEnteringCell && !positionable.CanEnterCell(target.Actor.Location, null, false))
				return AttackStatus.UnableToAttack;

			if (!attack.Info.TargetFrozenActors && !forceAttack && target.Type == TargetType.FrozenActor)
			{
				// Try to move within range, drop the target otherwise
				if (move == null)
					return AttackStatus.UnableToAttack;

				var rs = revealsShroud
					.Where(Exts.IsTraitEnabled)
					.MaxByOrDefault(s => s.Range);

				// Default to 2 cells if there are no active traits
				var sightRange = rs != null ? rs.Range : WDist.FromCells(2);

				attackStatus |= AttackStatus.NeedsToMove;
				QueueChild(self, move.MoveWithinRange(target, sightRange, target.CenterPosition, Color.Red), true);
				return AttackStatus.NeedsToMove;
			}

			// Drop the target once none of the weapons are effective against it
			var armaments = attack.ChooseArmamentsForTarget(target, forceAttack).ToList();
			if (armaments.Count == 0)
				return AttackStatus.UnableToAttack;

			// Update ranges
			minRange = armaments.Max(a => a.Weapon.MinRange);
			maxRange = armaments.Min(a => a.MaxRange());

			var pos = self.CenterPosition;
			var mobile = move as Mobile;
			if (!target.IsInRange(pos, maxRange)
				|| (minRange.Length != 0 && target.IsInRange(pos, minRange))
				|| (mobile != null && !mobile.CanInteractWithGroundLayer(self)))
			{
				// Try to move within range, drop the target otherwise
				if (move == null)
					return AttackStatus.UnableToAttack;

				attackStatus |= AttackStatus.NeedsToMove;
				var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;
				QueueChild(self, move.MoveWithinRange(target, minRange, maxRange, checkTarget.CenterPosition, Color.Red), true);
				return AttackStatus.NeedsToMove;
			}

			var targetedPosition = attack.GetTargetPosition(pos, target);
			var desiredFacing = (targetedPosition - pos).Yaw.Facing;
			if (!Util.FacingWithinTolerance(facing.Facing, desiredFacing, ((AttackFrontalInfo)attack.Info).FacingTolerance))
			{
				attackStatus |= AttackStatus.NeedsToTurn;
				QueueChild(self, new Turn(self, desiredFacing), true);
				return AttackStatus.NeedsToTurn;
			}

			attackStatus |= AttackStatus.Attacking;
			DoAttack(self, attack, armaments);

			return AttackStatus.Attacking;
		}

		protected virtual void DoAttack(Actor self, AttackFrontal attack, IEnumerable<Armament> armaments)
		{
			if (!attack.IsTraitPaused)
				foreach (var a in armaments)
					a.CheckFire(self, facing, target);
		}
	}
}
