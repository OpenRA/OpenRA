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

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Cnc.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class LeapAttack : Activity
	{
		readonly Target target;
		readonly AttackLeapInfo info;
		readonly Mobile mobile;
		readonly AttackLeap attack;

		bool isApproachBuffOn = false;

		public LeapAttack(Actor self, Target target, bool allowMovement, AttackLeapInfo info)
		{
			this.target = target;
			this.info = info;
			mobile = allowMovement ? self.TraitOrDefault<Mobile>() : null;
			attack = self.Trait<AttackLeap>();
		}

		void QueueLeapActivity(Actor self)
		{
			var origin = self.World.Map.CenterOfSubCell(self.Location, mobile.FromSubCell);
			var targetMobile = target.Actor.TraitOrDefault<Mobile>();
			var targetSubcell = targetMobile != null ? targetMobile.FromSubCell : SubCell.Any;
			var destination = self.World.Map.CenterOfSubCell(target.Actor.Location, targetSubcell);
			var length = Math.Max((origin - destination).Length / info.Speed.Length, 1);

			attack.LeapBuffOn(self);
			QueueChild(new Turn(self, (destination - origin).Yaw.Facing));
			QueueChild(new Leap(self, origin, destination, length, mobile, attack, target));
		}

		Activity DebuffAndNextActivity(Actor self)
		{
			// When the target dies before approaching or leap (or even during the leap!)
			// Tick() continues to the next activity.
			// Here, we make sure that all the buffs are gone before proceeding.
			attack.ApproachBuffOff(self);
			attack.LeapBuffOff(self);
			return NextActivity;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return DebuffAndNextActivity(self);

			if (target.Actor.IsDead || !target.IsValidFor(self) || !attack.HasAnyValidWeapons(target) || mobile == null)
			{
				// let Leap finish otherwise we get visual glitch.
				if (ChildActivity == null || !(ChildActivity is Leap))
					return DebuffAndNextActivity(self);
			}

			// Wait for reload before leaping as leaping makes self more vulnerable, although
			// this effect isn't very observable for attack dogs in RA mod
			if (attack.Armaments.All(a => a.IsReloading))
				return this;

			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);

				// Child activity finished. Time to debuff.
				if (ChildActivity == null && isApproachBuffOn)
				{
					attack.ApproachBuffOff(self);
					isApproachBuffOn = false;
				}

				return this;
			}

			// Assuming minRange == 0, otherwise why call it a leap attack?
			var range = attack.GetMaximumRange();
			if (!target.IsInRange(self.CenterPosition, range))
			{
				attack.ApproachBuffOn(self);
				isApproachBuffOn = true;
				QueueChild(new MoveWithinRange(self, target, WDist.Zero, range));
				return this;
			}

			// Target is now in range. We moved, so freshly calculate leap stuff then leap.
			// Leap and doAttack are child activity so when they are done,
			// Tick() will queue them again to kill the target, if it doesn't die in one shot.
			QueueLeapActivity(self);
			return this;
		}
	}
}
