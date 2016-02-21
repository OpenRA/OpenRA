#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Follow : Activity
	{
		readonly Target target;
		readonly WDist minRange;
		readonly WDist maxRange;
		readonly IMove move;

		public Follow(Actor self, Target target, WDist minRange, WDist maxRange)
		{
			this.target = target;
			this.minRange = minRange;
			this.maxRange = maxRange;

			move = self.Trait<IMove>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			var cachedPosition = target.CenterPosition;
			var path = move.MoveWithinRange(target, minRange, maxRange);

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
