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

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("A dictionary of buildings placed on the map. Attach this to the world actor.")]
	public class BuildingInfluenceInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new BuildingInfluence(init.World); }
	}

	public class BuildingInfluence
	{
		readonly Map map;
		readonly CellLayer<Actor> influence;

		public BuildingInfluence(World world)
		{
			map = world.Map;

			influence = new CellLayer<Actor>(map);

			world.ActorAdded += a =>
			{
				var b = a.Info.TraitInfoOrDefault<BuildingInfo>();
				if (b == null)
					return;

				foreach (var u in FootprintUtils.Tiles(map.Rules, a.Info.Name, b, a.Location))
					if (influence.Contains(u) && influence[u] == null)
						influence[u] = a;
			};

			world.ActorRemoved += a =>
			{
				var b = a.Info.TraitInfoOrDefault<BuildingInfo>();
				if (b == null)
					return;

				foreach (var u in FootprintUtils.Tiles(map.Rules, a.Info.Name, b, a.Location))
					if (influence.Contains(u) && influence[u] == a)
						influence[u] = null;
			};
		}

		public Actor GetBuildingAt(CPos cell)
		{
			return influence.Contains(cell) ? influence[cell] : null;
		}
	}
}
