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

using System.Collections.Generic;
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

		public object Create(ActorInitializer init) { return new LowBridge(init.Self, this); }
	}

	class LowBridge : INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly LowBridgeInfo info;
		readonly IEnumerable<CPos> cells;

		public LowBridge(Actor self, LowBridgeInfo info)
		{
			this.info = info;

			var buildingInfo = self.Info.TraitInfo<BuildingInfo>();
			cells = FootprintUtils.PathableTiles(self.Info.Name, buildingInfo, self.Location);
		}

		void UpdateTerrain(Actor self, byte terrainIndex)
		{
			foreach (var cell in cells)
				self.World.Map.CustomTerrain[cell] = terrainIndex;

			var domainIndex = self.World.WorldActor.TraitOrDefault<DomainIndex>();
			if (domainIndex != null)
				domainIndex.UpdateCells(self.World, cells);
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var tileSet = self.World.Map.Rules.TileSet;
			var terrainIndex = tileSet.GetTerrainIndex(info.TerrainType);
			UpdateTerrain(self, terrainIndex);
		}

		void KillUnitsOnBridge(Actor self)
		{
			foreach (var c in cells)
				foreach (var a in self.World.ActorMap.GetActorsAt(c))
					if (a.Info.HasTraitInfo<IPositionableInfo>() && !a.Trait<IPositionable>().CanEnterCell(c))
						a.Kill(self);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			UpdateTerrain(self, byte.MaxValue);
			KillUnitsOnBridge(self);
		}
	}
}
