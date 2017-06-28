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
		public static IEnumerable<CPos> Tiles(Actor a)
		{
			var info = a.Info.TraitInfo<BuildingInfo>();
			return info.Tiles(a.Location);
		}

		public static IEnumerable<CPos> FrozenUnderFogTiles(Actor a)
		{
			var info = a.Info.TraitInfo<BuildingInfo>();
			return info.FrozenUnderFogTiles(a);
		}

		public static IEnumerable<CPos> UnpathableTiles(string name, BuildingInfo buildingInfo, CPos position)
		{
			return buildingInfo.UnpathableTiles(position);
		}

		public static IEnumerable<CPos> PathableTiles(string name, BuildingInfo buildingInfo, CPos position)
		{
			return buildingInfo.PathableTiles(position);
		}

		public static CVec AdjustForBuildingSize(BuildingInfo buildingInfo)
		{
			return buildingInfo.AdjustForBuildingSize();
		}

		public static WVec CenterOffset(World w, BuildingInfo buildingInfo)
		{
			return buildingInfo.CenterOffset(w);
		}
	}
}
