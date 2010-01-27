using System;
using System.Collections.Generic;
using System.Linq;
using OpenRa.Traits;
using System.Drawing;
using OpenRa.GameRules;
using OpenRa.FileFormats;

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
			var movement = b.traits.GetOrDefault<IMovement>();
			return movement != null && world.IsActorCrushableByMovementType(a, movement.GetMovementType());
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
			if (min.X < world.Map.XOffset) min.X = world.Map.XOffset;
			if (min.Y < world.Map.YOffset) min.Y = world.Map.YOffset;
			if (max.X > world.Map.XOffset + world.Map.Width - 1) max.X = world.Map.XOffset + world.Map.Width - 1;
			if (max.Y > world.Map.YOffset + world.Map.Height - 1) max.Y = world.Map.YOffset + world.Map.Height - 1;

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (r * r >= (new int2(i, j) - a).LengthSquared)
						yield return new int2(i, j);
		}

		public static IEnumerable<Actor> SelectActorsInBox(this World world, float2 a, float2 b)
		{
			return world.FindUnits(a, b)
				.Where( x => x.traits.Contains<Selectable>() )
				.GroupBy(x => (x.Owner == world.LocalPlayer) ? x.Info.Traits.Get<SelectableInfo>().Priority : 0)
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
			var buildingMaxBounds = bi.Dimensions;
			if( bi.Bib )
				buildingMaxBounds.Y += 1;

			var scanStart = world.ClampToWorld( topLeft - new int2( bi.Adjacent, bi.Adjacent ) );
			var scanEnd = world.ClampToWorld( topLeft + buildingMaxBounds + new int2( bi.Adjacent, bi.Adjacent ) );

			var nearnessCandidates = new List<int2>();

			for( int y = scanStart.Y ; y < scanEnd.Y ; y++ )
			{
				for( int x = scanStart.X ; x < scanEnd.X ; x++ )
				{
					var at = world.BuildingInfluence.GetBuildingAt( new int2( x, y ) );
					if( at != null && at.Owner == p )
						nearnessCandidates.Add( new int2( x, y ) );
				}
			}
			var buildingTiles = Footprint.Tiles( buildingName, bi, topLeft ).ToList();
			return nearnessCandidates
				.Any( a => buildingTiles
					.Any( b => Math.Abs( a.X - b.X ) <= bi.Adjacent
							&& Math.Abs( a.Y - b.Y ) <= bi.Adjacent ) );
		}

		static int2 ClampToWorld( this World world, int2 xy )
		{
			var mapStart = world.Map.Offset;
			var mapEnd = world.Map.Offset + world.Map.Size;
			if( xy.X < mapStart.X )
				xy.X = mapStart.X;
			if( xy.X > mapEnd.X )
				xy.X = mapEnd.X;

			if( xy.Y < mapStart.Y )
				xy.Y = mapStart.Y;
			if( xy.Y > mapEnd.Y )
				xy.Y = mapEnd.Y;

			return xy;
		}

		public static void LoadMapActors(this World world, IniFile mapfile)
		{
			var toLoad = 
				mapfile.GetSection("STRUCTURES", true)
				.Concat(mapfile.GetSection("UNITS", true));

			foreach (var s in toLoad)
			{
				//num=owner,type,health,location,facing,...
				var parts = s.Value.Split( ',' );
				var loc = int.Parse(parts[3]);
				world.CreateActor(parts[1].ToLowerInvariant(), new int2(loc % 128, loc / 128),
					world.players.Values.FirstOrDefault(p => p.InternalName == parts[0]) ?? world.players[0]);
			}
		}
	}
}
