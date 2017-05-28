#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class HeliLand : Activity
	{
		readonly Aircraft helicopter;
		readonly WDist landAltitude;
		readonly bool requireSpace;

		bool playedSound;

		public HeliLand(Actor self, bool requireSpace)
			: this(self, requireSpace, self.Info.TraitInfo<AircraftInfo>().LandAltitude) { }

		public HeliLand(Actor self, bool requireSpace, WDist landAltitude)
		{
			this.requireSpace = requireSpace;
			this.landAltitude = landAltitude;
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
				Game.Sound.Play(SoundType.World, helicopter.Info.LandingSound);
				playedSound = true;
			}

			if (HeliFly.AdjustAltitude(self, helicopter, landAltitude))
				return this;

			return NextActivity;
		}

		public override TargetLineNode TargetLineNode(Actor self)
		{
			var color = NextActivity == null ? Color.Green : NextActivity.TargetLineNode(self).Color;
			return new TargetLineNode(Target.Invalid, color, NextActivity);
		}
	}
}
