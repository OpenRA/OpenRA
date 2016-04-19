#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.TS.Traits
{
	[Desc("Bridge actor that can't be passed underneath.")]
	class LowBridgeInfo : ITraitInfo, Requires<BuildingInfo>
	{
		public readonly string TerrainType = "Road";

		public readonly bool Dead = false;

		public object Create(ActorInitializer init) { return new LowBridge(init.Self, this); }
	}

	class LowBridge : INotifyCreated
	{
		readonly LowBridgeInfo info;

		public LowBridge(Actor self, LowBridgeInfo info)
		{
			this.info = info;
		}

		public void Created(Actor self)
		{
			if (info.Dead)
				return;

			var buildingInfo = self.Info.TraitInfo<BuildingInfo>();
			var cells = FootprintUtils.PathableTiles(self.Info.Name, buildingInfo, self.Location);
			var tileSet = self.World.Map.Rules.TileSet;
			foreach (var cell in cells)
				self.World.Map.CustomTerrain[cell] = tileSet.GetTerrainIndex(info.TerrainType);

			var domainIndex = self.World.WorldActor.TraitOrDefault<DomainIndex>();
			if (domainIndex != null)
				domainIndex.UpdateCells(self.World, cells);
		}
	}
}
