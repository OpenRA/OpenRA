using System;
using System.Drawing;
using System.Linq;
using OpenRa.Traits;
using OpenRa.FileFormats;
using System.Drawing.Imaging;

namespace OpenRa.Graphics
{
	class Minimap
	{
		Sheet sheet, mapOnlySheet;
		SpriteRenderer rgbaRenderer;
		Sprite sprite, mapOnlySprite;
		Bitmap terrain, oreLayer;
		const int alpha = 230;

		public void Tick() { }

		public Minimap(Renderer r)
		{
			sheet = new Sheet(r, new Size(128, 128));
			mapOnlySheet = new Sheet(r, new Size(128, 128));

			rgbaRenderer = new SpriteRenderer(r, true, r.RgbaSpriteShader);
			var size = Math.Max(Game.world.Map.Width, Game.world.Map.Height);
			var dw = (size - Game.world.Map.Width) / 2;
			var dh = (size - Game.world.Map.Height) / 2;
			
			sprite = new Sprite(sheet, new Rectangle(Game.world.Map.Offset.X+dw, Game.world.Map.Offset.Y+dh, size, size), TextureChannel.Alpha);
			mapOnlySprite = new Sprite(mapOnlySheet, new Rectangle(Game.world.Map.Offset.X + dw, Game.world.Map.Offset.Y + dh, size, size), TextureChannel.Alpha);
		}

		Color[] terrainTypeColors;
		Color[] playerColors;
		Color shroudColor;
		string theater;

		public void InvalidateOre() { oreLayer = null; }

		public void Update()
		{
			if (Game.world.Map.Theater != theater)
			{
				terrainTypeColors = null;
				theater = Game.world.Map.Theater;
			}

			if (terrainTypeColors == null)
			{
				var pal = new Palette(FileSystem.Open(Game.world.Map.Theater + ".pal"));
				terrainTypeColors = new[] {theater.ToLowerInvariant() == "snow" ? 0xe3 :0x1a, 0x63, 0x2f, 0x1f, 0x14, 0x64, 0x1f, 0x68, 0x6b, 0x6d }
					.Select( a => Color.FromArgb(alpha, pal.GetColor(a) )).ToArray();
				
				playerColors = Util.MakeArray<Color>( 8, b => Color.FromArgb(alpha, Chat.paletteColors[b]) );
				shroudColor = Color.FromArgb(alpha, Color.Black);
			}
			
			if (terrain == null)
			{
				terrain = new Bitmap(128, 128);
				for (var y = 0; y < 128; y++)
					for (var x = 0; x < 128; x++)
						terrain.SetPixel(x, y, Game.world.Map.IsInMap(x, y)
							? terrainTypeColors[Game.world.TileSet.GetWalkability(Game.world.Map.MapTiles[x, y])]
							: shroudColor);
			}

			if (oreLayer == null)
			{
				oreLayer = new Bitmap(terrain);
				for (var y = 0; y < 128; y++)
					for (var x = 0; x < 128; x++)
						if (Game.world.Map.ContainsResource(new int2(x, y)))
						oreLayer.SetPixel(x, y, terrainTypeColors[(int)TerrainMovementType.Ore]);
			}

			mapOnlySheet.Texture.SetData(oreLayer);

			if (!Game.world.Actors.Any(a => a.Owner == Game.LocalPlayer && a.traits.Contains<ProvidesRadar>()))
				return;

			var bitmap = new Bitmap(oreLayer);
			var bitmapData = bitmap.LockBits(new Rectangle( 0,0,bitmap.Width, bitmap.Height ), 
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* c = (int*)bitmapData.Scan0;

				for (var y = 0; y < 128; y++)
					for (var x = 0; x < 128; x++)
					{
						var b = Game.BuildingInfluence.GetBuildingAt(new int2(x, y));
						if (b != null)
							*(c + (y * bitmapData.Stride >> 2) + x) =
								(b.Owner != null ? playerColors[(int)b.Owner.Palette] : terrainTypeColors[4]).ToArgb();
					}

				foreach (var a in Game.world.Actors.Where(a => a.traits.Contains<Unit>()))
					*(c + (a.Location.Y * bitmapData.Stride >> 2) + a.Location.X) =
						playerColors[(int)a.Owner.Palette].ToArgb();

				unchecked
				{
					for (var y = 0; y < 128; y++)
						for (var x = 0; x < 128; x++)
							if (!Game.LocalPlayer.Shroud.DisplayOnRadar(x, y))
								*(c + (y * bitmapData.Stride >> 2) + x) = shroudColor.ToArgb();
				}
			}

			bitmap.UnlockBits(bitmapData);
			sheet.Texture.SetData(bitmap);
		}

		public void Draw(RectangleF rect, bool mapOnly)
		{
			rgbaRenderer.DrawSprite(mapOnly ? mapOnlySprite : sprite, 
				new float2(rect.X, rect.Y), PaletteType.Chrome, new float2(rect.Width, rect.Height));
			rgbaRenderer.Flush();
		}
	}
}
