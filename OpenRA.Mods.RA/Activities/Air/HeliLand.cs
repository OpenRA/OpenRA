#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class HeliLand : Activity
	{
		bool requireSpace;

		public HeliLand(bool requireSpace)
		{
			this.requireSpace = requireSpace;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			var helicopter = self.Trait<Helicopter>();

			if (requireSpace && !helicopter.CanLand(self.Location))
				return this;

			if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.LandAltitude))
				return this;

			return NextActivity;
		}
	}
}
