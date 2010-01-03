using System.Drawing;

namespace OpenRa.Game.Graphics
{
	class Minimap
	{
		Sheet sheet;
		SpriteRenderer spriteRenderer;
		Sprite sprite;

		public void Tick() { }

		public Minimap(Renderer r)
		{
			sheet = new Sheet(r, new Size(128, 128));
			spriteRenderer = new SpriteRenderer(r, true, r.RgbaSpriteShader);
			sprite = new Sprite(sheet, new Rectangle(0, 0, 128, 128), TextureChannel.Alpha);
		}

		public void Update()
		{
			var bitmap = new Bitmap(128, 128);
			
			for( var y = 0; y < 128; y++ )
				for (var x = 0; x < 128; x++)
				{
					// todo: use player color
					var b = Game.BuildingInfluence.GetBuildingAt(new int2(x, y));
					if (b != null && b.Owner != null)
						bitmap.SetPixel(x, y, Chat.paletteColors[ (int)b.Owner.Palette ]);
					//else
					//    bitmap.SetPixel(x, y, Color.Red);
				}

			sheet.Texture.SetData(bitmap);
		}

		public void Draw(float2 pos)
		{
			spriteRenderer.DrawSprite(sprite, pos, PaletteType.Chrome);
			spriteRenderer.Flush();
		}
	}
}
