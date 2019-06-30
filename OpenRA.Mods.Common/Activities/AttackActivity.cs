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
	class AttackActivity : Activity, IActivityNotifyStanceChanged
	{
		readonly AttackFollow attack;
		readonly RevealsShroud[] revealsShroud;
		readonly IMove move;
		readonly Aircraft aircraft;
		readonly Rearmable rearmable;
		readonly bool forceAttack;
		readonly Color? targetLineColor;
		readonly int ticksUntilTurn;

		Target target;
		Target lastVisibleTarget;
		bool useLastVisibleTarget;
		WDist lastVisibleMaximumRange;
		WDist lastVisibleMinimumRange;
		BitSet<TargetableType> lastVisibleTargetTypes;
		Player lastVisibleOwner;
		bool wasMovingWithinRange;
		bool hasTicked;
		bool returnToBase;
		int remainingTicksUntilTurn;

		public AttackActivity(Actor self, Target target, bool allowMove, bool forceAttack, Color? targetLineColor = null)
		{
			attack = self.Trait<AttackFollow>();
			move = allowMove ? self.TraitOrDefault<IMove>() : null;
			aircraft = self.TraitOrDefault<Aircraft>();
			revealsShroud = self.TraitsImplementing<RevealsShroud>().ToArray();
			rearmable = self.TraitOrDefault<Rearmable>();
			ticksUntilTurn = attack.Info.AttackTurnDelay;

			this.target = target;
			this.forceAttack = forceAttack;
			this.targetLineColor = targetLineColor;

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
				|| target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
			{
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
				lastVisibleMaximumRange = attack.GetMaximumRangeVersusTarget(target);
				lastVisibleMinimumRange = attack.GetMinimumRangeVersusTarget(target);

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

		public override bool Tick(Actor self)
		{
			returnToBase = false;

			if (IsCanceling)
				return true;

			// Check that AttackFollow hasn't cancelled the target by modifying attack.Target
			// Having both this and AttackFollow modify that field is a horrible hack.
			if (hasTicked && attack.RequestedTarget.Type == TargetType.Invalid)
				return true;

			if (attack.IsTraitPaused)
				return false;

			bool targetIsHiddenActor;
			target = target.Recalculate(self.Owner, out targetIsHiddenActor);
			attack.SetRequestedTarget(self, target, forceAttack);
			hasTicked = true;

			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
			{
				lastVisibleTarget = Target.FromTargetPositions(target);
				lastVisibleMaximumRange = attack.GetMaximumRangeVersusTarget(target);
				lastVisibleMinimumRange = attack.GetMinimumRangeVersusTarget(target);
				lastVisibleOwner = target.Actor.Owner;
				lastVisibleTargetTypes = target.Actor.GetEnabledTargetTypes();

				// Try and sit at least one cell away from the min or max ranges to give some leeway if the target starts moving.
				if (move != null && target.Actor.Info.HasTraitInfo<IMoveInfo>())
				{
					var preferMinRange = Math.Min(lastVisibleMinimumRange.Length + 1024, lastVisibleMaximumRange.Length);
					var preferMaxRange = Math.Max(lastVisibleMaximumRange.Length - 1024, lastVisibleMinimumRange.Length);
					lastVisibleMaximumRange = new WDist((lastVisibleMaximumRange.Length - 1024).Clamp(preferMinRange, preferMaxRange));
				}
			}

			var maxRange = lastVisibleMaximumRange;
			var minRange = lastVisibleMinimumRange;
			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Most actors want to be able to see their target before shooting
			if (target.Type == TargetType.FrozenActor && !attack.Info.TargetFrozenActors && !forceAttack)
			{
				var rs = revealsShroud
					.Where(Exts.IsTraitEnabled)
					.MaxByOrDefault(s => s.Range);

				// Default to 2 cells if there are no active traits
				var sightRange = rs != null ? rs.Range : WDist.FromCells(2);
				if (sightRange < maxRange)
					maxRange = sightRange;
			}

			// If we are ticking again after previously sequencing a MoveWithRange then that move must have completed
			// Either we are in range and can see the target, or we've lost track of it and should give up
			if (wasMovingWithinRange && targetIsHiddenActor)
				return true;

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				return true;

			// If all valid weapons have depleted their ammo and Rearmable trait exists, return to RearmActor to reload and then resume the activity
			// TODO: Apply to ground units as well.
			if (aircraft != null && rearmable != null && !useLastVisibleTarget && attack.Armaments.All(x => x.IsTraitPaused || !x.Weapon.IsValidAgainst(target, self.World, self)))
			{
				QueueChild(new ReturnToBase(self));
				returnToBase = true;
				return attack.Info.AbortOnResupply;
			}

			var pos = self.CenterPosition;
			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

			// We don't know where the target actually is, so move to where we last saw it
			if (useLastVisibleTarget)
			{
				// We've reached the assumed position but it is not there - give up
				if (checkTarget.IsInRange(pos, maxRange) && !checkTarget.IsInRange(pos, minRange))
					return true;

				// We can't move into range, so give up
				if (move == null || maxRange == WDist.Zero || maxRange < minRange)
					return true;

				// Move towards the last known position
				wasMovingWithinRange = true;
				QueueChild(attack.Move.MoveWithinRange(target, minRange, maxRange, checkTarget.CenterPosition, Color.Red));
				return false;
			}

			if (aircraft != null)
				QueueChild(new TakeOff(self));

			// When strafing we must move forward for a minimum number of ticks after passing the target.
			// TODO: Apply to ground units as well.
			if (remainingTicksUntilTurn > 0)
			{
				Fly.FlyTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
				remainingTicksUntilTurn--;
			}

			// Default attack pattern --> Follow target and stop when in range.
			else if (!target.IsInRange(pos, maxRange) || target.IsInRange(pos, minRange))
			{
				// We can't move into range, so give up
				if (move == null || maxRange == WDist.Zero || maxRange < minRange)
					return true;

				QueueChild(attack.Move.MoveWithinRange(target, minRange, maxRange, target.CenterPosition, Color.Red));
			}

			// Strafing unit must keep moving forward even if it is already in an ideal position.
			// TODO: Apply to ground units as well.
			else if ((aircraft != null && !aircraft.Info.CanHover) || attack.Info.AttackType == AttackType.Strafe)
			{
				Fly.FlyTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
				remainingTicksUntilTurn = attack.Info.AttackTurnDelay;
			}

			// Turn to face the target if required.
			else if (!attack.TargetInFiringArc(self, target, attack.Info.FacingTolerance))
			{
				var delta = attack.GetTargetPosition(pos, target) - pos;
				var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : attack.Move.Facing;
				attack.Move.Facing = Util.TickFacing(attack.Move.Facing, desiredFacing, attack.Move.TurnSpeed);
			}

			return false;
		}

		protected override void OnLastRun(Actor self)
		{
			// Cancel the requested target, but keep firing on it while in range
			attack.ClearRequestedTarget();
		}

		void IActivityNotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
		{
			// Cancel non-forced targets when switching to a more restrictive stance if they are no longer valid for auto-targeting
			if (newStance > oldStance || forceAttack)
				return;

			if (!autoTarget.HasValidTargetPriority(self, lastVisibleOwner, lastVisibleTargetTypes))
				attack.ClearRequestedTarget();
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
			{
				if (returnToBase)
					foreach (var n in ChildActivity.TargetLineNodes(self))
						yield return n;
				if (!returnToBase || !attack.Info.AbortOnResupply)
					yield return new TargetLineNode(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value);
			}
		}
	}
}
