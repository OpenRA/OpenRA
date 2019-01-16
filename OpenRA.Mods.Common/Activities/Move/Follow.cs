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

using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Follow : Activity
	{
		readonly Target target;
		readonly WDist minRange;
		readonly WDist maxRange;
		readonly IMove move;
		readonly Color? targetLineColor;

		public Follow(Actor self, Target target, WDist minRange, WDist maxRange,
			WPos? initialTargetPosition, Color? targetLineColor = null)
		{
			this.target = target;
			this.minRange = minRange;
			this.maxRange = maxRange;
			this.targetLineColor = targetLineColor;

			move = self.Trait<IMove>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			var cachedPosition = target.CenterPosition;
			var path = move.MoveWithinRange(target, minRange, maxRange, targetLineColor: targetLineColor);

			// We are already in range, so wait until the target moves before doing anything
			if (target.IsInRange(self.CenterPosition, maxRange) && !target.IsInRange(self.CenterPosition, minRange))
			{
				var wait = new WaitFor(() => !target.IsValidFor(self) || target.CenterPosition != cachedPosition);
				return ActivityUtils.SequenceActivities(wait, path, this);
			}

			return ActivityUtils.SequenceActivities(path, this);
		}
	}
}
