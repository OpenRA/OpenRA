#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyAttack : Activity
	{
		readonly Aircraft aircraft;
		readonly AttackAircraft attackAircraft;
		readonly bool attackOnlyVisibleTargets;
		readonly Rearmable rearmable;

		int ticksUntilTurn;
		Target target;
		bool canHideUnderFog;
		protected Target Target
		{
			get
			{
				return target;
			}

			private set
			{
				target = value;
				if (target.Type == TargetType.Actor)
					canHideUnderFog = target.Actor.Info.HasTraitInfo<HiddenUnderFogInfo>();
			}
		}

		public FlyAttack(Actor self, Target target, bool attackOnlyVisibleTargets = true)
		{
			Target = target;
			aircraft = self.Trait<Aircraft>();
			attackAircraft = self.Trait<AttackAircraft>();
			rearmable = self.TraitOrDefault<Rearmable>();
			this.attackOnlyVisibleTargets = attackOnlyVisibleTargets;
			ticksUntilTurn = attackAircraft.AttackAircraftInfo.AttackTurnDelay;
		}

		public override Activity Tick(Actor self)
		{
			// Refuse to take off if it would land immediately again.
			if (aircraft.ForceLanding)
			{
				Cancel(self);
				return NextActivity;
			}

			if (!target.IsValidFor(self))
				return NextActivity;

			var pos = self.CenterPosition;
			var targetPos = attackAircraft.GetTargetPosition(pos, target);
			if (attackOnlyVisibleTargets && target.Type == TargetType.Actor && canHideUnderFog
				&& !target.Actor.CanBeViewedByPlayer(self.Owner))
			{
				var newTarget = Target.FromCell(self.World, self.World.Map.CellContaining(targetPos));

				Cancel(self);
				self.SetTargetLine(newTarget, Color.Green);
				return new Fly(self, newTarget);
			}

			// If all valid weapons have depleted their ammo and Rearmable trait exists, return to RearmActor to reload and then resume the activity
			if (rearmable != null && attackAircraft.Armaments.All(x => x.IsTraitPaused || !x.Weapon.IsValidAgainst(target, self.World, self)))
				return ActivityUtils.SequenceActivities(new ReturnToBase(self, aircraft.Info.AbortOnResupply), this);

			if (aircraft.Info.CanHover)
			{
				if (IsCanceled)
					return NextActivity;

				var dist = targetPos - pos;

				// Can rotate facing while ascending
				var desiredFacing = dist.HorizontalLengthSquared != 0 ? dist.Yaw.Facing : aircraft.Facing;

				if (Fly.FlyToward(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude, moveVerticalOnly: true))
					return this;

				// Fly towards the target when outside max range,
				// fly backwards from the target when inside min range,
				// and just turn towards target when at the right distance.
				var withinMaxRange = target.IsInRange(pos, attackAircraft.GetMaximumRangeVersusTarget(target));
				var withinMinRange = target.IsInRange(pos, attackAircraft.GetMinimumRangeVersusTarget(target));
				if (!withinMaxRange)
				{
					Fly.FlyToward(self, aircraft, desiredFacing, aircraft.Info.CruiseAltitude);
					return this;
				}
				else if (withinMinRange)
				{
					// Facing 0 doesn't work with the following position change
					var facing = 1;
					if (desiredFacing != 0)
						facing = desiredFacing;
					else if (aircraft.Facing != 0)
						facing = aircraft.Facing;

					Fly.FlyToward(self, aircraft, facing, aircraft.Info.CruiseAltitude, moveVerticalOnly: false, flyBackward: true);
					return this;
				}
				else if (withinMaxRange && !withinMinRange && desiredFacing != aircraft.Facing)
					aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.TurnSpeed);

				attackAircraft.DoAttack(self, target);
			}
			else
			{
				attackAircraft.DoAttack(self, target);

				if (ChildActivity == null)
				{
					if (IsCanceled)
						return NextActivity;

					// TODO: This should fire each weapon at its maximum range
					if (target.IsInRange(self.CenterPosition, attackAircraft.Armaments.Where(Exts.IsTraitEnabled).Select(a => a.Weapon.MinRange).Min()))
						ChildActivity = ActivityUtils.SequenceActivities(new FlyTimed(ticksUntilTurn, self), new Fly(self, target), new FlyTimed(ticksUntilTurn, self));
					else
						ChildActivity = ActivityUtils.SequenceActivities(new Fly(self, target), new FlyTimed(ticksUntilTurn, self));

					// HACK: This needs to be done in this round-about way because TakeOff doesn't behave as expected when it doesn't have a NextActivity.
					// TODO: Fix this, if possible.
					if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length < aircraft.Info.MinAirborneAltitude)
						ChildActivity = ActivityUtils.SequenceActivities(new TakeOff(self), ChildActivity);
				}

				ActivityUtils.RunActivity(self, ChildActivity);
			}

			return this;
		}
	}
}
