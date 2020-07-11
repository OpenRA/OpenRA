#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	public static class BuildingUtils
	{
		public static bool IsCellBuildable(this World world, CPos cell, ActorInfo ai, BuildingInfo bi, Actor toIgnore = null)
		{
			if (!world.Map.Contains(cell))
				return false;

			var building = world.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(cell);
			if (building != null)
			{
				if (ai == null)
					return false;

				var replacementInfo = ai.TraitInfoOrDefault<ReplacementInfo>();
				if (replacementInfo == null)
					return false;

				if (!building.TraitsImplementing<Replaceable>().Any(p => !p.IsTraitDisabled &&
					p.Info.Types.Overlaps(replacementInfo.ReplaceableTypes)))
					return false;
			}
			else if (!bi.AllowInvalidPlacement && world.ActorMap.GetActorsAt(cell).Any(a => a != toIgnore))
				return false;

			// Buildings can never be placed on ramps
			return world.Map.Ramp[cell] == 0 && bi.TerrainTypes.Contains(world.Map.GetTerrainInfo(cell).Type);
		}

		public static bool CanPlaceBuilding(this World world, CPos cell, ActorInfo ai, BuildingInfo bi, Actor toIgnore)
		{
			if (bi.AllowInvalidPlacement)
				return true;

			var res = world.WorldActor.TraitOrDefault<ResourceLayer>();
			return bi.Tiles(cell).All(t => world.Map.Contains(t) &&
				(bi.AllowPlacementOnResources || res == null || res.GetResourceType(t) == null) &&
					world.IsCellBuildable(t, ai, bi, toIgnore));
		}

		public static IEnumerable<Pair<CPos, Actor>> GetLineBuildCells(World world, CPos cell, ActorInfo ai, BuildingInfo bi, Player owner)
		{
			var lbi = ai.TraitInfo<LineBuildInfo>();
			var topLeft = cell;	// 1x1 assumption!

			if (world.IsCellBuildable(topLeft, ai, bi))
				yield return Pair.New<CPos, Actor>(topLeft, null);

			// Start at place location, search outwards
			// TODO: First make it work, then make it nice
			var vecs = new[] { new CVec(1, 0), new CVec(0, 1), new CVec(-1, 0), new CVec(0, -1) };
			int[] dirs = { 0, 0, 0, 0 };
			Actor[] connectors = { null, null, null, null };

			for (var d = 0; d < 4; d++)
			{
				for (var i = 1; i < lbi.Range; i++)
				{
					if (dirs[d] != 0)
						continue;

					// Continue the search if the cell is empty or not visible
					var c = topLeft + i * vecs[d];
					if (world.IsCellBuildable(c, ai, bi) || !owner.Shroud.IsExplored(c))
						continue;

					// Cell contains an actor. Is it the type we want?
					connectors[d] = world.ActorMap.GetActorsAt(c)
						.FirstOrDefault(a => a.Info.TraitInfos<LineBuildNodeInfo>()
							.Any(info => info.Types.Overlaps(lbi.NodeTypes) && info.Connections.Contains(vecs[d])));

					dirs[d] = connectors[d] != null ? i : -1;
				}

				// Place intermediate-line sections
				if (dirs[d] > 0)
					for (var i = 1; i < dirs[d]; i++)
						yield return Pair.New(topLeft + i * vecs[d], connectors[d]);
			}
		}
	}
}
