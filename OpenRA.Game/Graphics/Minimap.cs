#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
			var b = map.Bounds;
			var width = b.Width;
			var height = b.Height;

			if (!actualSize)
				width = height = Exts.NextPowerOf2(Math.Max(b.Width, b.Height));

			var terrain = new Bitmap(width, height);

			var bitmapData = terrain.LockBits(terrain.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			var mapTiles = map.MapTiles.Value;

			unsafe
			{
				var colors = (int*)bitmapData.Scan0;
				var stride = bitmapData.Stride / 4;
				for (var y = 0; y < b.Height; y++)
				{
					for (var x = 0; x < b.Width; x++)
					{
						var mapX = x + b.Left;
						var mapY = y + b.Top;
						var type = tileset.GetTileInfo(mapTiles[new MPos(mapX, mapY)]);
						var color = type != null ? type.LeftColor : Color.Black;

						colors[y * stride + x] = color.ToArgb();
					}
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
			var b = map.Bounds;

			var bitmapData = terrain.LockBits(terrain.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				var colors = (int*)bitmapData.Scan0;
				var stride = bitmapData.Stride / 4;
				for (var y = 0; y < b.Height; y++)
				{
					for (var x = 0; x < b.Width; x++)
					{
						var mapX = x + b.Left;
						var mapY = y + b.Top;
						if (map.MapResources.Value[new MPos(mapX, mapY)].Type == 0)
							continue;

						var res = resourceRules.Actors["world"].Traits.WithInterface<ResourceTypeInfo>()
							.Where(t => t.ResourceType == map.MapResources.Value[new MPos(mapX, mapY)].Type)
								.Select(t => t.TerrainType).FirstOrDefault();

						if (res == null)
							continue;

						colors[y * stride + x] = tileset[tileset.GetTerrainIndex(res)].Color.ToArgb();
					}
				}
			}

			terrain.UnlockBits(bitmapData);

			return terrain;
		}

		public static Bitmap RenderMapPreview(TileSet tileset, Map map, bool actualSize)
		{
			return RenderMapPreview(tileset, map, map.Rules, actualSize);
		}

		public static Bitmap RenderMapPreview(TileSet tileset, Map map, Ruleset resourceRules, bool actualSize)
		{
			using (var terrain = TerrainBitmap(tileset, map, actualSize))
				return AddStaticResources(tileset, map, resourceRules, terrain);
		}
	}
}
