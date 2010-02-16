using System;
using System.Drawing;
using System.Linq;
using OpenRa.Traits;
using OpenRa.FileFormats;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace OpenRa.Graphics
{
	class Minimap
	{
		readonly World world;
		Sheet sheet, mapOnlySheet, mapSpawnPointSheet;
		SpriteRenderer rgbaRenderer;
		LineRenderer lineRenderer;
		Sprite sprite, mapOnlySprite, mapSpawnPointSprite;
		Bitmap terrain, oreLayer;
		Rectangle bounds;

		Sprite ownedSpawnPoint;
		Sprite unownedSpawnPoint;

		const int alpha = 230;

		public void Tick() { }

		public Minimap(World world, Renderer r)
		{
			this.world = world;
			sheet = new Sheet(r, new Size(128, 128));
			mapOnlySheet = new Sheet(r, new Size(128, 128));
			mapSpawnPointSheet = new Sheet(r, new Size(128, 128));

			lineRenderer = new LineRenderer(r);
			rgbaRenderer = new SpriteRenderer(r, true, r.RgbaSpriteShader);
			var size = Math.Max(world.Map.Width, world.Map.Height);
			var dw = (size - world.Map.Width) / 2;
			var dh = (size - world.Map.Height) / 2;

			bounds = new Rectangle(world.Map.Offset.X - dw, world.Map.Offset.Y - dh, size, size);

			sprite = new Sprite(sheet, bounds, TextureChannel.Alpha);
			mapOnlySprite = new Sprite(mapOnlySheet, bounds, TextureChannel.Alpha);
			mapSpawnPointSprite = new Sprite(mapSpawnPointSheet, bounds, TextureChannel.Alpha);

			shroudColor = Color.FromArgb(alpha, Color.Black);

			ownedSpawnPoint = ChromeProvider.GetImage(r, "spawnpoints", "owned");
			unownedSpawnPoint = ChromeProvider.GetImage(r, "spawnpoints", "unowned");
		}

		public static Rectangle MakeMinimapBounds(Map m)
		{
			var size = Math.Max(m.Width, m.Height);
			var dw = (size - m.Width) / 2;
			var dh = (size - m.Height) / 2;

			return new Rectangle(m.Offset.X - dw, m.Offset.Y - dh, size, size);
		}

		static Cache<string, Color[]> terrainTypeColors = new Cache<string, Color[]>(
			theater =>
			{
				var pal = Game.world.WorldRenderer.GetPalette("terrain");
				return new[] {
						theater == "snow" ? 0xe3 :0x1a, 
						0x63, 0x2f, 0x1f, 0x14, 0x64, 0x1f, 0x68, 0x6b, 0x6d, 0x88 }
					.Select(a => Color.FromArgb(alpha, pal.GetColor(a))).ToArray();
			});

		static Color shroudColor;

		public void InvalidateOre() { oreLayer = null; }

		public static Bitmap RenderTerrainBitmap(Map map, TileSet tileset)
		{
			var colors = terrainTypeColors[map.Theater.ToLowerInvariant()];
			var terrain = new Bitmap(128, 128);
			for (var y = 0; y < 128; y++)
				for (var x = 0; x < 128; x++)
					terrain.SetPixel(x, y, map.IsInMap(x, y)
						? colors[tileset.GetWalkability(map.MapTiles[x, y])]
						: shroudColor);
			return terrain;
		}

		public static Bitmap RenderTerrainBitmapWithSpawnPoints(Map map, TileSet tileset)
		{
			/* todo: do this a bit nicer */

			var terrain = RenderTerrainBitmap(map, tileset);
			foreach (var sp in map.SpawnPoints)
				terrain.SetPixel(sp.X, sp.Y, Color.White);

			return terrain;
		}

		public void Update()
		{
			if (terrain == null)
				terrain = RenderTerrainBitmap(world.Map, world.TileSet);

			if (oreLayer == null)
			{
				var colors = terrainTypeColors[world.Map.Theater.ToLowerInvariant()];
				oreLayer = new Bitmap(terrain);
				for (var y = world.Map.YOffset; y < world.Map.YOffset + world.Map.Height; y++)
					for (var x = world.Map.XOffset; x < world.Map.XOffset + world.Map.Width; x++)
						if (world.Map.ContainsResource(new int2(x, y)))
							oreLayer.SetPixel(x, y, colors[(int)TerrainMovementType.Ore]);
			}

			mapOnlySheet.Texture.SetData(oreLayer);

			if (!world.Queries.OwnedBy[world.LocalPlayer].WithTrait<ProvidesRadar>().Any())
				return;

			var bitmap = new Bitmap(oreLayer);
			var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				var colors = terrainTypeColors[world.Map.Theater.ToLowerInvariant()];
				int* c = (int*)bitmapData.Scan0;

				foreach (var a in world.Queries.WithTrait<Unit>().Where( a => a.Actor.Owner != null ))
					*(c + (a.Actor.Location.Y * bitmapData.Stride >> 2) + a.Actor.Location.X) =
						Color.FromArgb(alpha, a.Actor.Owner.Color).ToArgb();

				for (var y = world.Map.YOffset; y < world.Map.YOffset + world.Map.Height; y++)
					for (var x = world.Map.XOffset; x < world.Map.XOffset + world.Map.Width; x++)
					{
						if (!world.LocalPlayer.Shroud.DisplayOnRadar(x, y))
						{
							*(c + (y * bitmapData.Stride >> 2) + x) = shroudColor.ToArgb();
							continue;
						}
						var b = world.WorldActor.traits.Get<BuildingInfluence>().GetBuildingAt(new int2(x, y));
						if (b != null)
							*(c + (y * bitmapData.Stride >> 2) + x) =
								(b.Owner != null ? Color.FromArgb(alpha, b.Owner.Color) : colors[4]).ToArgb();
					}
			}

			bitmap.UnlockBits(bitmapData);
			sheet.Texture.SetData(bitmap);
		}

		public void Draw(RectangleF rect, bool mapOnly)
		{
			rgbaRenderer.DrawSprite(mapOnly ? mapOnlySprite : sprite, 
				new float2(rect.X, rect.Y), "chrome", new float2(rect.Width, rect.Height));
			rgbaRenderer.Flush();
		}

		int2 TransformCellToMinimapPixel(RectangleF viewRect, int2 p)
		{
			var fx = (float)(p.X - bounds.X) / bounds.Width;
			var fy = (float)(p.Y - bounds.Y) / bounds.Height;

			return new int2(
				(int)(viewRect.Width * fx + viewRect.Left) - 8,
				(int)(viewRect.Height * fy + viewRect.Top) - 8);
		}

		public void DrawSpawnPoints(RectangleF rect)
		{
			var points = world.Map.SpawnPoints
				.Select( (sp,i) => Pair.New(sp,world.players.Values.FirstOrDefault( 
					p => p.SpawnPointIndex == i + 1 ) ))
				.ToList();

			foreach (var p in points)
			{
				var pos = TransformCellToMinimapPixel(rect, p.First);

				if (p.Second == null)
					rgbaRenderer.DrawSprite(unownedSpawnPoint, pos, "chrome");
				else
				{
					lineRenderer.FillRect(new RectangleF(
						Game.viewport.Location.X + pos.X + 2,
						Game.viewport.Location.Y + pos.Y + 2,
						12, 12), p.Second.Color);
			
					rgbaRenderer.DrawSprite(ownedSpawnPoint, pos, "chrome");
				}
			}

			lineRenderer.Flush();
			rgbaRenderer.Flush();
		}
	}
}
