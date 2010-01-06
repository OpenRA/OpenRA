using System.Drawing;
using System.Linq;
using OpenRa.Game.Traits;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	class Minimap
	{
		Sheet sheet;
		SpriteRenderer spriteRenderer;
		Sprite sprite;
		Bitmap terrain, oreLayer;

		public void Tick() { }

		public Minimap(Renderer r)
		{
			sheet = new Sheet(r, new Size(128, 128));
			spriteRenderer = new SpriteRenderer(r, true, r.RgbaSpriteShader);
			sprite = new Sprite(sheet, new Rectangle(0, 0, 128, 128), TextureChannel.Alpha);
		}

		// todo: extract these from the palette
		Color[] terrainTypeColors;

		public void InvalidateOre() { oreLayer = null; }

		public void Update()
		{
			if (terrainTypeColors == null)
			{
				var pal = new Palette(FileSystem.Open(Rules.Map.Theater + ".pal"));
				terrainTypeColors = new[] {
					pal.GetColor(0x1a),
					pal.GetColor(0x63),
					pal.GetColor(0x2f),
					pal.GetColor(0x1f),
					pal.GetColor(0x14),
					pal.GetColor(0x64),
					pal.GetColor(0x1f),
					pal.GetColor(0x68),
					pal.GetColor(0x6b),
					pal.GetColor(0x6d),
				};
			}
			
			if (terrain == null)
			{
				terrain = new Bitmap(128, 128);
				for (var y = 0; y < 128; y++)
					for (var x = 0; x < 128; x++)
						terrain.SetPixel(x, y, Rules.Map.IsInMap(x, y)
							? terrainTypeColors[Rules.TileSet.GetWalkability(Rules.Map.MapTiles[x, y])]
							: Color.Black);
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
			
			for( var y = 0; y < 128; y++ )
				for (var x = 0; x < 128; x++)
				{
					var b = Game.BuildingInfluence.GetBuildingAt(new int2(x, y));
					if (b != null)
						bitmap.SetPixel(x, y, b.Owner != null ? Chat.paletteColors[(int)b.Owner.Palette] : terrainTypeColors[4]);
				}

			foreach (var a in Game.world.Actors.Where(a => a.traits.Contains<Unit>()))
				bitmap.SetPixel(a.Location.X, a.Location.Y, Chat.paletteColors[(int)a.Owner.Palette]);

			sheet.Texture.SetData(bitmap);
		}

		public void Draw(float2 pos)
		{
			spriteRenderer.DrawSprite(sprite, pos, PaletteType.Chrome, new float2(256,256));
			spriteRenderer.Flush();
		}
	}
}
