using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.TechTree;
using BluntDirectX.Direct3D;
using OpenRa.FileFormats;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace OpenRa.Game
{
	class Sidebar
	{
		TechTree.TechTree techTree;

		SpriteRenderer spriteRenderer;
		Sprite blank;
		readonly Region region;

		public Region Region
		{
			get { return region; }
		}

		Dictionary<string, Sprite> sprites = new Dictionary<string,Sprite>();
		const float spriteWidth = 64, spriteHeight = 48;

		public float Width
		{
			get { return spriteWidth * 2; }
		}


		public Sidebar( TechTree.TechTree techTree, Race race, Renderer renderer, Viewport viewport )
		{
			this.techTree = techTree;
			region = Region.Create(viewport, DockStyle.Right, 128, Paint);
			viewport.AddRegion( region );
			techTree.CurrentRace = race;
			techTree.Build("FACT", true);
			spriteRenderer = new SpriteRenderer(renderer, false);

			LoadSprites("buildings.txt");
			LoadSprites("units.txt");

			blank = SheetBuilder.Add(new Size((int)spriteWidth, (int)spriteHeight), 16);
		}

		public void Build(string key, Game game )
		{
			game.world.orderGenerator = new PlaceBuilding( 1, key.ToLowerInvariant() );
		}

		void LoadSprites(string filename)
		{
			foreach (string line in Util.ReadAllLines(FileSystem.Open(filename)))
			{
				string key = line.Substring(0, line.IndexOf(','));
				sprites.Add(key, SpriteSheetBuilder.LoadSprite(key + "icon.shp"));
			}
		}

		void DrawSprite(Sprite s, ref float2 p)
		{
			spriteRenderer.DrawSprite(s, p, 0);
			p.Y += spriteHeight;
		}

		void Fill(float height, float2 p)
		{
			while (p.Y < height)
				DrawSprite(blank, ref p);
		}

		void Paint()
		{
			float2 buildPos = region.Location + new float2(region.Size.X - spriteWidth * 2, 0);
			float2 unitPos = region.Location + new float2(region.Size.X - spriteWidth, 0);
			
			foreach (Item i in techTree.BuildableItems)
			{
				Sprite sprite;
				if (!sprites.TryGetValue(i.tag, out sprite)) continue;

				if (i.IsStructure)
					DrawSprite( sprite, ref buildPos );
				else
					DrawSprite( sprite, ref unitPos );
			}

			Fill( region.Location.Y + region.Size.Y, buildPos );
			Fill( region.Location.Y + region.Size.Y, unitPos );

			spriteRenderer.Flush();
		}

		public string FindSpriteAtPoint(float2 point)
		{
			float y1 = 0, y2 = 0;
			foreach (Item i in techTree.BuildableItems)
			{
				RectangleF rect;
				if (i.IsStructure)
				{
					rect = new RectangleF(region.Location.X, region.Location.Y + y1, spriteWidth, spriteHeight);
					y1 += 48;
				}
				else
				{
					rect = new RectangleF(region.Location.X + spriteWidth, region.Location.Y + y2, spriteWidth, spriteHeight);
					y2 += 48;
				}
				if (rect.Contains(point.ToPointF())) return i.tag;
			}
			return null;
		}
	}

	class PlaceBuilding : IOrderGenerator
	{
		int palette;
		string buildingName;

		public PlaceBuilding( int palette, string buildingName )
		{
			this.palette = palette;
			this.buildingName = buildingName;
		}

		public IOrder Order( int2 xy )
		{
			// todo: check that space is free
			return new PlaceBuildingOrder( this, xy );
		}

		class PlaceBuildingOrder : IOrder
		{
			PlaceBuilding building;
			int2 xy;

			public PlaceBuildingOrder( PlaceBuilding building, int2 xy )
			{
				this.building = building;
				this.xy = xy;
			}

			public void Apply( Game game )
			{
				game.world.AddFrameEndTask( delegate
				{
					Provider<Building, int2, int> newBuilding;
					if( game.buildingCreation.TryGetValue( building.buildingName, out newBuilding ) )
					{
						game.world.Add( newBuilding( xy, building.palette ) );
						game.techTree.Build( building.buildingName );
					}
					game.world.orderGenerator = null;
				} );
			}
		}
	}
}
