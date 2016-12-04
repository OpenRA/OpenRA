#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.AS.Activities
{
	public class HeliFlyCircleTimed : HeliFlyCircle
	{
		int remainingTicks;

		public HeliFlyCircleTimed(Actor self, int ticks)
			: base(self)
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
