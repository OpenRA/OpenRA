#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Buildings
{
	public static class BuildingUtils
	{
		public static bool IsCellBuildable(this World world, int2 a, bool waterBound)
		{
			return world.IsCellBuildable(a, waterBound, null);	
		}
		
		public static bool IsCellBuildable(this World world, int2 a, bool waterBound, Actor toIgnore)
		{
			if (world.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(a) != null) return false;
			if (world.WorldActor.Trait<UnitInfluence>().GetUnitsAt(a).Any(b => b != toIgnore)) return false;
			
			if (waterBound)
				return world.Map.IsInMap(a.X,a.Y) && world.GetTerrainInfo(a).IsWater;

			return world.Map.IsInMap(a.X, a.Y) && world.GetTerrainInfo(a).Buildable;
		}

		public static bool CanPlaceBuilding(this World world, string name, BuildingInfo building, int2 topLeft, Actor toIgnore)
		{
			var res = world.WorldActor.Trait<ResourceLayer>();
			return FootprintUtils.Tiles(name, building, topLeft).All(
				t => world.Map.IsInMap(t.X, t.Y) && res.GetResource(t) == null &&
					world.IsCellBuildable(t, building.WaterBound, toIgnore));
		}

		public static IEnumerable<int2> GetLineBuildCells(World world, int2 location, string name, BuildingInfo bi)
		{
			int range = Rules.Info[name].Traits.Get<LineBuildInfo>().Range;
			var topLeft = location;	// 1x1 assumption!

			if (world.IsCellBuildable(topLeft, bi.WaterBound))
				yield return topLeft;

			// Start at place location, search outwards
			// TODO: First make it work, then make it nice
			var vecs = new[] { new int2(1, 0), new int2(0, 1), new int2(-1, 0), new int2(0, -1) };
			int[] dirs = { 0, 0, 0, 0 };
			for (int d = 0; d < 4; d++)
			{
				for (int i = 1; i < range; i++)
				{
					if (dirs[d] != 0)
						continue;

					int2 cell = topLeft + i * vecs[d];
					if (world.IsCellBuildable(cell, bi.WaterBound))
						continue; // Cell is empty; continue search

					// Cell contains an actor. Is it the type we want?
					if (world.Queries.WithTrait<LineBuild>().Any(a => (a.Actor.Info.Name == name && a.Actor.Location.X == cell.X && a.Actor.Location.Y == cell.Y)))
						dirs[d] = i; // Cell contains actor of correct type
					else
						dirs[d] = -1; // Cell is blocked by another actor type
				}

				// Place intermediate-line sections
				if (dirs[d] > 0)
					for (int i = 1; i < dirs[d]; i++)
						yield return topLeft + i * vecs[d];
			}
		}
	}
}
