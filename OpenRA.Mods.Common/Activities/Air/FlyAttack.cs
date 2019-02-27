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
	public class FlyAttack : Activity
	{
		readonly Aircraft aircraft;
		readonly AttackAircraft attackAircraft;
		readonly Rearmable rearmable;
		Target target;
		Target lastVisibleTarget;
		WDist lastVisibleMaximumRange;
		bool useLastVisibleTarget;

		int ticksUntilTurn;

		public FlyAttack(Actor self, Target target)
		{
			this.target = target;
			aircraft = self.Trait<Aircraft>();
			attackAircraft = self.Trait<AttackAircraft>();
			rearmable = self.TraitOrDefault<Rearmable>();
			ticksUntilTurn = attackAircraft.AttackAircraftInfo.AttackTurnDelay;

			// The target may become hidden between the initial order request and the first tick (e.g. if queued)
			// Moving to any position (even if quite stale) is still better than immediately giving up
			if ((target.Type == TargetType.Actor && target.Actor.CanBeViewedByPlayer(self.Owner))
			    || target.Type == TargetType.FrozenActor || target.Type == TargetType.Terrain)
			{
				lastVisibleTarget = Target.FromPos(target.CenterPosition);
				lastVisibleMaximumRange = attackAircraft.GetMaximumRangeVersusTarget(target);
			}
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
				Cancel(self);

			if (IsCanceled)
				return NextActivity;

			bool targetIsHiddenActor;
			target = target.Recalculate(self.Owner, out targetIsHiddenActor);
			if (!targetIsHiddenActor && target.Type == TargetType.Actor)
			{
				lastVisibleTarget = Target.FromTargetPositions(target);
				lastVisibleMaximumRange = attackAircraft.GetMaximumRangeVersusTarget(target);
			}

			var oldUseLastVisibleTarget = useLastVisibleTarget;
			useLastVisibleTarget = targetIsHiddenActor || !target.IsValidFor(self);

			// Update target lines if required
			if (useLastVisibleTarget != oldUseLastVisibleTarget)
				self.SetTargetLine(useLastVisibleTarget ? lastVisibleTarget : target, Color.Red, false);

			// Target is hidden or dead, and we don't have a fallback position to move towards
			if (useLastVisibleTarget && !lastVisibleTarget.IsValidFor(self))
				return NextActivity;

			// If all valid weapons have depleted their ammo and Rearmable trait exists, return to RearmActor to reload and then resume the activity
			if (rearmable != null && !useLastVisibleTarget && attackAircraft.Armaments.All(x => x.IsTraitPaused || !x.Weapon.IsValidAgainst(target, self.World, self)))
				return ActivityUtils.SequenceActivities(self, new ReturnToBase(self, aircraft.Info.AbortOnResupply), this);

			var pos = self.CenterPosition;
			var checkTarget = useLastVisibleTarget ? lastVisibleTarget : target;

			// We don't know where the target actually is, so move to where we last saw it
			if (useLastVisibleTarget)
			{
				// We've reached the assumed position but it is not there - give up
				if (checkTarget.IsInRange(pos, lastVisibleMaximumRange))
					return NextActivity;

				// Fly towards the last known position
				return ActivityUtils.SequenceActivities(self,
					new Fly(self, target, WDist.Zero, lastVisibleMaximumRange, checkTarget.CenterPosition, Color.Red),
					this);
			}

			// If we reach here the target is guaranteed to be both visible and alive, so use target instead of checkTarget.
			// The target may not be in range, but try attacking anyway...
			attackAircraft.DoAttack(self, target);

			if (ChildActivity == null)
			{
				// TODO: This should fire each weapon at its maximum range
				if (attackAircraft != null && target.IsInRange(self.CenterPosition, attackAircraft.GetMinimumRange()))
					ChildActivity = ActivityUtils.SequenceActivities(self,
						new FlyTimed(ticksUntilTurn, self),
						new Fly(self, target, checkTarget.CenterPosition, Color.Red),
						new FlyTimed(ticksUntilTurn, self));
				else
					ChildActivity = ActivityUtils.SequenceActivities(self,
						new Fly(self, target, checkTarget.CenterPosition, Color.Red),
						new FlyTimed(ticksUntilTurn, self));

				// HACK: This needs to be done in this round-about way because TakeOff doesn't behave as expected when it doesn't have a NextActivity.
				if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length < aircraft.Info.MinAirborneAltitude)
					ChildActivity = ActivityUtils.SequenceActivities(self, new TakeOff(self), ChildActivity);
			}

			ActivityUtils.RunActivity(self, ChildActivity);

			return this;
		}
	}
}
