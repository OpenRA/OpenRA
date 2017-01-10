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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class JumpjetActorLayerInfo : ITraitInfo
	{
		[Desc("Terrain type of the airborne layer.")]
		public readonly string TerrainType = "Jumpjet";

		[Desc("Height offset relative to the smoothed terrain for movement.")]
		public readonly WDist HeightOffset = new WDist(2304);

		[Desc("Cell radius for smoothing adjacent cell heights.")]
		public readonly int SmoothingRadius = 2;

		public object Create(ActorInitializer init) { return new JumpjetActorLayer(init.Self, this); }
	}

	public class JumpjetActorLayer : ICustomMovementLayer
	{
		readonly Map map;

		readonly byte terrainIndex;
		readonly CellLayer<int> height;

		public JumpjetActorLayer(Actor self, JumpjetActorLayerInfo info)
		{
			map = self.World.Map;
			terrainIndex = self.World.Map.Rules.TileSet.GetTerrainIndex(info.TerrainType);
			height = new CellLayer<int>(map);
			foreach (var c in map.AllCells)
			{
				var neighbourCount = 0;
				var neighbourHeight = 0;
				for (var dy = -info.SmoothingRadius; dy <= info.SmoothingRadius; dy++)
				{
					for (var dx = -info.SmoothingRadius; dx <= info.SmoothingRadius; dx++)
					{
						var neighbour = c + new CVec(dx, dy);
						if (!map.AllCells.Contains(neighbour))
							continue;

						neighbourCount++;
						neighbourHeight += map.Height[neighbour];
					}
				}

				height[c] = info.HeightOffset.Length + neighbourHeight * 512 / neighbourCount;
			}
		}

		bool ICustomMovementLayer.EnabledForActor(ActorInfo a, MobileInfo mi) { return mi.Jumpjet; }
		byte ICustomMovementLayer.Index { get { return CustomMovementLayerType.Jumpjet; } }
		bool ICustomMovementLayer.InteractsWithDefaultLayer { get { return true; } }

		WPos ICustomMovementLayer.CenterOfCell(CPos cell)
		{
			var pos = map.CenterOfCell(cell);
			return pos + new WVec(0, 0, height[cell] - pos.Z);
		}

		bool ValidTransitionCell(CPos cell, MobileInfo mi)
		{
			var terrainType = map.GetTerrainInfo(cell).Type;
			if (!mi.JumpjetTransitionTerrainTypes.Contains(terrainType) && mi.JumpjetTransitionTerrainTypes.Any())
				return false;

			if (mi.JumpjetTransitionOnRamps)
				return true;

			var tile = map.Tiles[cell];
			var ti = map.Rules.TileSet.GetTileInfo(tile);
			return ti == null || ti.RampType == 0;
		}

		int ICustomMovementLayer.EntryMovementCost(ActorInfo a, MobileInfo mi, CPos cell)
		{
			return ValidTransitionCell(cell, mi) ? mi.JumpjetTransitionCost : int.MaxValue;
		}

		int ICustomMovementLayer.ExitMovementCost(ActorInfo a, MobileInfo mi, CPos cell)
		{
			return ValidTransitionCell(cell, mi) ? mi.JumpjetTransitionCost : int.MaxValue;
		}

		byte ICustomMovementLayer.GetTerrainIndex(CPos cell)
		{
			return terrainIndex;
		}
	}
}
