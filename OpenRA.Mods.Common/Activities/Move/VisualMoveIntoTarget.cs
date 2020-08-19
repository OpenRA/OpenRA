#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class VisualMoveIntoTarget : Activity
	{
		readonly Mobile mobile;
		readonly Target target;
		readonly Color? targetLineColor;
		readonly WDist targetMovementThreshold;
		WPos targetStartPos;

		public VisualMoveIntoTarget(Actor self, in Target target, WDist targetMovementThreshold, Color? targetLineColor = null)
		{
			mobile = self.Trait<Mobile>();
			this.target = target;
			this.targetMovementThreshold = targetMovementThreshold;
			this.targetLineColor = targetLineColor;
		}

		protected override void OnFirstRun(Actor self)
		{
			targetStartPos = target.Positions.PositionClosestTo(self.CenterPosition);
		}

		public override bool Tick(Actor self)
		{
			if (IsCanceling || target.Type == TargetType.Invalid)
				return true;

			if (mobile.IsTraitDisabled || mobile.IsTraitPaused)
				return false;

			var currentPos = self.CenterPosition;
			var targetPos = target.Positions.PositionClosestTo(currentPos);

			// Give up if the target has moved too far
			if (targetMovementThreshold > WDist.Zero && (targetPos - targetStartPos).LengthSquared > targetMovementThreshold.LengthSquared)
				return true;

			// Turn if required
			var delta = targetPos - currentPos;
			var facing = delta.HorizontalLengthSquared != 0 ? delta.Yaw : mobile.Facing;
			if (facing != mobile.Facing)
			{
				mobile.Facing = Util.TickFacing(mobile.Facing, facing, mobile.TurnSpeed);
				return false;
			}

			// Can complete the move in this step
			var speed = mobile.MovementSpeedForCell(self, self.Location);
			if (delta.LengthSquared <= speed * speed)
			{
				mobile.SetVisualPosition(self, targetPos);
				return true;
			}

			// Move towards the target
			mobile.SetVisualPosition(self, currentPos + delta * speed / delta.Length);
			return false;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			yield return target;
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			if (targetLineColor != null)
				yield return new TargetLineNode(target, targetLineColor.Value);
		}
	}
}
