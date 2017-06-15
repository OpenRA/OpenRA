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

			IsInterruptible = false; // Leaping can't be canceled mid air!
			attack.LeapBuffOn(self);

			QueueChild(new Leap(self, origin, destination, length, mobile));
			QueueChild(new CallFunc(() =>
			{
				if (!self.IsDead && !self.Disposed)
				{
					attack.LeapBuffOff(self);
					IsInterruptible = true;
					attack.DoAttack(self, target);
				}
			}));
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

		public override bool Cancel(Actor self, bool keepQueue = false)
		{
			if (ChildActivity is Leap)
				return false;

			return base.Cancel(self, keepQueue);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || target.Actor.IsDead || target.Actor.Disposed)
			{
				// Let leap finish otherwise there will be visual glitch.
				if (!(ChildActivity is Leap))
					return DebuffAndNextActivity(self);
			}

			if (!target.IsValidFor(self) || !attack.HasAnyValidWeapons(target) || mobile == null)
				return DebuffAndNextActivity(self);

			// Wait for reload before leaping as leaping makes self more vulnerable, although
			// this effect isn't very observable for attack dogs in RA mod
			if (attack.Armaments.All(a => a.IsReloading))
				return this;

			if (ChildActivity != null)
			{
				ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			// Assuming minRange == 0, otherwise why call it a leap attack?
			var range = attack.GetMaximumRange();
			if (!target.IsInRange(self.CenterPosition, range))
			{
				attack.ApproachBuffOn(self);
				QueueChild(new MoveWithinRange(self, target, WDist.Zero, range));
				QueueChild(new CallFunc(() => attack.ApproachBuffOff(self)));
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
