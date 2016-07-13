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
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyCircleTimed : FlyCircle
	{
		int remainingTicks;

		public FlyCircleTimed(int ticks, Actor self) : base(self)
		{
			remainingTicks = ticks;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || remainingTicks-- == 0)
				return NextActivity;

			base.Tick(self);

			return this;
		}
	}
}
