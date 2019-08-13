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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyAttack : Activity, IActivityNotifyStanceChanged
	{
		readonly Aircraft aircraft;
		readonly AttackAircraft attackAircraft;
		readonly Rearmable rearmable;
		readonly bool forceAttack;
		readonly Color? targetLineColor;

		Target target;
		Target lastVisibleTarget;
		WDist lastVisibleMaximumRange;
		BitSet<TargetableType> lastVisibleTargetTypes;
		Player lastVisibleOwner;
		bool useLastVisibleTarget;
		bool hasTicked;
		bool returnToBase;
		int remainingTicksUntilTurn;

		public FlyAttack(Actor self, Target target, bool forceAttack, Color? targetLineColor)
		{
			this.target = target;
			this.forceAttack = forceAttack;
			this.targetLineColor = targetLineColor;

			aircraft = self.Trait<Aircraft>();
			attackAircraft = self.Trait<AttackAircraft>();
			rearmable = self.TraitOrDefault<Rearmable>();

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
				|| target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
			{
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
				lastVisibleMaximumRange = attackAircraft.GetMaximumRangeVersusTarget(target);

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

			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
				Cancel(self);

			if (IsCanceling)
				return true;

			// Check that AttackFollow hasn't cancelled the target by modifying attack.Target
			// Having both this and AttackFollow modify that field is a horrible hack.
			if (hasTicked && attackAircraft.RequestedTarget.Type == TargetType.Invalid)
				return true;

			if (attackAircraft.IsTraitPaused)
				return false;

			bool targetIsHiddenActor;
			target = target.Recalculate(self.Owner, out targetIsHiddenActor);
			attackAircraft.SetRequestedTarget(self, target, forceAttack);
			hasTicked = true;

			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
			{
				lastVisibleTarget = Target.FromTargetPositions(target);
				lastVisibleMaximumRange = attackAircraft.GetMaximumRangeVersusTarget(target);
				lastVisibleOwner = target.Actor.Owner;
				lastVisibleTargetTypes = target.Actor.GetEnabledTargetTypes();
			}

			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				return true;

			// If all valid weapons have depleted their ammo and Rearmable trait exists, return to RearmActor to reload
			// and resume the activity after reloading if AbortOnResupply is set to 'false'
			if (rearmable != null && !useLastVisibleTarget && attackAircraft.Armaments.All(x => x.IsTraitPaused || !x.Weapon.IsValidAgainst(target, self.World, self)))
			{
				QueueChild(new ReturnToBase(self));
				returnToBase = true;
				return attackAircraft.Info.AbortOnResupply;
			}

			var pos = self.CenterPosition;
			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

			// We don't know where the target actually is, so move to where we last saw it
			if (useLastVisibleTarget)
			{
				// We've reached the assumed position but it is not there - give up
				if (checkTarget.IsInRange(pos, lastVisibleMaximumRange))
					return true;

				// Fly towards the last known position
				QueueChild(new Fly(self, target, WDist.Zero, lastVisibleMaximumRange, checkTarget.CenterPosition, Color.Red));
				return false;
			}

			var delta = attackAircraft.GetTargetPosition(pos, target) - pos;
			var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : aircraft.Facing;

			QueueChild(new TakeOff(self));

			var minimumRange = attackAircraft.Info.AttackType == AirAttackType.Strafe ? WDist.Zero : attackAircraft.GetMinimumRangeVersusTarget(target);

			// When strafing we must move forward for a minimum number of ticks after passing the target.
			if (remainingTicksUntilTurn > 0)
			{
				Fly.FlyTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
				remainingTicksUntilTurn--;
			}

			// Move into range of the target.
			else if (!target.IsInRange(pos, lastVisibleMaximumRange) || target.IsInRange(pos, minimumRange))
				QueueChild(aircraft.MoveWithinRange(target, minimumRange, lastVisibleMaximumRange, target.CenterPosition, Color.Red));

			// The aircraft must keep moving forward even if it is already in an ideal position.
			else if (!aircraft.Info.CanHover || attackAircraft.Info.AttackType == AirAttackType.Strafe)
			{
				Fly.FlyTick(self, aircraft, aircraft.Facing, aircraft.Info.CruiseAltitude);
				remainingTicksUntilTurn = attackAircraft.Info.AttackTurnDelay;
			}

			// Turn to face the target if required.
			else if (!attackAircraft.TargetInFiringArc(self, target, attackAircraft.Info.FacingTolerance))
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);

			return false;
		}

		protected override void OnLastRun(Actor self)
		{
			// Cancel the requested target, but keep firing on it while in range
			attackAircraft.ClearRequestedTarget();
		}

		void IActivityNotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
		{
			// Cancel non-forced targets when switching to a more restrictive stance if they are no longer valid for auto-targeting
			if (newStance > oldStance || forceAttack)
				return;

			if (!autoTarget.HasValidTargetPriority(self, lastVisibleOwner, lastVisibleTargetTypes))
				attackAircraft.ClearRequestedTarget();
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
			{
				if (returnToBase)
					foreach (var n in ChildActivity.TargetLineNodes(self))
						yield return n;
				if (!returnToBase || !attackAircraft.Info.AbortOnResupply)
					yield return new TargetLineNode(useLastVisibleTarget ? lastVisibleTarget : target, targetLineColor.Value);
			}
		}
	}
}
