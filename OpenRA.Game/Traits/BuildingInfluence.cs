#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Traits
{
	public class BuildingInfluenceInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new BuildingInfluence(init.world); }
	}

	public class BuildingInfluence
	{
		CellLayer<Actor> influence;
		Map map;

		public BuildingInfluence(World world)
		{
			map = world.Map;

			influence = new CellLayer<Actor>(map);

			world.ActorAdded +=	a =>
			{
				var b = a.TraitOrDefault<Building>();
				if (b == null)
					return;

				foreach (var u in FootprintUtils.Tiles(map.Rules, a.Info.Name, b.Info, a.Location))
					if (map.Contains(u) && influence[u] == null)
						influence[u] = a;
			};

			world.ActorRemoved += a =>
			{
				var b = a.TraitOrDefault<Building>();
				if (b == null)
					return;

				foreach (var u in FootprintUtils.Tiles(map.Rules, a.Info.Name, b.Info, a.Location))
					if (map.Contains(u) && influence[u] == a)
						influence[u] = null;
			};
		}

		public Actor GetBuildingAt(CPos cell)
		{
			if (!map.Contains(cell))
				return null;

			return influence[cell];
		}
	}
}
