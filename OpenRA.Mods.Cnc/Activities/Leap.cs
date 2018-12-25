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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	public class Leap : Activity
	{
		readonly Mobile mobile;
		readonly WPos destination, origin;
		readonly CPos destinationCell;
		readonly SubCell destinationSubCell = SubCell.Any;
		readonly int length;
		readonly AttackLeap attack;
		readonly EdibleByLeap edible;
		readonly Target target;

		bool canceled = false;
		bool jumpComplete = false;
		int ticks = 0;

		public Leap(Actor self, Target target, Mobile mobile, Mobile targetMobile, int speed, AttackLeap attack, EdibleByLeap edible)
		{
			this.mobile = mobile;
			this.attack = attack;
			this.target = target;
			this.edible = edible;

			destinationCell = target.Actor.Location;
			if (targetMobile != null)
				destinationSubCell = targetMobile.ToSubCell;

			origin = self.World.Map.CenterOfSubCell(self.Location, mobile.FromSubCell);
			destination = self.World.Map.CenterOfSubCell(destinationCell, destinationSubCell);
			length = Math.Max((origin - destination).Length / speed, 1);
		}

		protected override void OnFirstRun(Actor self)
		{
			// First check if we are still allowed to leap
			// We need an extra boolean as using Cancel() in OnFirstRun doesn't work
			canceled = !edible.GetLeapAtBy(self) || target.Type != TargetType.Actor;

			IsInterruptible = false;

			if (canceled)
				return;

			attack.GrantLeapCondition(self);
		}

		public override Activity Tick(Actor self)
		{
			if (canceled)
				return NextActivity;

			// Correct the visual position after we jumped
			if (jumpComplete)
			{
				if (ChildActivity == null)
					return NextActivity;

				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			var position = length > 1 ? WPos.Lerp(origin, target.CenterPosition, ticks, length - 1) : target.CenterPosition;
			mobile.SetVisualPosition(self, position);

			// We are at the destination
			if (++ticks >= length)
			{
				mobile.IsMoving = false;

				// Revoke the run condition
				attack.IsAiming = false;

				// Move to the correct subcells, if our target actor uses subcells
				// (This does not update the visual position!)
				mobile.SetLocation(destinationCell, destinationSubCell, destinationCell, destinationSubCell);

				// Revoke the condition before attacking, as it is usually used to pause the attack trait
				attack.RevokeLeapCondition(self);
				attack.DoAttack(self, target);

				jumpComplete = true;
				QueueChild(mobile.VisualMove(self, position, self.World.Map.CenterOfSubCell(destinationCell, destinationSubCell)));

				return this;
			}

			mobile.IsMoving = true;

			return this;
		}

		protected override void OnLastRun(Actor self)
		{
			attack.RevokeLeapCondition(self);
			base.OnLastRun(self);
		}

		protected override void OnActorDispose(Actor self)
		{
			attack.RevokeLeapCondition(self);
			base.OnActorDispose(self);
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return Target.FromPos(ticks < length / 2 ? origin : destination);
		}
	}
}
