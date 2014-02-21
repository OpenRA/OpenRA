#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;
using OpenRA.Mods.RA.Move;

namespace OpenRA.Mods.RA.Activities
{
	public class Follow : Activity
	{
		Target target;
		IMove move;
		WDist range;
		int nextPathTime;

		const int delayBetweenPathingAttempts = 20;
		const int delaySpread = 5;

		public Follow(Actor self, Target target, WDist range)
		{
			this.target = target;
			move = self.Trait<IMove>();
			this.range = range;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			if (target.IsInRange(self.CenterPosition, range) || --nextPathTime > 0)
				return this;

			nextPathTime = self.World.SharedRandom.Next(delayBetweenPathingAttempts - delaySpread,
				delayBetweenPathingAttempts + delaySpread);

			return Util.SequenceActivities(move.MoveWithinRange(target, range), this);
		}
	}
}
