#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor will be affected by the slope of the terrain")]
	class AffectedBySlopeInfo : ConditionalTraitInfo, Requires<BodyOrientationInfo>
	{
		public override object Create(ActorInitializer init) { return new AffectedBySlope(init.Self, this); }
	}

	class AffectedBySlope : ConditionalTrait<AffectedBySlopeInfo>, ITick, ISync, ITerrainSlope
	{
		public WRot Slope { get; set; }
		CPos lastLocation;
		readonly WAngle rotStep;
		WRot targetSlope;

		public AffectedBySlope(Actor self, AffectedBySlopeInfo info)
			: base(info)
		{
			Slope = WRot.Zero;
			targetSlope = WRot.Zero;
			lastLocation = self.Location;
			rotStep = WAngle.FromDegrees(1);
		}

		void ITick.Tick(Actor self)
		{
			if (self.Location != lastLocation)
			{
				var rampType = self.World.Map.Rules.TileSet.GetTileInfo(self.World.Map.Tiles[self.Location]).RampType;
				targetSlope = self.World.Map.Grid.GetGridSlope(rampType);
				lastLocation = self.Location;
			}

			if (Slope != targetSlope)
			{
				Slope = new WRot(WAngle.ChangeByStep(Slope.Roll, targetSlope.Roll, rotStep),
				WAngle.ChangeByStep(Slope.Pitch, targetSlope.Pitch, rotStep),
				WAngle.ChangeByStep(Slope.Yaw, targetSlope.Yaw, rotStep));
			}
		}
	}
}
