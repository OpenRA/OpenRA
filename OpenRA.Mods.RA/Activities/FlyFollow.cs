#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class FlyFollow : Activity
	{
		Target target;
		Plane plane;
		WRange minRange;
		WRange maxRange;

		public FlyFollow(Actor self, Target target, WRange minRange, WRange maxRange)
		{
			this.target = target;
			plane = self.Trait<Plane>();
			this.minRange = minRange;
			this.maxRange = maxRange;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			if (target.IsInRange(self.CenterPosition, maxRange) && !target.IsInRange(self.CenterPosition, minRange))
			{
				Fly.FlyToward(self, plane, plane.Facing, plane.Info.CruiseAltitude);
				return this;
			}

			return Util.SequenceActivities(new Fly(self, target, minRange, maxRange), this);
		}
	}
}
