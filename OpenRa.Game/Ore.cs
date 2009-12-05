using System;
using OpenRa.FileFormats;

namespace OpenRa.Game
{
	public static class Ore
	{
		public static void AddOre(this Map map, int i, int j)
		{
			if (Rules.General.OreSpreads)
				if (map.ContainsOre(i, j) && map.MapTiles[i, j].density < 12)
					map.MapTiles[i, j].density++;
				else if (map.MapTiles[i, j].overlay == 0xff)
				{
					map.MapTiles[i, j].overlay = ChooseOre();
					map.MapTiles[i, j].density = 1;
				}
		}

		public static void Destroy(int i, int j)
		{
			if (Rules.Map.ContainsResource(new int2(i, j)))
			{
				Rules.Map.MapTiles[i, j].density = 0;
				Rules.Map.MapTiles[i, j].overlay = 0xff;
			}
		}

		public static bool CanSpreadInto(int i, int j)
		{
			if (Game.BuildingInfluence.GetBuildingAt(new int2(i, j)) != null)
				return false;

			return TerrainCosts.Cost(UnitMovementType.Wheel,
				Rules.TileSet.GetWalkability(Rules.Map.MapTiles[i, j]))
				< double.PositiveInfinity;
		}

		public static void GrowOre(this Map map, Random r)
		{
			var mini = map.XOffset; var maxi = map.XOffset + map.Width;
			var minj = map.YOffset; var maxj = map.YOffset + map.Height;
			var chance = Rules.General.OreChance;

			/* phase 1: grow into neighboring regions */
			if (Rules.General.OreSpreads)
			{
				var newOverlay = new byte[128, 128];
				for (int j = minj; j < maxj; j++)
					for (int i = mini; i < maxi; i++)
					{
						newOverlay[i, j] = 0xff;
						if (!map.HasOverlay(i, j)
							&& r.NextDouble() < chance
							&& map.GetOreDensity(i, j) > 0
							&& CanSpreadInto(i,j))
							newOverlay[i, j] = ChooseOre();
					}

				for (int j = minj; j < maxj; j++)
					for (int i = mini; i < maxi; i++)
						if (newOverlay[i, j] != 0xff)
							map.MapTiles[i, j].overlay = newOverlay[i, j];
			}

			/* phase 2: increase density of existing areas */
			if (Rules.General.OreGrows)
			{
				var newDensity = new byte[128, 128];
				for (int j = minj; j < maxj; j++)
					for (int i = mini; i < maxi; i++)
						if (map.ContainsOre(i, j)) newDensity[i, j] = map.GetOreDensity(i, j);

				for (int j = minj; j < maxj; j++)
					for (int i = mini; i < maxi; i++)
						if (map.MapTiles[i, j].density < newDensity[i, j])
							++map.MapTiles[i, j].density;
			}
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

		public static bool HasOverlay(this Map map, int i, int j)
		{
			return map.MapTiles[i, j].overlay < overlayIsOre.Length;
		}

		public static bool ContainsOre(this Map map, int i, int j)
		{
			return map.HasOverlay(i, j) && overlayIsOre[map.MapTiles[i, j].overlay];
		}

		public static bool ContainsGem(this Map map, int i, int j)
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
