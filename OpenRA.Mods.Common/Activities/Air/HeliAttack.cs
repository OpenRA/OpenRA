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

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliAttack : Activity
	{
		readonly Aircraft aircraft;
		readonly AttackAircraft attackAircraft;
		readonly Rearmable rearmable;

		Target target;
		Target lastVisibleTarget;
		WDist lastVisibleMinimumRange;
		WDist lastVisibleMaximumRange;
		bool useLastVisibleTarget;
		bool hasTicked;

		public HeliAttack(Actor self, Target target)
		{
			this.target = target;
			aircraft = self.Trait<Aircraft>();
			attackAircraft = self.Trait<AttackAircraft>();
			rearmable = self.TraitOrDefault<Rearmable>();

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
			    || target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
			{
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
				lastVisibleMinimumRange = attackAircraft.GetMinimumRangeVersusTarget(target);
				lastVisibleMaximumRange = attackAircraft.GetMaximumRangeVersusTarget(target);
			}
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
				Cancel(self);

			if (IsCanceling)
			{
				// Cancel the requested target, but keep firing on it while in range
				if (attackAircraft.Info.PersistentTargeting)
				{
					attackAircraft.OpportunityTarget = attackAircraft.RequestedTarget;
					attackAircraft.OpportunityForceAttack = attackAircraft.RequestedForceAttack;
					attackAircraft.OpportunityTargetIsPersistentTarget = true;
				}

				attackAircraft.RequestedTarget = Target.Invalid;
				return NextActivity;
			}

			// Check that AttackFollow hasn't cancelled the target by modifying attack.Target
			// Having both this and AttackFollow modify that field is a horrible hack.
			if (hasTicked && attackAircraft.RequestedTarget.Type == TargetType.Invalid)
				return NextActivity;

			if (attackAircraft.IsTraitPaused)
				return this;

			bool targetIsHiddenActor;
			attackAircraft.RequestedTarget = target = target.Recalculate(self.Owner, out targetIsHiddenActor);
			attackAircraft.RequestedTargetLastTick = self.World.WorldTick;
			hasTicked = true;

			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
			{
				lastVisibleTarget = Target.FromTargetPositions(target);
				lastVisibleMinimumRange = attackAircraft.GetMinimumRangeVersusTarget(target);
				lastVisibleMaximumRange = attackAircraft.GetMaximumRangeVersusTarget(target);
			}

			var oldUseLastVisibleTarget = useLastVisibleTarget;
			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Update target lines if required
			if (useLastVisibleTarget != oldUseLastVisibleTarget)
				self.SetTargetLine(useLastVisibleTarget ? lastVisibleTarget : target, Color.Red, false);

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
			{
				attackAircraft.RequestedTarget = Target.Invalid;
				return NextActivity;
			}

			// If all valid weapons have depleted their ammo and Rearmable trait exists, return to RearmActor to reload and then resume the activity
			if (rearmable != null && !useLastVisibleTarget && attackAircraft.Armaments.All(x => x.IsTraitPaused || !x.Weapon.IsValidAgainst(target, self.World, self)))
			{
				QueueChild(self, new ReturnToBase(self, aircraft.Info.AbortOnResupply), true);
				return this;
			}

			var pos = self.CenterPosition;
			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

			// We've reached the assumed position of lastVisibleTarget but it is not there - give up
			if (useLastVisibleTarget && checkTarget.IsInRange(pos, lastVisibleMaximumRange))
			{
				attackAircraft.RequestedTarget = Target.Invalid;
				return NextActivity;
			}

			var delta = attackAircraft.GetTargetPosition(pos, checkTarget) - pos;
			var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : aircraft.Facing;
			var inRange = target.IsInRange(pos, lastVisibleMaximumRange) && !target.IsInRange(pos, lastVisibleMinimumRange);
			var dat = self.World.Map.DistanceAboveTerrain(aircraft.CenterPosition);

			// If not within range, fly towards target (or the last known position if useLastVisibleTarget is true).
			// If VTOL and not at CruiseAltitude or within range & CanHover & not facing target, adjust altitude and/or facing first.
			if ((inRange && aircraft.Info.CanHover && aircraft.Facing != desiredFacing) || (aircraft.Info.VTOL && dat != aircraft.Info.CruiseAltitude))
				Fly.FlyTick(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude, -1, MovementType.Vertical | MovementType.Turn);
			else if (!inRange)
				QueueChild(self, aircraft.MoveWithinRange(checkTarget, lastVisibleMinimumRange, lastVisibleMaximumRange, checkTarget.CenterPosition, Color.Red), true);

			return this;
		}
	}
}
