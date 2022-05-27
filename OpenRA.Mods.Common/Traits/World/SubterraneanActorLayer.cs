#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Pathfinder;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	public class SubterraneanActorLayerInfo : TraitInfo, ICustomMovementLayerInfo
	{
		[Desc("Terrain type of the underground layer.")]
		public readonly string TerrainType = "Subterranean";

		[Desc("Height offset relative to the smoothed terrain for movement.")]
		public readonly WDist HeightOffset = -new WDist(2048);

		[Desc("Cell radius for smoothing adjacent cell heights.")]
		public readonly int SmoothingRadius = 2;

		public override object Create(ActorInitializer init) { return new SubterraneanActorLayer(init.Self, this); }
	}

	public class SubterraneanActorLayer : ICustomMovementLayer
	{
		readonly Map map;

		readonly byte terrainIndex;
		readonly CellLayer<int> height;

		public SubterraneanActorLayer(Actor self, SubterraneanActorLayerInfo info)
		{
			map = self.World.Map;
			terrainIndex = self.World.Map.Rules.TerrainInfo.GetTerrainIndex(info.TerrainType);
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

		bool ICustomMovementLayer.EnabledForLocomotor(LocomotorInfo li) { return li is SubterraneanLocomotorInfo; }
		byte ICustomMovementLayer.Index => CustomMovementLayerType.Subterranean;
		bool ICustomMovementLayer.InteractsWithDefaultLayer => false;
		bool ICustomMovementLayer.ReturnToGroundLayerOnIdle => true;

		WPos ICustomMovementLayer.CenterOfCell(CPos cell)
		{
			var pos = map.CenterOfCell(cell);
			return pos + new WVec(0, 0, height[cell] - pos.Z);
		}

		bool ValidTransitionCell(CPos cell, SubterraneanLocomotorInfo sli)
		{
			var terrainType = map.GetTerrainInfo(cell).Type;
			if (!sli.SubterraneanTransitionTerrainTypes.Contains(terrainType) && sli.SubterraneanTransitionTerrainTypes.Count > 0)
				return false;

			if (sli.SubterraneanTransitionOnRamps)
				return true;

			return map.Ramp[cell] == 0;
		}

		short ICustomMovementLayer.EntryMovementCost(LocomotorInfo li, CPos cell)
		{
			var sli = (SubterraneanLocomotorInfo)li;
			return ValidTransitionCell(cell, sli) ? sli.SubterraneanTransitionCost : PathGraph.MovementCostForUnreachableCell;
		}

		short ICustomMovementLayer.ExitMovementCost(LocomotorInfo li, CPos cell)
		{
			var sli = (SubterraneanLocomotorInfo)li;
			return ValidTransitionCell(cell, sli) ? sli.SubterraneanTransitionCost : PathGraph.MovementCostForUnreachableCell;
		}

		byte ICustomMovementLayer.GetTerrainIndex(CPos cell)
		{
			return terrainIndex;
		}
	}
}
