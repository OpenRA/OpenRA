#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliLand : Activity
	{
		readonly Helicopter helicopter;
		readonly WRange landAltitude;
		bool requireSpace;

		public HeliLand(Actor self, bool requireSpace)
		{
			this.requireSpace = requireSpace;
			helicopter = self.Trait<Helicopter>();
			landAltitude = helicopter.Info.LandAltitude;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (requireSpace && !helicopter.CanLand(self.Location))
				return this;

			if (HeliFly.AdjustAltitude(self, helicopter, landAltitude))
				return this;

			return NextActivity;
		}
	}
}
