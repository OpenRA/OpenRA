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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class ElevatedBridgeLayerInfo : ITraitInfo, Requires<DomainIndexInfo>, ILobbyCustomRulesIgnore
	{
		[Desc("Terrain type used by cells outside any elevated bridge footprint.")]
		public readonly string ImpassableTerrainType = "Impassable";

		public object Create(ActorInitializer init) { return new ElevatedBridgeLayer(init.Self, this); }
	}

	// For now this is mostly copies TerrainTunnelLayer. This will change once bridge destruction is implemented
	public class ElevatedBridgeLayer : ICustomMovementLayer, IWorldLoaded
	{
		readonly Map map;
		readonly CellLayer<WPos> cellCenters;
		readonly CellLayer<byte> terrainIndices;
		readonly HashSet<CPos> ends = new HashSet<CPos>();
		bool enabled;

		public ElevatedBridgeLayer(Actor self, ElevatedBridgeLayerInfo info)
		{
			map = self.World.Map;
			cellCenters = new CellLayer<WPos>(map);
			terrainIndices = new CellLayer<byte>(map);
			terrainIndices.Clear(map.Rules.TileSet.GetTerrainIndex(info.ImpassableTerrainType));
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			var domainIndex = world.WorldActor.Trait<DomainIndex>();
			foreach (var tti in world.WorldActor.Info.TraitInfos<ElevatedBridgePlaceholderInfo>())
			{
				enabled = true;

				var terrain = map.Rules.TileSet.GetTerrainIndex(tti.TerrainType);
				foreach (var c in tti.BridgeCells())
				{
					var uv = c.ToMPos(map);
					terrainIndices[uv] = terrain;

					var pos = map.CenterOfCell(c);
					cellCenters[uv] = pos - new WVec(0, 0, pos.Z - 512 * tti.Height);
				}

				var end = tti.EndCells();
				domainIndex.AddFixedConnection(end);
				foreach (var c in end)
				{
					// Need to explicitly set both default and tunnel layers, otherwise the .Contains check will fail
					ends.Add(new CPos(c.X, c.Y, 0));
					ends.Add(new CPos(c.X, c.Y, CustomMovementLayerType.ElevatedBridge));
				}
			}
		}

		bool ICustomMovementLayer.EnabledForActor(ActorInfo a, MobileInfo mi) { return enabled; }
		byte ICustomMovementLayer.Index { get { return CustomMovementLayerType.ElevatedBridge; } }
		bool ICustomMovementLayer.InteractsWithDefaultLayer { get { return true; } }

		WPos ICustomMovementLayer.CenterOfCell(CPos cell)
		{
			return cellCenters[cell];
		}

		int ICustomMovementLayer.EntryMovementCost(ActorInfo a, MobileInfo mi, CPos cell)
		{
			return ends.Contains(cell) ? 0 : int.MaxValue;
		}

		int ICustomMovementLayer.ExitMovementCost(ActorInfo a, MobileInfo mi, CPos cell)
		{
			return ends.Contains(cell) ? 0 : int.MaxValue;
		}

		byte ICustomMovementLayer.GetTerrainIndex(CPos cell)
		{
			return terrainIndices[cell];
		}
	}
}
