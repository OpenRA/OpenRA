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
		readonly World world;
		Sheet sheet;
		Sprite sprite;
		Bitmap terrain, customLayer;
		Rectangle bounds;
		
		const int alpha = 230;

		public Minimap(World world)
		{
			this.world = world;
			sheet = new Sheet( new Size(world.Map.MapSize.X, world.Map.MapSize.Y));
			var size = Math.Max(world.Map.Width, world.Map.Height);
			var dw = (size - world.Map.Width) / 2;
			var dh = (size - world.Map.Height) / 2;

			bounds = new Rectangle(world.Map.TopLeft.X - dw, world.Map.TopLeft.Y - dh, size, size);

			sprite = new Sprite(sheet, bounds, TextureChannel.Alpha);
		
			shroudColor = Color.FromArgb(alpha, Color.Black);
		}

		public static Rectangle MakeMinimapBounds(Map m)
		{
			var size = Math.Max(m.Width, m.Height);
			var dw = (size - m.Width) / 2;
			var dh = (size - m.Height) / 2;

			return new Rectangle(m.TopLeft.X - dw, m.TopLeft.Y - dh, size, size);
		}
		
		static Color shroudColor;

		public void InvalidateCustom() { customLayer = null; }

		public void Update()
		{			
			if (terrain == null)
				terrain = RenderTerrainBitmap(world.Map);

			
			// Custom terrain layer
			if (customLayer == null)
				customLayer = AddCustomTerrain(world,terrain);		

			if (!world.GameHasStarted || !world.Queries.OwnedBy[world.LocalPlayer].WithTrait<ProvidesRadar>().Any())
				return;
			
			sheet.Texture.SetData(AddActors(world, customLayer));
		}

		public void Draw(RectangleF rect)
		{
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(sprite, 
				new float2(rect.X, rect.Y), "chrome", new float2(rect.Width, rect.Height));
			Game.Renderer.RgbaSpriteRenderer.Flush();
		}

		public static int2 CellToMinimapPixel(Map map, RectangleF viewRect, int2 p)
		{
			var size = Math.Max(map.Width, map.Height);
			var dw = (size - map.Width) / 2;
			var dh = (size - map.Height) / 2;
			var bounds = new Rectangle(map.TopLeft.X - dw, map.TopLeft.Y - dh, size, size);

			var fx = (float)(p.X - bounds.X) / bounds.Width;
			var fy = (float)(p.Y - bounds.Y) / bounds.Height;

			return new int2(
				(int)(viewRect.Width * fx + viewRect.Left),
				(int)(viewRect.Height * fy + viewRect.Top));
		}

		public static int2 MinimapPixelToCell(Map map, RectangleF viewRect, int2 p)
		{
			var size = Math.Max(map.Width, map.Height);
			var dw = (size - map.Width) / 2;
			var dh = (size - map.Height) / 2;
			var bounds = new Rectangle(map.TopLeft.X - dw, map.TopLeft.Y - dh, size, size);
			
			var fx = (float)(p.X - viewRect.Left) / viewRect.Width;
			var fy = (float)(p.Y - viewRect.Top) / viewRect.Height;

			return new int2(
				(int)(bounds.Width * fx + bounds.Left),
				(int)(bounds.Height * fy + bounds.Top));
		}
		
		
		static int NextPowerOf2(int v)
		{
			--v;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			++v;
			return v;
		}
		
		public static Bitmap RenderTerrainBitmap(Map map)
		{
			var tileset = Rules.TileSets[map.Tileset];
			var size = NextPowerOf2(Math.Max(map.Width, map.Height));
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
						*(c + (y * bitmapData.Stride >> 2) + x) = map.IsInMap(mapX, mapY)
							? Color.FromArgb(alpha, tileset.Terrain[type].Color).ToArgb()
							: shroudColor.ToArgb();
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
						
						if (!world.LocalPlayer.Shroud.DisplayOnRadar(mapX, mapY))
						{
							*(c + (y * bitmapData.Stride >> 2) + x) = shroudColor.ToArgb();
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
