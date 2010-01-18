using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Traits;
using System.Drawing;
using OpenRa.GameRules;

namespace OpenRa
{
	static class WorldUtils
	{
		public static bool IsCellBuildable(this World world, int2 a, UnitMovementType umt)
		{
			return world.IsCellBuildable(a, umt, null);
		}

		public static bool IsCellBuildable(this World world, int2 a, UnitMovementType umt, Actor toIgnore)
		{
			if (world.BuildingInfluence.GetBuildingAt(a) != null) return false;
			if (world.UnitInfluence.GetUnitsAt(a).Any(b => b != toIgnore)) return false;

			return world.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(umt,
					world.TileSet.GetWalkability(world.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		public static bool IsActorCrushableByActor(this World world, Actor a, Actor b)
		{
			return world.IsActorCrushableByMovementType(a, b.traits.GetOrDefault<IMovement>().GetMovementType());
		}
		
		public static bool IsActorPathableToCrush(this World world, Actor a, UnitMovementType umt)
		{
			return a != null &&
					a.traits.WithInterface<ICrushable>()
					.Any(c => c.IsPathableCrush(umt, a.Owner));
		}
		
		public static bool IsActorCrushableByMovementType(this World world, Actor a, UnitMovementType umt)
		{
			return a != null &&
				a.traits.WithInterface<ICrushable>()
				.Any(c => c.IsCrushableBy(umt, a.Owner));
		}
		
		public static bool IsWater(this World world, int2 a)
		{
			return world.Map.IsInMap(a.X, a.Y) &&
				TerrainCosts.Cost(UnitMovementType.Float,
					world.TileSet.GetWalkability(world.Map.MapTiles[a.X, a.Y])) < double.PositiveInfinity;
		}

		public static IEnumerable<Actor> FindUnits(this World world, float2 a, float2 b)
		{
			var min = float2.Min(a, b);
			var max = float2.Max(a, b);

			var rect = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);

			return world.Actors
				.Where(x => x.GetBounds(true).IntersectsWith(rect));
		}

		public static IEnumerable<Actor> FindUnitsInCircle(this World world, float2 a, float r)
		{
			var min = a - new float2(r, r);
			var max = a + new float2(r, r);

			var rect = new RectangleF(min.X, min.Y, max.X - min.X, max.Y - min.Y);

			var inBox = world.Actors.Where(x => x.GetBounds(false).IntersectsWith(rect));

			return inBox.Where(x => (x.CenterLocation - a).LengthSquared < r * r);
		}

		public static IEnumerable<int2> FindTilesInCircle(this World world, int2 a, int r)
		{
			var min = a - new int2(r, r);
			var max = a + new int2(r, r);
			if (min.X < 0) min.X = 0;
			if (min.Y < 0) min.Y = 0;
			if (max.X > 127) max.X = 127;
			if (max.Y > 127) max.Y = 127;

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (r * r >= (new int2(i, j) - a).LengthSquared)
						yield return new int2(i, j);
		}

		public static IEnumerable<Actor> SelectActorsInBox(this World world, float2 a, float2 b)
		{
			return world.FindUnits(a, b)
				.Where( x => x.traits.Contains<Selectable>() )
				.GroupBy(x => (x.Owner == Game.LocalPlayer) ? x.Info.Traits.Get<SelectableInfo>().Priority : 0)
				.OrderByDescending(g => g.Key)
				.Select( g => g.AsEnumerable() )
				.DefaultIfEmpty( new Actor[] {} )
				.FirstOrDefault();
		}

		public static bool CanPlaceBuilding(this World world, string name, BuildingInfo building, int2 topLeft, Actor toIgnore)
		{
			return !Footprint.Tiles(name, building, topLeft).Any(
				t => !world.Map.IsInMap(t.X, t.Y) || world.Map.ContainsResource(t) || !world.IsCellBuildable(t,
					building.WaterBound ? UnitMovementType.Float : UnitMovementType.Wheel,
					toIgnore));
		}

		public static bool IsCloseEnoughToBase(this World world, Player p, string buildingName, BuildingInfo bi, int2 topLeft)
		{
			var maxDistance = bi.Adjacent + 1;
			var position = topLeft + Footprint.AdjustForBuildingSize( bi );

			var search = new PathSearch()
			{
				heuristic = loc =>
				{
					var b = world.BuildingInfluence.GetBuildingAt(loc);
					if (b != null && b.Owner == p && b.Info.Traits.Get<BuildingInfo>().BaseNormal) return 0;
					if ((loc - position).Length > maxDistance)
						return float.PositiveInfinity;	/* not quite right */
					return 1;
				},
				checkForBlocked = false,
				ignoreTerrain = true,
			};

			foreach (var t in Footprint.Tiles(buildingName, bi, topLeft)) search.AddInitialCell(t);

			return world.PathFinder.FindPath(search).Count != 0;
		}
	}
}
