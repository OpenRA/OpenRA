#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	[TraitLocation(SystemActors.World)]
	[Desc("Attach this to the world actor. Required for " + nameof(LaysPavement) + " to work.")]
	public class PavementLayerInfo : TraitInfo
	{
		[Desc("The terrain type to place.")]
		[FieldLoader.Require]
		public readonly string TerrainType = null;

		public override object Create(ActorInitializer init) { return new PavementLayer(init.Self, this); }
	}

	public class PavementLayer
	{
		public readonly CellLayer<bool> Occupied;

		readonly PavementLayerInfo info;
		readonly Map map;

		public PavementLayer(Actor self, PavementLayerInfo info)
		{
			this.info = info;
			map = self.World.Map;
			Occupied = new CellLayer<bool>(map);
		}

		public void Add(CPos cell)
		{
			Occupied[cell] = true;
			map.CustomTerrain[cell] = map.Rules.TerrainInfo.GetTerrainIndex(info.TerrainType);
		}
	}
}
