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
		Target Target;
		int Range;
		int nextPathTime;

		const int delayBetweenPathingAttempts = 20;
		const int delaySpread = 5;

		public Follow(Target target, int range)
		{
			Target = target;
			Range = range;
		}

		public override Activity Tick( Actor self )
		{
			if (IsCanceled) return NextActivity;
			if (!Target.IsValid) return NextActivity;

			var inRange = ( Target.CenterPosition.ToCPos() - self.Location ).LengthSquared < Range * Range;

			if( inRange ) return this;
			if (--nextPathTime > 0) return this;

			nextPathTime = self.World.SharedRandom.Next(delayBetweenPathingAttempts - delaySpread,
				delayBetweenPathingAttempts + delaySpread);

			var mobile = self.Trait<Mobile>();
			return Util.SequenceActivities( mobile.MoveWithinRange( Target, new WRange(1024*Range) ), this );
		}
	}
}
