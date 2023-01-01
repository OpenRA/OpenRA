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

using System.Collections.Generic;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	public class TerrainTunnelLayerInfo : TraitInfo, ILobbyCustomRulesIgnore, ICustomMovementLayerInfo
	{
		[Desc("Terrain type used by cells outside any tunnel footprint.")]
		public readonly string ImpassableTerrainType = "Impassable";

		public override object Create(ActorInitializer init) { return new TerrainTunnelLayer(init.Self, this); }
	}

	public class TerrainTunnelLayer : ICustomMovementLayer, IWorldLoaded
	{
		readonly Map map;
		readonly CellLayer<WPos> cellCenters;
		readonly CellLayer<byte> terrainIndices;
		readonly HashSet<CPos> portals = new HashSet<CPos>();
		bool enabled;

		public TerrainTunnelLayer(Actor self, TerrainTunnelLayerInfo info)
		{
			map = self.World.Map;
			cellCenters = new CellLayer<WPos>(map);
			terrainIndices = new CellLayer<byte>(map);
			terrainIndices.Clear(map.Rules.TerrainInfo.GetTerrainIndex(info.ImpassableTerrainType));
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			var cellHeight = world.Map.CellHeightStep.Length;
			foreach (var tti in world.WorldActor.Info.TraitInfos<TerrainTunnelInfo>())
			{
				enabled = true;

				var terrain = map.Rules.TerrainInfo.GetTerrainIndex(tti.TerrainType);
				foreach (var c in tti.TunnelCells())
				{
					var uv = c.ToMPos(map);
					terrainIndices[uv] = terrain;

					var pos = map.CenterOfCell(c);
					cellCenters[uv] = pos - new WVec(0, 0, pos.Z - cellHeight * tti.Height);
				}

				var portal = tti.PortalCells();
				foreach (var c in portal)
				{
					// Need to explicitly set both default and tunnel layers, otherwise the .Contains check will fail
					portals.Add(new CPos(c.X, c.Y, 0));
					portals.Add(new CPos(c.X, c.Y, CustomMovementLayerType.Tunnel));
				}
			}
		}

		bool ICustomMovementLayer.EnabledForLocomotor(LocomotorInfo li) { return enabled; }
		byte ICustomMovementLayer.Index => CustomMovementLayerType.Tunnel;
		bool ICustomMovementLayer.InteractsWithDefaultLayer => false;
		bool ICustomMovementLayer.ReturnToGroundLayerOnIdle => true;

		WPos ICustomMovementLayer.CenterOfCell(CPos cell)
		{
			return cellCenters[cell];
		}

		short ICustomMovementLayer.EntryMovementCost(LocomotorInfo li, CPos cell)
		{
			return portals.Contains(cell) ? (short)0 : PathGraph.MovementCostForUnreachableCell;
		}

		short ICustomMovementLayer.ExitMovementCost(LocomotorInfo li, CPos cell)
		{
			return portals.Contains(cell) ? (short)0 : PathGraph.MovementCostForUnreachableCell;
		}

		byte ICustomMovementLayer.GetTerrainIndex(CPos cell)
		{
			return terrainIndices[cell];
		}
	}
}
