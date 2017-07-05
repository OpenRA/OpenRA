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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	public static class FootprintUtils
	{
		public static IEnumerable<CPos> Tiles(Ruleset rules, string name, BuildingInfo buildingInfo, CPos topLeft, bool includePassable = false)
		{
			var dim = buildingInfo.Dimensions;
			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x));

			return TilesWhere(name, dim, footprint.ToArray(), a => includePassable || a != '_').Select(t => t + topLeft);
		}

		public static IEnumerable<CPos> Tiles(Actor a)
		{
			return Tiles(a.World.Map.Rules, a.Info.Name, a.Info.TraitInfo<BuildingInfo>(), a.Location);
		}

		public static IEnumerable<CPos> FrozenUnderFogTiles(Actor a)
		{
			return Tiles(a.World.Map.Rules, a.Info.Name, a.Info.TraitInfo<BuildingInfo>(), a.Location, true);
		}

		public static IEnumerable<CPos> UnpathableTiles(string name, BuildingInfo buildingInfo, CPos position)
		{
			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x)).ToArray();
			foreach (var tile in TilesWhere(name, buildingInfo.Dimensions, footprint, a => a == 'x'))
				yield return tile + position;
		}

		public static IEnumerable<CPos> PathableTiles(string name, BuildingInfo buildingInfo, CPos position)
		{
			var footprint = buildingInfo.Footprint.Where(x => !char.IsWhiteSpace(x)).ToArray();
			foreach (var tile in TilesWhere(name, buildingInfo.Dimensions, footprint, a => a == '_'))
				yield return tile + position;
		}

		static IEnumerable<CVec> TilesWhere(string name, CVec dim, char[] footprint, Func<char, bool> cond)
		{
			if (footprint.Length != dim.X * dim.Y)
				throw new InvalidOperationException("Invalid footprint for " + name);
			var index = 0;

			for (var y = 0; y < dim.Y; y++)
				for (var x = 0; x < dim.X; x++)
					if (cond(footprint[index++]))
						yield return new CVec(x, y);
		}

		public static CVec AdjustForBuildingSize(BuildingInfo buildingInfo)
		{
			var dim = buildingInfo.Dimensions;
			return new CVec(dim.X / 2, dim.Y > 1 ? (dim.Y + 1) / 2 : 0);
		}

		public static WVec CenterOffset(World w, BuildingInfo buildingInfo)
		{
			var dim = buildingInfo.Dimensions;
			var localOffset = buildingInfo.LocalCenterOffset;
			var off = (w.Map.CenterOfCell(new CPos(dim.X, dim.Y)) - w.Map.CenterOfCell(new CPos(1, 1))) / 2;
			return (off - new WVec(0, 0, off.Z)) + localOffset;
		}
	}
}
