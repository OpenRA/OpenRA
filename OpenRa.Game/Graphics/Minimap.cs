using System;
using System.Drawing;
using System.Linq;
using OpenRa.Game.Traits;
using OpenRa.FileFormats;
using System.Drawing.Imaging;

namespace OpenRa.Game.Graphics
{
	class Minimap
	{
		Sheet sheet;
		SpriteRenderer rgbaRenderer;
		Sprite sprite;
		Bitmap terrain, oreLayer;
		const int alpha = 230;

		public void Tick() { }

		public Minimap(Renderer r)
		{
			sheet = new Sheet(r, new Size(128, 128));

			rgbaRenderer = new SpriteRenderer(r, true, r.RgbaSpriteShader);
			var size = Math.Max(Rules.Map.Width, Rules.Map.Height);
			var dw = (size - Rules.Map.Width) / 2;
			var dh = (size - Rules.Map.Height) / 2;
			
			sprite = new Sprite(sheet, new Rectangle(Rules.Map.Offset.X+dw, Rules.Map.Offset.Y+dh, size, size), TextureChannel.Alpha);
		}

		Color[] terrainTypeColors;
		Color[] playerColors;
		Color shroudColor;

		public void InvalidateOre() { oreLayer = null; }

		public void Update()
		{
			if (!Game.world.Actors.Any(a => a.Owner == Game.LocalPlayer && a.traits.Contains<ProvidesRadar>()))
				return;

			if (terrainTypeColors == null)
			{
				var pal = new Palette(FileSystem.Open(Rules.Map.Theater + ".pal"));
				terrainTypeColors = new[] {
					Color.FromArgb(alpha, pal.GetColor(0x1a)),
					Color.FromArgb(alpha, pal.GetColor(0x63)),
					Color.FromArgb(alpha, pal.GetColor(0x2f)),
					Color.FromArgb(alpha, pal.GetColor(0x1f)),
					Color.FromArgb(alpha, pal.GetColor(0x14)),
					Color.FromArgb(alpha, pal.GetColor(0x64)),
					Color.FromArgb(alpha, pal.GetColor(0x1f)),
					Color.FromArgb(alpha, pal.GetColor(0x68)),
					Color.FromArgb(alpha, pal.GetColor(0x6b)),
					Color.FromArgb(alpha, pal.GetColor(0x6d)),
				};
				
				playerColors = Util.MakeArray<Color>( 8, b => Color.FromArgb(alpha, Chat.paletteColors[b]) );
				shroudColor = Color.FromArgb(alpha, Color.Black);
			}
			
			if (terrain == null)
			{
				terrain = new Bitmap(128, 128);
				for (var y = 0; y < 128; y++)
					for (var x = 0; x < 128; x++)
						terrain.SetPixel(x, y, Rules.Map.IsInMap(x, y)
							? terrainTypeColors[Rules.TileSet.GetWalkability(Rules.Map.MapTiles[x, y])]
							: shroudColor);
			}

			if (oreLayer == null)
			{
				oreLayer = new Bitmap(terrain);
				for (var y = 0; y < 128; y++)
					for (var x = 0; x < 128; x++)
						if (Rules.Map.ContainsResource(new int2(x, y)))
						oreLayer.SetPixel(x, y, terrainTypeColors[(int)TerrainMovementType.Ore]);
			}

			var bitmap = new Bitmap(oreLayer);
			var bitmapData = bitmap.LockBits(new Rectangle( 0,0,bitmap.Width, bitmap.Height ), 
				ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				int* c = (int *)bitmapData.Scan0;

				for (var y = 0; y < 128; y++)
					for (var x = 0; x < 128; x++)
					{
						var b = Game.BuildingInfluence.GetBuildingAt(new int2(x, y));
						if (b != null)
							*(c + (y * bitmapData.Stride >> 2) + x) =
								(b.Owner != null ? playerColors[(int)b.Owner.Palette] : terrainTypeColors[4]).ToArgb();
					}

				foreach (var a in Game.world.Actors.Where(a => a.traits.Contains<Unit>()))
					*(c + (a.Location.Y * bitmapData.Stride >> 2) + a.Location.X) = Chat.paletteColors[(int)a.Owner.Palette].ToArgb();
				
				unchecked
				{
					for (var y = 0; y < 128; y++)
						for (var x = 0; x < 128; x++)
							if (!Game.LocalPlayer.Shroud.IsExplored(new int2(x, y)))
								*(c + (y * bitmapData.Stride >> 2) + x) = shroudColor.ToArgb();
				}
			}

			bitmap.UnlockBits(bitmapData);
			sheet.Texture.SetData(bitmap);
		}

		public void Draw(RectangleF rect, bool hasRadar, bool isJammed)
		{
			rgbaRenderer.DrawSprite(sprite, new float2(rect.X, rect.Y), PaletteType.Chrome, new float2(rect.Width, rect.Height));
			rgbaRenderer.Flush();
		}
	}
}
