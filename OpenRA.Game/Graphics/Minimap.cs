#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	public static class Minimap
	{
		public static Bitmap TerrainBitmap(TileSet tileset, Map map, bool actualSize = false)
		{
			var width = map.Bounds.Width;
			var height = map.Bounds.Height;

			if (!actualSize)
				width = height = Exts.NextPowerOf2(Math.Max(map.Bounds.Width, map.Bounds.Height));

			var terrain = new Bitmap(width, height);

			var bitmapData = terrain.LockBits(terrain.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			var mapTiles = map.MapTiles.Value;

			unsafe
			{
				var c = (int*)bitmapData.Scan0;

				for (var x = 0; x < map.Bounds.Width; x++)
					for (var y = 0; y < map.Bounds.Height; y++)
					{
						var mapX = x + map.Bounds.Left;
						var mapY = y + map.Bounds.Top;
						var type = tileset.GetTerrainInfo(mapTiles[mapX, mapY]);

						*(c + (y * bitmapData.Stride >> 2) + x) = type.Color.ToArgb();
					}
			}

			terrain.UnlockBits(bitmapData);
			return terrain;
		}

		// Add the static resources defined in the map; if the map lives
		// in a world use AddCustomTerrain instead
		static Bitmap AddStaticResources(TileSet tileset, Map map, Ruleset resourceRules, Bitmap terrainBitmap)
		{
			var terrain = new Bitmap(terrainBitmap);

			var bitmapData = terrain.LockBits(terrain.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				var c = (int*)bitmapData.Scan0;

				for (var x = 0; x < map.Bounds.Width; x++)
					for (var y = 0; y < map.Bounds.Height; y++)
					{
						var mapX = x + map.Bounds.Left;
						var mapY = y + map.Bounds.Top;
						if (map.MapResources.Value[mapX, mapY].Type == 0)
							continue;

						var res = resourceRules.Actors["world"].Traits.WithInterface<ResourceTypeInfo>()
								.Where(t => t.ResourceType == map.MapResources.Value[mapX, mapY].Type)
								.Select(t => t.TerrainType).FirstOrDefault();
						if (res == null)
							continue;

						*(c + (y * bitmapData.Stride >> 2) + x) = tileset[tileset.GetTerrainIndex(res)].Color.ToArgb();
					}
			}

			terrain.UnlockBits(bitmapData);

			return terrain;
		}

		public static Bitmap CustomTerrainBitmap(World world)
		{
			var map = world.Map;
			var size = Exts.NextPowerOf2(Math.Max(map.Bounds.Width, map.Bounds.Height));
			var bitmap = new Bitmap(size, size);
			var bitmapData = bitmap.LockBits(bitmap.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				var c = (int*)bitmapData.Scan0;

				for (var x = 0; x < map.Bounds.Width; x++)
					for (var y = 0; y < map.Bounds.Height; y++)
					{
						var mapX = x + map.Bounds.Left;
						var mapY = y + map.Bounds.Top;
						var custom = map.CustomTerrain[mapX, mapY];
						if (custom == -1)
							continue;
						*(c + (y * bitmapData.Stride >> 2) + x) = world.TileSet[custom].Color.ToArgb();
					}
			}

			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}

		public static Bitmap ActorsBitmap(World world)
		{
			var map = world.Map;
			var size = Exts.NextPowerOf2(Math.Max(map.Bounds.Width, map.Bounds.Height));
			var bitmap = new Bitmap(size, size);
			var bitmapData = bitmap.LockBits(bitmap.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				var c = (int*)bitmapData.Scan0;

				foreach (var t in world.ActorsWithTrait<IRadarSignature>())
				{
					if (world.FogObscures(t.Actor))
						continue;

					var color = t.Trait.RadarSignatureColor(t.Actor);
					foreach (var cell in t.Trait.RadarSignatureCells(t.Actor))
						if (world.Map.IsInMap(cell))
							*(c + ((cell.Y - world.Map.Bounds.Top) * bitmapData.Stride >> 2) + cell.X - world.Map.Bounds.Left) = color.ToArgb();
				}
			}

			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}

		public static Bitmap ShroudBitmap(World world)
		{
			var map = world.Map;
			var size = Exts.NextPowerOf2(Math.Max(map.Bounds.Width, map.Bounds.Height));
			var bitmap = new Bitmap(size, size);
			if (world.RenderPlayer == null)
				return bitmap;

			var bitmapData = bitmap.LockBits(bitmap.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			var shroud = Color.Black.ToArgb();
			var fog = Color.FromArgb(128, Color.Black).ToArgb();

			unsafe
			{
				var c = (int*)bitmapData.Scan0;

				for (var x = 0; x < map.Bounds.Width; x++)
					for (var y = 0; y < map.Bounds.Height; y++)
					{
						var p = new CPos(x + map.Bounds.Left, y + map.Bounds.Top);
						if (world.ShroudObscures(p))
							*(c + (y * bitmapData.Stride >> 2) + x) = shroud;
						else if (world.FogObscures(p))
							*(c + (y * bitmapData.Stride >> 2) + x) = fog;
					}
			}

			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}

		public static Bitmap RenderMapPreview(TileSet tileset, Map map, bool actualSize)
		{
			return RenderMapPreview(tileset, map, map.Rules, actualSize);
		}

		public static Bitmap RenderMapPreview(TileSet tileset, Map map, Ruleset resourceRules, bool actualSize)
		{
			var terrain = TerrainBitmap(tileset, map, actualSize);
			return AddStaticResources(tileset, map, resourceRules, terrain);
		}
	}
}
