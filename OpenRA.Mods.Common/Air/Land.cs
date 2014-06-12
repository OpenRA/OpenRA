#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Air
{
	public class Land : Activity
	{
		Target target;

		public Land(Target t) { target = t; }

		public override Activity Tick(Actor self)
		{
			if (!target.IsValidFor(self))
				Cancel(self);

			if (IsCanceled)
				return NextActivity;

			var plane = self.Trait<Plane>();
			var d = target.CenterPosition - self.CenterPosition;

			// The next move would overshoot, so just set the final position
			var move = plane.FlyStep(plane.Facing);
			if (d.HorizontalLengthSquared < move.HorizontalLengthSquared)
			{
				plane.SetPosition(self, target.CenterPosition);
				return NextActivity;
			}

			var desiredFacing = Util.GetFacing(d, plane.Facing);
			Fly.FlyToward(self, plane, desiredFacing, WRange.Zero);

			return this;
		}
	}
}
