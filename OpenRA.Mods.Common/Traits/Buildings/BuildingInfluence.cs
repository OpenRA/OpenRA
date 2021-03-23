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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("A dictionary of buildings placed on the map. Attach this to the world actor.")]
	public class BuildingInfluenceInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new BuildingInfluence(init.World); }
	}

	public class BuildingInfluence
	{
		readonly Map map;
		readonly CellLayer<Actor> influence;

		public BuildingInfluence(World world)
		{
			map = world.Map;

			influence = new CellLayer<Actor>(map);
		}

		internal void AddInfluence(Actor a, IEnumerable<CPos> tiles)
		{
			foreach (var u in tiles)
				if (influence.Contains(u) && influence[u] == null)
					influence[u] = a;
		}

		internal void RemoveInfluence(Actor a, IEnumerable<CPos> tiles)
		{
			foreach (var u in tiles)
				if (influence.Contains(u) && influence[u] == a)
					influence[u] = null;
		}

		public Actor GetBuildingAt(CPos cell)
		{
			return influence.Contains(cell) ? influence[cell] : null;
		}
	}
}
