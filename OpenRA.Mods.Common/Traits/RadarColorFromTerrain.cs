#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RadarColorFromTerrainInfo : ITraitInfo
	{
		public readonly string Terrain;
		public object Create(ActorInitializer init) { return new RadarColorFromTerrain(init.Self, Terrain); }
	}

	public class RadarColorFromTerrain : IRadarColorModifier
	{
		Color c;

		public RadarColorFromTerrain(Actor self, string terrain)
		{
			c = self.World.TileSet[self.World.TileSet.GetTerrainIndex(terrain)].Color;
		}

		public bool VisibleOnRadar(Actor self) { return true; }
		public Color RadarColorOverride(Actor self, Color color) { return c; }
	}
}