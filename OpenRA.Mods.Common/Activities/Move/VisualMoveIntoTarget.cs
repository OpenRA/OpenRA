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
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class VisualMoveIntoTarget : Activity
	{
		readonly Mobile mobile;
		readonly Target target;
		readonly WDist targetMovementThreshold;
		WPos targetStartPos;

		public VisualMoveIntoTarget(Actor self, Target target, WDist targetMovementThreshold)
		{
			mobile = self.Trait<Mobile>();
			this.target = target;
			this.targetMovementThreshold = targetMovementThreshold;
		}

		protected override void OnFirstRun(Actor self)
		{
			targetStartPos = target.Positions.PositionClosestTo(self.CenterPosition);
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity != null)
			{
				ChildActivity = ActivityUtils.RunActivity(self, ChildActivity);
				if (ChildActivity != null)
					return this;
			}

			if (IsCanceling || target.Type == TargetType.Invalid)
				return NextActivity;

			if (mobile.IsTraitDisabled || mobile.IsTraitPaused)
				return this;

			var currentPos = self.CenterPosition;
			var targetPos = target.Positions.PositionClosestTo(currentPos);

			// Give up if the target has moved too far
			if (targetMovementThreshold > WDist.Zero && (targetPos - targetStartPos).LengthSquared > targetMovementThreshold.LengthSquared)
				return NextActivity;

			// Turn if required
			var delta = targetPos - currentPos;
			var facing = delta.HorizontalLengthSquared != 0 ? delta.Yaw.Facing : mobile.Facing;
			if (facing != mobile.Facing)
			{
				var turn = ActivityUtils.RunActivity(self, new Turn(self, facing));
				if (turn != null)
					QueueChild(self, turn);

				return this;
			}

			// Can complete the move in this step
			var speed = mobile.MovementSpeedForCell(self, self.Location);
			if (delta.LengthSquared <= speed * speed)
			{
				mobile.SetVisualPosition(self, targetPos);
				return NextActivity;
			}

			// Move towards the target
			mobile.SetVisualPosition(self, currentPos + delta * speed / delta.Length);

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return target;
		}
	}
}
