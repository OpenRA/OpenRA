#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Traits
{
	public class RadarColorFromTerrainInfo : ITraitInfo
	{
		public readonly string Terrain;
		public object Create( ActorInitializer init ) { return new RadarColorFromTerrain(init.self,Terrain); }
	}

	public class RadarColorFromTerrain : IRadarSignatureModifier
	{
		Color c;
		public RadarColorFromTerrain(Actor self, string terrain)
		{
			c = self.World.TileSet.Terrain[terrain].Color;
		}
		
		public Color RadarColorOverride(Actor self)
		{
			return c;
		}
	}
}