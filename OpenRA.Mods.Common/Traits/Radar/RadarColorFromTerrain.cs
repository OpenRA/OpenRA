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

using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Radar
{
	public class RadarColorFromTerrainInfo : TraitInfo
	{
		[FieldLoader.Require]
		public readonly string Terrain;

		public Color GetColorFromTerrain(World world)
		{
			var tileSet = world.Map.Rules.TileSet;
			return tileSet[tileSet.GetTerrainIndex(Terrain)].Color;
		}

		public override object Create(ActorInitializer init) { return new RadarColorFromTerrain(init.Self, this); }
	}

	public class RadarColorFromTerrain : IRadarColorModifier
	{
		readonly Color c;

		public RadarColorFromTerrain(Actor self, RadarColorFromTerrainInfo info)
		{
			c = info.GetColorFromTerrain(self.World);
		}

		public bool VisibleOnRadar(Actor self) { return true; }
		public Color RadarColorOverride(Actor self, Color color) { return c; }
	}
}
