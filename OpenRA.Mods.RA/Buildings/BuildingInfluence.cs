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

namespace OpenRA.Mods.RA.Buildings
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
				if (b != null)
					ChangeInfluence(a, b, true);
			};

			world.ActorRemoved += a =>
			{
				var b = a.TraitOrDefault<Building>();
				if (b != null)
					ChangeInfluence(a, b, false);
			};
		}

		void ChangeInfluence(Actor a, Building building, bool isAdd)
		{
			foreach (var u in FootprintUtils.Tiles(a.Info.Name, building.Info, a.Location))
				if (map.IsInMap(u))
					influence[u.X, u.Y] = isAdd ? a : null;
		}

		public Actor GetBuildingAt(CPos cell)
		{
			if (!map.IsInMap(cell))
				return null;

			return influence[cell.X, cell.Y];
		}
	}
}
