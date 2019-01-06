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
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Cnc.Traits;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Activities
{
	public class LeapAttack : Activity
	{
		readonly Target target;
		readonly AttackLeapInfo info;
		readonly AttackLeap attack;
		readonly Mobile mobile, targetMobile;
		readonly EdibleByLeap edible;
		readonly bool allowMovement;
		readonly IFacing facing;

		public LeapAttack(Actor self, Target target, bool allowMovement, AttackLeap attack, AttackLeapInfo info)
		{
			this.target = target;
			this.info = info;
			this.attack = attack;
			this.allowMovement = allowMovement;

			mobile = self.Trait<Mobile>();
			facing = self.TraitOrDefault<IFacing>();

			if (target.Type == TargetType.Actor)
			{
				targetMobile = target.Actor.TraitOrDefault<Mobile>();
				edible = target.Actor.TraitOrDefault<EdibleByLeap>();
			}

			attack.IsAiming = true;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || edible == null)
				return NextActivity;

			// Run this even if the target became invalid to avoid visual glitches
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				return this;
			}

			if (target.Type != TargetType.Actor || !edible.CanLeap(self) || !target.IsValidFor(self) || !attack.HasAnyValidWeapons(target))
				return NextActivity;

			var minRange = attack.GetMinimumRangeVersusTarget(target);
			var maxRange = attack.GetMaximumRangeVersusTarget(target);
			if (!target.IsInRange(self.CenterPosition, maxRange) || target.IsInRange(self.CenterPosition, minRange))
			{
				if (!allowMovement)
					return NextActivity;

				QueueChild(new MoveWithinRange(self, target, minRange, maxRange));
				return this;
			}

			if (attack.Armaments.All(a => a.IsReloading))
				return this;

			// Use CenterOfSubCell with ToSubCell instead of target.Centerposition
			// to avoid continuous facing adjustments as the target moves
			var targetSubcell = targetMobile != null ? targetMobile.ToSubCell : SubCell.Any;
			var destination = self.World.Map.CenterOfSubCell(target.Actor.Location, targetSubcell);
			var origin = self.World.Map.CenterOfSubCell(self.Location, mobile.FromSubCell);
			var desiredFacing = (destination - origin).Yaw.Facing;
			if (facing != null && facing.Facing != desiredFacing)
			{
				QueueChild(new Turn(self, desiredFacing));
				return this;
			}

			QueueChild(new Leap(self, target, mobile, targetMobile, info.Speed.Length, attack, edible));

			// Re-queue the child activities to kill the target if it didn't die in one go
			return this;
		}

		protected override void OnLastRun(Actor self)
		{
			attack.IsAiming = false;
			base.OnLastRun(self);
		}
	}
}
