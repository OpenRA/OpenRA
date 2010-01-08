using System.Drawing;
using System.Linq;
using OpenRa.Game.Traits;
using OpenRa.FileFormats;

namespace OpenRa.Game.Graphics
{
	class Minimap
	{
		Sheet sheet;
		SpriteRenderer rgbaRenderer;
		SpriteRenderer shpRenderer;
		Sprite sprite;
		Bitmap terrain, oreLayer;
		Animation radarAnim, alliesAnim, sovietAnim;

		public void Tick() { }

		public Minimap(Renderer r)
		{
			sheet = new Sheet(r, new Size(128, 128));
			shpRenderer = new SpriteRenderer(r, true);
			rgbaRenderer = new SpriteRenderer(r, true, r.RgbaSpriteShader);
			sprite = new Sprite(sheet, new Rectangle(0, 0, 128, 128), TextureChannel.Alpha);

			sovietAnim = new Animation("ussrradr");
			sovietAnim.PlayRepeating("idle");
			alliesAnim = new Animation("natoradr");
			alliesAnim.PlayRepeating("idle");
			radarAnim = Game.LocalPlayer.Race == Race.Allies ? alliesAnim : sovietAnim;
		}

		Color[] terrainTypeColors;

		public void InvalidateOre() { oreLayer = null; }

		public void Update()
		{
			radarAnim = Game.LocalPlayer.Race == Race.Allies ? alliesAnim : sovietAnim;
			radarAnim.Tick();

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
			
			for (var y = 0; y < 128; y++ )
				for (var x = 0; x < 128; x++ )
				{
					if( ! Game.LocalPlayer.Shroud.IsExplored(new int2(x,y)))
						bitmap.SetPixel(x, y, Color.Black);			
				}
			
			sheet.Texture.SetData(bitmap);
		}

		public void Draw(float2 pos, bool hasRadar, bool isJammed)
		{
			if (hasRadar && radarAnim.CurrentSequence.Name == "idle")
				radarAnim.PlayThen("open", () => radarAnim.PlayRepeating("active"));
			if (hasRadar && radarAnim.CurrentSequence.Name == "no-power")
				radarAnim.PlayBackwardsThen("close", () => radarAnim.PlayRepeating("active"));
			if (!hasRadar && radarAnim.CurrentSequence.Name == "active")
				radarAnim.PlayThen("close", () => radarAnim.PlayRepeating("no-power"));
			if (isJammed && radarAnim.CurrentSequence.Name == "active")
				radarAnim.PlayRepeating("jammed");
			if (!isJammed && radarAnim.CurrentSequence.Name == "jammed")
				radarAnim.PlayRepeating("active");
				
			shpRenderer.DrawSprite(radarAnim.Image, pos + Game.viewport.Location - new float2( 290-256,0), PaletteType.Chrome, new float2(290, 272));
			shpRenderer.Flush();

			if (radarAnim.CurrentSequence.Name == "active")
			{
				rgbaRenderer.DrawSprite(sprite, pos - new float2((290-256)/2, -5), PaletteType.Chrome, new float2(256, 256));
				rgbaRenderer.Flush();
			}
		}
	}
}
