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
			var isDiamond = map.TileShape == TileShape.Diamond;
			var b = map.Bounds;
			var width = b.Width;
			var height = b.Height;

			var bitmapWidth = width;
			if (isDiamond)
				bitmapWidth = 2 * bitmapWidth - 1;

			if (!actualSize)
				bitmapWidth = height = Exts.NextPowerOf2(Math.Max(bitmapWidth, height));

			var terrain = new Bitmap(bitmapWidth, height);

			var bitmapData = terrain.LockBits(terrain.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			var mapTiles = map.MapTiles.Value;

			unsafe
			{
				var colors = (int*)bitmapData.Scan0;
				var stride = bitmapData.Stride / 4;
				for (var y = 0; y < height; y++)
				{
					for (var x = 0; x < width; x++)
					{
						var uv = new MPos(x + b.Left, y + b.Top);
						var type = tileset.GetTileInfo(mapTiles[uv]);
						var leftColor = type != null ? type.LeftColor : Color.Black;

						if (isDiamond)
						{
							// Odd rows are shifted right by 1px
							var dx = uv.V & 1;
							var rightColor = type != null ? type.RightColor : Color.Black;
							if (x + dx > 0)
								colors[y * stride + 2 * x + dx - 1] = leftColor.ToArgb();

							colors[y * stride + 2 * x + dx] = rightColor.ToArgb();
						}
						else
							colors[y * stride + x] = leftColor.ToArgb();
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
			var isDiamond = map.TileShape == TileShape.Diamond;
			var b = map.Bounds;
			var width = b.Width;
			var height = b.Height;

			var resources = resourceRules.Actors["world"].Traits.WithInterface<ResourceTypeInfo>()
				.ToDictionary(r => r.ResourceType, r => r.TerrainType);

			var bitmapData = terrain.LockBits(terrain.Bounds(),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				var colors = (int*)bitmapData.Scan0;
				var stride = bitmapData.Stride / 4;
				for (var y = 0; y < height; y++)
				{
					for (var x = 0; x < width; x++)
					{
						var uv = new MPos(x + b.Left, y + b.Top);
						if (map.MapResources.Value[uv].Type == 0)
							continue;

						string res;
						if (!resources.TryGetValue(map.MapResources.Value[uv].Type, out res))
							continue;

						var color = tileset[tileset.GetTerrainIndex(res)].Color.ToArgb();
						if (isDiamond)
						{
							// Odd rows are shifted right by 1px
							var dx = uv.V & 1;
							if (x + dx > 0)
								colors[y * stride + 2 * x + dx - 1] = color;

							colors[y * stride + 2 * x + dx] = color;
						}
						else
							colors[y * stride + x] = color;
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
