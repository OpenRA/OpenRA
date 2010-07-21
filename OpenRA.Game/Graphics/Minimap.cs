#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Graphics
{
	class Minimap
	{		
		public static Bitmap RenderTerrainBitmap(Map map)
		{
			var tileset = Rules.TileSets[map.Tileset];
			var size = Util.NextPowerOf2(Math.Max(map.Width, map.Height));
			Bitmap terrain = new Bitmap(size, size);
			
			var bitmapData = terrain.LockBits(new Rectangle(0, 0, terrain.Width, terrain.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
			
			unsafe
			{
				int* c = (int*)bitmapData.Scan0;

				for (var x = 0; x < map.Width; x++)
					for (var y = 0; y < map.Height; y++)
					{
						var mapX = x + map.TopLeft.X;
						var mapY = y + map.TopLeft.Y;
						var type = tileset.GetTerrainType(map.MapTiles[mapX, mapY]);
						*(c + (y * bitmapData.Stride >> 2) + x) = tileset.Terrain[type].Color.ToArgb();
					}
			}
			terrain.UnlockBits(bitmapData);
			return terrain;
		}
		
		// Add the static resources defined in the map; if the map lives
		// in a world use AddCustomTerrain instead
		public static Bitmap AddStaticResources(Map map, Bitmap terrainBitmap)
		{
			Bitmap terrain = new Bitmap(terrainBitmap);
			var tileset = Rules.TileSets[map.Tileset];

			var bitmapData = terrain.LockBits(new Rectangle(0, 0, terrain.Width, terrain.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* c = (int*)bitmapData.Scan0;

				for (var x = 0; x < map.Width; x++)
					for (var y = 0; y < map.Height; y++)
					{
						var mapX = x + map.TopLeft.X;
						var mapY = y + map.TopLeft.Y;	
						if (map.MapResources[mapX, mapY].type == 0)
							continue;
					
						var res = Rules.Info["world"].Traits.WithInterface<ResourceTypeInfo>()
								.Where(t => t.ResourceType == map.MapResources[mapX, mapY].type)
								.Select(t => t.TerrainType).FirstOrDefault();
						if (res == null)
							continue;
						
						*(c + (y * bitmapData.Stride >> 2) + x) = tileset.Terrain[res].Color.ToArgb();
					}
			}
			terrain.UnlockBits(bitmapData);

			return terrain;
		}
		
		public static Bitmap AddCustomTerrain(World world, Bitmap terrainBitmap)
		{
			var map = world.Map;
			var bitmap = new Bitmap(terrainBitmap);
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* c = (int*)bitmapData.Scan0;
					
				for (var x = 0; x < map.Width; x++)
					for (var y = 0; y < map.Height; y++)
					{
						var mapX = x + map.TopLeft.X;
						var mapY = y + map.TopLeft.Y;
						
						// This is extremely slow (~7ms). TODO: cache this information and only update when it changes
						var customTerrain = world.WorldActor.traits.WithInterface<ITerrainTypeModifier>()
							.Select( t => t.GetTerrainType(new int2(mapX, mapY)) )
							.FirstOrDefault( t => t != null );
					
						if (customTerrain == null) continue;
						
						*(c + (y * bitmapData.Stride >> 2) + x) = world.TileSet.Terrain[customTerrain].Color.ToArgb();
					}
			}
			
			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}
				
		public static Bitmap AddActors(World world, Bitmap terrain)
		{	
			var map = world.Map;
			var bitmap = new Bitmap(terrain);
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

			var shroud = Color.Black.ToArgb();
			var fogOpacity = 0.5f;
						
			unsafe
			{
				int* c = (int*)bitmapData.Scan0;

				foreach (var a in world.Queries.WithTrait<Unit>().Where(a => a.Actor.Owner != null && a.Actor.IsVisible()))
					*(c + ((a.Actor.Location.Y - world.Map.TopLeft.Y)* bitmapData.Stride >> 2) + a.Actor.Location.X - world.Map.TopLeft.X) =
						a.Actor.Owner.Color.ToArgb();

				for (var x = 0; x < map.Width; x++)
					for (var y = 0; y < map.Height; y++)
					{
						var mapX = x + map.TopLeft.X;
						var mapY = y + map.TopLeft.Y;
						
						if (!world.LocalPlayer.Shroud.IsExplored(mapX, mapY))
						{
							*(c + (y * bitmapData.Stride >> 2) + x) = shroud;
							continue;
						}
						if (!world.LocalPlayer.Shroud.IsVisible(mapX,mapY))
						{						
							*(c + (y * bitmapData.Stride >> 2) + x) = Util.LerpARGBColor(fogOpacity, *(c + (y * bitmapData.Stride >> 2) + x), shroud);
							continue;
						}
						
						var b = world.WorldActor.traits.Get<BuildingInfluence>().GetBuildingAt(new int2(mapX, mapY));
						if (b != null)
							*(c + (y * bitmapData.Stride >> 2) + x) = b.Owner.Color.ToArgb();
					}
			}

			bitmap.UnlockBits(bitmapData);
			return bitmap;
		}
		
		public static Bitmap RenderMapPreview(Map map)
		{
			Bitmap terrain = RenderTerrainBitmap(map);
			return AddStaticResources(map, terrain);
		}
	}
}
