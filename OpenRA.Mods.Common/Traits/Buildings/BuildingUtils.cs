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
using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	public static class BuildingUtils
	{
		public static bool IsCellBuildable(this World world, CPos cell, BuildingInfo bi, Actor toIgnore = null)
		{
			if (!world.Map.Contains(cell))
				return false;

			if (world.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(cell) != null)
				return false;

			if (!bi.AllowInvalidPlacement && world.ActorMap.GetActorsAt(cell).Any(a => a != toIgnore))
				return false;

			var tile = world.Map.Tiles[cell];
			var tileInfo = world.Map.Rules.TileSet.GetTileInfo(tile);

			// TODO: This is bandaiding over bogus tilesets.
			if (tileInfo != null && tileInfo.RampType > 0)
				return false;

			return bi.TerrainTypes.Contains(world.Map.GetTerrainInfo(cell).Type);
		}

		public static bool CanPlaceBuilding(this World world, string name, BuildingInfo building, CPos topLeft, Actor toIgnore)
		{
			if (building.AllowInvalidPlacement)
				return true;

			var res = world.WorldActor.Trait<ResourceLayer>();
			return FootprintUtils.Tiles(world.Map.Rules, name, building, topLeft).All(
				t => world.Map.Contains(t) && res.GetResource(t) == null &&
					world.IsCellBuildable(t, building, toIgnore));
		}

		public static IEnumerable<CPos> GetLineBuildCells(World world, CPos location, string name, BuildingInfo bi)
		{
			var lbi = world.Map.Rules.Actors[name].TraitInfo<LineBuildInfo>();
			var topLeft = location;	// 1x1 assumption!

			if (world.IsCellBuildable(topLeft, bi))
				yield return topLeft;

			// Start at place location, search outwards
			// TODO: First make it work, then make it nice
			var vecs = new[] { new CVec(1, 0), new CVec(0, 1), new CVec(-1, 0), new CVec(0, -1) };
			int[] dirs = { 0, 0, 0, 0 };
			for (var d = 0; d < 4; d++)
			{
				for (var i = 1; i < lbi.Range; i++)
				{
					if (dirs[d] != 0)
						continue;

					var cell = topLeft + i * vecs[d];
					if (world.IsCellBuildable(cell, bi))
						continue; // Cell is empty; continue search

					// Cell contains an actor. Is it the type we want?
					var hasConnector = world.ActorMap.GetActorsAt(cell)
						.Any(a => a.Info.TraitInfos<LineBuildNodeInfo>()
							.Any(info => info.Types.Overlaps(lbi.NodeTypes) && info.Connections.Contains(vecs[d])));

					dirs[d] = hasConnector ? i : -1;
				}

				// Place intermediate-line sections
				if (dirs[d] > 0)
					for (var i = 1; i < dirs[d]; i++)
						yield return topLeft + i * vecs[d];
			}
		}
	}
}
