using System;
using OpenRa.FileFormats;
using OpenRa.Traits;

namespace OpenRa
{
	public static class Ore
	{
		public static void AddOre(this Map map, int i, int j)
		{
			if (map.ContainsOre(i, j) && map.MapTiles[i, j].density < 12)
				map.MapTiles[i, j].density++;
			else if (map.MapTiles[i, j].overlay == 0xff)
			{
				map.MapTiles[i, j].overlay = ChooseOre();
				map.MapTiles[i, j].density = 1;
			}
		}

		public static void DestroyOre(this Map map, int i, int j)
		{
			if (map.ContainsResource(new int2(i, j)))
			{
				map.MapTiles[i, j].density = 0;
				map.MapTiles[i, j].overlay = 0xff;
			}
		}

		public static bool OreCanSpreadInto(this World world, int i, int j)
		{
			if (world.WorldActor.traits.Get<BuildingInfluence>().GetBuildingAt(new int2(i, j)) != null)
				return false;

			return TerrainCosts.Cost(UnitMovementType.Wheel,
				world.TileSet.GetWalkability(world.Map.MapTiles[i, j]))
				< double.PositiveInfinity;
		}

		public static void SpreadOre(this World world, Random r, float chance)
		{
			var map = world.Map;

			var mini = map.XOffset; var maxi = map.XOffset + map.Width;
			var minj = map.YOffset; var maxj = map.YOffset + map.Height;

			/* phase 1: grow into neighboring regions */
			var newOverlay = new byte[128, 128];
			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
				{
					newOverlay[i, j] = 0xff;
					if (!map.HasOverlay(i, j)
						&& r.NextDouble() < chance
						&& map.GetOreDensity(i, j) > 0
						&& world.OreCanSpreadInto(i, j))
						newOverlay[i, j] = ChooseOre();
				}

			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
					if (newOverlay[i, j] != 0xff)
						map.MapTiles[i, j].overlay = newOverlay[i, j];
		}

		public static void GrowOre(this World world, Random r)
		{
			var map = world.Map;

			var mini = map.XOffset; var maxi = map.XOffset + map.Width;
			var minj = map.YOffset; var maxj = map.YOffset + map.Height;

			/* phase 2: increase density of existing areas */
			var newDensity = new byte[128, 128];
			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
					if (map.ContainsOre(i, j)) newDensity[i, j] = map.GetOreDensity(i, j);

			for (int j = minj; j < maxj; j++)
				for (int i = mini; i < maxi; i++)
					if (map.MapTiles[i, j].density < newDensity[i, j])
						++map.MapTiles[i, j].density;
		}

		public static void InitOreDensity( this Map map )
		{
			for (int j = 0; j < 128; j++)
				for (int i = 0; i < 128; i++)
				{
					if (map.ContainsOre(i, j)) map.MapTiles[i, j].density = map.GetOreDensity(i, j);
					if (map.ContainsGem(i, j)) map.MapTiles[i, j].density = map.GetGemDensity(i, j);
				}
		}

		static byte GetOreDensity(this Map map, int i, int j)
		{
			int sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					if (map.ContainsOre(i + u, j + v))
						++sum;
			sum = (sum * 4 + 2) / 3;
			return (byte)sum;
		}

		static byte GetGemDensity(this Map map, int i, int j)
		{
			int sum = 0;
			for (var u = -1; u < 2; u++)
				for (var v = -1; v < 2; v++)
					if (map.ContainsGem(i + u, j + v))
						++sum;
			sum = (sum+2) / 3;		/* 3 gem units/tile is full. */
			return (byte)sum;
		}

		static bool HasOverlay(this Map map, int i, int j)
		{
			return map.MapTiles[i, j].overlay < overlayIsOre.Length;
		}

		static bool ContainsOre(this Map map, int i, int j)
		{
			return map.HasOverlay(i, j) && overlayIsOre[map.MapTiles[i, j].overlay];
		}

		static bool ContainsGem(this Map map, int i, int j)
		{
			return map.HasOverlay(i, j) && overlayIsGems[map.MapTiles[i, j].overlay];
		}

		public static bool ContainsResource(this Map map, int2 p)
		{
			return map.ContainsGem(p.X, p.Y) || map.ContainsOre(p.X, p.Y);
		}

		public static bool Harvest(this Map map, int2 p, out bool isGems)		/* harvests one unit if possible */
		{
			isGems = map.ContainsGem(p.X, p.Y);
			if (map.MapTiles[p.X, p.Y].density == 0) return false;

			if (--map.MapTiles[p.X, p.Y].density == 0)
				map.MapTiles[p.X, p.Y].overlay = 0xff;

			return true;
		}

		static byte ore = 5;
		static byte ChooseOre()
		{
			if (++ore > 8) ore = 5;
			return ore;
		}

		public static bool IsOverlaySolid(this Map map, int2 p)
		{
			var o = map.MapTiles[p.X, p.Y].overlay;
			return o < overlayIsFence.Length && overlayIsFence[o];
		}

		public static bool[] overlayIsFence =
			{
				true, true, true, true, true,
				false, false, false, false,
				false, false, false, false,
				false, false, false, false, false, false, false,
				false, false, false, true, true,
			};

		public static bool[] overlayIsOre =
			{
				false, false, false, false, false,
				true, true, true, true,
				false, false, false, false,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
			};

		public static bool[] overlayIsGems =
			{
				false, false, false, false, false,
				false, false, false, false,
				true, true, true, true,
				false, false, false, false, false, false, false,
				false, false, false, false, false,
			};
	}
}
