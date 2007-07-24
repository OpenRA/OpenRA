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

		Dictionary<string, Sprite> sprites = new Dictionary<string,Sprite>();
		Viewport viewport;
		const float spriteWidth = 64, spriteHeight = 48;

		public float Width
		{
			get { return spriteWidth * 2; }
		}


		public Sidebar( TechTree.TechTree techTree, Race race, Renderer renderer, Viewport viewport )
		{
			this.techTree = techTree;
			this.viewport = viewport;
			viewport.AddRegion( Region.Create(viewport, DockStyle.Right, 128, Paint));
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

		float2 location;

		public float2 Location
		{
			get { return location; }
		}

		public void Paint( Game game )
		{
			float2 buildPos = location = viewport.Location + new float2(viewport.Size.X - spriteWidth * 2, 0);
			float2 unitPos = viewport.Location + new float2(viewport.Size.X - spriteWidth, 0);
			
			foreach (Item i in techTree.BuildableItems)
			{
				Sprite sprite;
				if (!sprites.TryGetValue(i.tag, out sprite)) continue;

				if (i.IsStructure)
					DrawSprite( sprite, ref buildPos );
				else
					DrawSprite( sprite, ref unitPos );
			}

			Fill( viewport.Location.Y + viewport.Size.Y, buildPos );
			Fill( viewport.Location.Y + viewport.Size.Y, unitPos );

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
					rect = new RectangleF(location.X, location.Y + y1, spriteWidth, spriteHeight);
					y1 += 48;
				}
				else
				{
					rect = new RectangleF(location.X + spriteWidth, location.Y + y2, spriteWidth, spriteHeight);
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
