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
	public class HeliAttack : Activity, IActivityNotifyStanceChanged
	{
		readonly Aircraft aircraft;
		readonly AttackAircraft attackAircraft;
		readonly Rearmable rearmable;
		readonly bool forceAttack;

		Target target;
		Target lastVisibleTarget;
		WDist lastVisibleMaximumRange;
		BitSet<TargetableType> lastVisibleTargetTypes;
		Player lastVisibleOwner;
		bool useLastVisibleTarget;
		bool hasTicked;

		public HeliAttack(Actor self, Target target, bool forceAttack)
		{
			this.target = target;
			this.forceAttack = forceAttack;
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
				lastVisibleMaximumRange = attackAircraft.GetMaximumRangeVersusTarget(target);
				lastVisibleOwner = target.Actor.Owner;
				lastVisibleTargetTypes = target.Actor.GetEnabledTargetTypes();
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

			// Update facing
			var delta = attackAircraft.GetTargetPosition(pos, checkTarget) - pos;
			var desiredFacing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : aircraft.Facing;
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);
			if (HeliFly.AdjustAltitude(self, aircraft, aircraft.Info.CruiseAltitude))
				return this;

			// We don't know where the target actually is, so move to where we last saw it
			if (useLastVisibleTarget)
			{
				// We've reached the assumed position but it is not there - give up
				if (checkTarget.IsInRange(pos, lastVisibleMaximumRange))
				{
					attackAircraft.RequestedTarget = Target.Invalid;
					return NextActivity;
				}

				// Fly towards the last known position
				aircraft.SetPosition(self, aircraft.CenterPosition + aircraft.FlyStep(desiredFacing));
				return this;
			}

			// Fly towards the target
			if (!target.IsInRange(pos, attackAircraft.GetMaximumRangeVersusTarget(target)))
				aircraft.SetPosition(self, aircraft.CenterPosition + aircraft.FlyStep(desiredFacing));

			// Fly backwards from the target
			if (target.IsInRange(pos, attackAircraft.GetMinimumRangeVersusTarget(target)))
			{
				// Facing 0 doesn't work with the following position change
				var facing = 1;
				if (desiredFacing != 0)
					facing = desiredFacing;
				else if (aircraft.Facing != 0)
					facing = aircraft.Facing;
				aircraft.SetPosition(self, aircraft.CenterPosition + aircraft.FlyStep(-facing));
			}

			return this;
		}

		void IActivityNotifyStanceChanged.StanceChanged(Actor self, AutoTarget autoTarget, UnitStance oldStance, UnitStance newStance)
		{
			// Cancel non-forced targets when switching to a more restrictive stance if they are no longer valid for auto-targeting
			if (newStance > oldStance || forceAttack)
				return;

			if (!autoTarget.HasValidTargetPriority(self, lastVisibleOwner, lastVisibleTargetTypes))
				attackAircraft.RequestedTarget = Target.Invalid;
		}
	}
}
