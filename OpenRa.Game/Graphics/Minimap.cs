using System.Drawing;
using System.Linq;
using OpenRa.Game.Traits;

namespace OpenRa.Game.Graphics
{
	class Minimap
	{
		Sheet sheet;
		SpriteRenderer spriteRenderer;
		Sprite sprite;
		Bitmap terrain;

		public void Tick() { }

		public Minimap(Renderer r)
		{
			sheet = new Sheet(r, new Size(128, 128));
			spriteRenderer = new SpriteRenderer(r, true, r.RgbaSpriteShader);
			sprite = new Sprite(sheet, new Rectangle(0, 0, 128, 128), TextureChannel.Alpha);
		}

		// todo: extract these from the palette
		static readonly Color[] terrainTypeColors = { 
														Color.Green,
														Color.Red,
														Color.Blue,
														Color.Yellow,
														Color.Purple,
														Color.Turquoise,
														Color.Violet,
														Color.Tomato,
														Color.Teal,
													};

		public void Update()
		{
			if (terrain == null)
			{
				terrain = new Bitmap(128, 128);
				for (var y = 0; y < 128; y++)
					for (var x = 0; x < 128; x++)
						terrain.SetPixel(x, y, Rules.Map.IsInMap(x, y)
							? terrainTypeColors[Rules.TileSet.GetWalkability(Rules.Map.MapTiles[x, y])]
							: Color.Black);
			}

			var bitmap = new Bitmap(terrain);
			
			for( var y = 0; y < 128; y++ )
				for (var x = 0; x < 128; x++)
				{
					var b = Game.BuildingInfluence.GetBuildingAt(new int2(x, y));
					if (b != null)
						bitmap.SetPixel(x, y, b.Owner != null ? Chat.paletteColors[(int)b.Owner.Palette] : Color.Gray);
				}

			foreach (var a in Game.world.Actors.Where(a => a.traits.Contains<Unit>()))
				bitmap.SetPixel(a.Location.X, a.Location.Y, Chat.paletteColors[(int)a.Owner.Palette]);

			sheet.Texture.SetData(bitmap);
		}

		public void Draw(float2 pos)
		{
			spriteRenderer.DrawSprite(sprite, pos, PaletteType.Chrome);
			spriteRenderer.Flush();
		}
	}
}
