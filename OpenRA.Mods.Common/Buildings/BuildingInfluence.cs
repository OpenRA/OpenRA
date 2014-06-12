#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Buildings
{
	public class BuildingInfluenceInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new BuildingInfluence(init.world); }
	}

	public class BuildingInfluence
	{
		Actor[,] influence;
		Map map;

		public BuildingInfluence(World world)
		{
			map = world.Map;

			influence = new Actor[map.MapSize.X, map.MapSize.Y];

			world.ActorAdded +=	a =>
			{
				var b = a.TraitOrDefault<Building>();
				if (b == null)
					return;

				foreach (var u in FootprintUtils.Tiles(map.Rules, a.Info.Name, b.Info, a.Location))
					if (map.IsInMap(u) && influence[u.X, u.Y] == null)
						influence[u.X, u.Y] = a;
			};

			world.ActorRemoved += a =>
			{
				var b = a.TraitOrDefault<Building>();
				if (b == null)
					return;

				foreach (var u in FootprintUtils.Tiles(map.Rules, a.Info.Name, b.Info, a.Location))
					if (map.IsInMap(u) && influence[u.X, u.Y] == a)
						influence[u.X, u.Y] = null;
			};
		}

		public Actor GetBuildingAt(CPos cell)
		{
			if (!map.IsInMap(cell))
				return null;

			return influence[cell.X, cell.Y];
		}
	}
}
