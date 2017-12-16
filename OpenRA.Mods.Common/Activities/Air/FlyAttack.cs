#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyAttack : Activity
	{
		readonly Target target;
		readonly Aircraft aircraft;
		readonly AttackPlane attackPlane;

		readonly bool autoReloads;
		int ticksUntilTurn;

		public FlyAttack(Actor self, Target target)
		{
			this.target = target;
			aircraft = self.Trait<Aircraft>();
			attackPlane = self.TraitOrDefault<AttackPlane>();
			ticksUntilTurn = attackPlane.AttackPlaneInfo.AttackTurnDelay;
			autoReloads = self.TraitsImplementing<AmmoPool>().All(p => p.AutoReloads);
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

			// If all valid weapons have depleted their ammo and RearmBuilding is defined, return to RearmBuilding to reload and then resume the activity
			if (!autoReloads && aircraft.Info.RearmBuildings.Any() && attackPlane.Armaments.All(x => x.IsTraitPaused || !x.Weapon.IsValidAgainst(target, self.World, self)))
				return ActivityUtils.SequenceActivities(new ReturnToBase(self, aircraft.Info.AbortOnResupply), this);

			if (attackPlane != null)
				attackPlane.DoAttack(self, target);

			if (ChildActivity == null)
			{
				if (IsCanceled)
					return NextActivity;

				// TODO: This should fire each weapon at its maximum range
				if (attackPlane != null && target.IsInRange(self.CenterPosition, attackPlane.Armaments.Where(Exts.IsTraitEnabled).Select(a => a.Weapon.MinRange).Min()))
					ChildActivity = ActivityUtils.SequenceActivities(new FlyTimed(ticksUntilTurn, self), new Fly(self, target), new FlyTimed(ticksUntilTurn, self));
				else
					ChildActivity = ActivityUtils.SequenceActivities(new Fly(self, target), new FlyTimed(ticksUntilTurn, self));

				// HACK: This needs to be done in this round-about way because TakeOff doesn't behave as expected when it doesn't have a NextActivity.
				if (self.World.Map.DistanceAboveTerrain(self.CenterPosition).Length < aircraft.Info.MinAirborneAltitude)
					ChildActivity = ActivityUtils.SequenceActivities(new TakeOff(self), ChildActivity);
			}

			ActivityUtils.RunActivity(self, ChildActivity);

			return this;
		}
	}
}
