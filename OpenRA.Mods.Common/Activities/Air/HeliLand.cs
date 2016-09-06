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
	public class HeliLand : Activity
	{
		readonly Aircraft helicopter;
		bool requireSpace;
		bool playedSound;

		public HeliLand(Actor self, bool requireSpace)
		{
			this.requireSpace = requireSpace;
			helicopter = self.Trait<Aircraft>();
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;

			if (requireSpace && !helicopter.CanLand(self.Location))
				return this;

			if (!playedSound && helicopter.Info.LandingSound != null && !self.IsAtGroundLevel())
			{
				Game.Sound.Play(helicopter.Info.LandingSound);
				playedSound = true;
			}

			if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.LandAltitude))
				return this;

			return NextActivity;
		}
	}
}
