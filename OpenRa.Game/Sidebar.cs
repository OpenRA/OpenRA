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

		SpriteRenderer spriteRenderer, clockRenderer;
		Sprite blank;
		Game game;
		readonly Region region;

		Animation clockAnimation = new Animation("clock");

		public Region Region { get { return region; } }
		public float Width { get { return spriteWidth * 2; } }

		Dictionary<string, Sprite> sprites = new Dictionary<string,Sprite>();
		const int spriteWidth = 64, spriteHeight = 48;

		List<SidebarItem> items = new List<SidebarItem>();

		public Sidebar( Race race, Renderer renderer, Game game )
		{
			this.techTree = game.techTree;
			this.game = game;
			region = Region.Create(game.viewport, DockStyle.Right, 128, Paint, MouseHandler);
			game.viewport.AddRegion( region );
			techTree.CurrentRace = race;
			techTree.Build("FACT", true);
			spriteRenderer = new SpriteRenderer(renderer, false);
			clockRenderer = new SpriteRenderer(renderer, true);

			LoadSprites("buildings.txt");
			LoadSprites("units.txt");

			blank = SheetBuilder.Add(new Size((int)spriteWidth, (int)spriteHeight), 16);

			clockAnimation.PlayRepeating("idle");
		}

		public void Build(SidebarItem item)
		{
			if( item != null )
				game.world.orderGenerator = new PlaceBuilding( game.players[ 1 ], item.techTreeItem.tag.ToLowerInvariant() );
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

		void PopulateItemList()
		{
			int buildPos = 0, unitPos = 0;

			items.Clear();

			foreach (Item i in techTree.BuildableItems)
			{
				Sprite sprite;
				if (!sprites.TryGetValue(i.tag, out sprite)) continue;

				items.Add(new SidebarItem(sprite, i, i.IsStructure ? buildPos : unitPos));

				if (i.IsStructure)
					buildPos += spriteHeight;
				else
					unitPos += spriteHeight;
			}
		}

		void Paint()
		{
			PopulateItemList();	// todo: do this less often, just when things actually change!

			foreach (SidebarItem i in items)
				i.Paint(spriteRenderer, region.Location);

			spriteRenderer.Flush();	//todo: fix filling

			clockRenderer.DrawSprite( clockAnimation.Images[0], region.Location, 0 );
			clockAnimation.Tick(1);

			clockRenderer.Flush();
		}

		public SidebarItem GetItem(float2 point)
		{
			foreach (SidebarItem i in items)
				if (i.Clicked(point))
					return i;

			return null;
		}

		void MouseHandler(object sender, MouseEventArgs e)
		{
			float2 point = new float2(e.Location);
			Build(GetItem(point));
		}
	}

	class PlaceBuilding : IOrderGenerator
	{
		Player owner;
		string buildingName;

		public PlaceBuilding( Player owner, string buildingName )
		{
			this.owner = owner;
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
					Provider<Building, int2, Player> newBuilding;
					if( game.buildingCreation.TryGetValue( building.buildingName, out newBuilding ) )
					{
						Log.Write( "Player \"{0}\" builds {1}", building.owner.PlayerName, building.buildingName );
						game.world.Add( newBuilding( xy, building.owner ) );
						game.techTree.Build( building.buildingName );
					}
					game.world.orderGenerator = null;
				} );
			}
		}
	}
}
