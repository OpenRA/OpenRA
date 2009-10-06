using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using OpenRa.TechTree;

namespace OpenRa.Game
{
	using GRegion = OpenRa.Game.Graphics.Region;

	class Sidebar
	{
		TechTree.TechTree techTree;

		SpriteRenderer spriteRenderer, clockRenderer;
		Sprite blank;
		Game game;
		readonly GRegion region;

		Animation clockAnimation = new Animation("clock");

		public GRegion Region { get { return region; } }
		public float Width { get { return spriteWidth * 2; } }

		Dictionary<string, Sprite> sprites = new Dictionary<string,Sprite>();
		const int spriteWidth = 64, spriteHeight = 48;

		List<SidebarItem> items = new List<SidebarItem>();

		public Sidebar( Race race, Renderer renderer, Game game )
		{
			this.techTree = game.LocalPlayer.TechTree;
			this.techTree.BuildableItemsChanged += (sender, e) => { PopulateItemList(); };
			this.game = game;
			region = GRegion.Create(game.viewport, DockStyle.Right, 128, Paint, MouseHandler);
			game.viewport.AddRegion( region );
			spriteRenderer = new SpriteRenderer(renderer, false);
			clockRenderer = new SpriteRenderer(renderer, true);

			LoadSprites("buildings.txt");
			LoadSprites("units.txt");

			blank = SheetBuilder.Add(new Size((int)spriteWidth, (int)spriteHeight), 16);

			clockAnimation.PlayRepeating("idle");
		}

		public void Build(SidebarItem item)
		{
			if (item != null)
				game.controller.orderGenerator = new PlaceBuilding(game.players[1], item.techTreeItem.tag.ToLowerInvariant());
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

		int buildPos = 0;
		int unitPos = 0;

		void PopulateItemList()
		{
			buildPos = 0; unitPos = 0;

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
//			PopulateItemList();	// todo: do this less often, just when things actually change!

			foreach (SidebarItem i in items)
				i.Paint(spriteRenderer, region.Location);

			Fill(region.Size.Y + region.Location.Y, new float2(region.Location.X, buildPos + region.Location.Y));
			Fill(region.Size.Y + region.Location.Y, new float2(region.Location.X + spriteWidth, unitPos + region.Location.Y));

			spriteRenderer.Flush();

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

		void MouseHandler(MouseInput mi)
		{
            if (mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Down)
            {
                var point = new float2(mi.Location.X, mi.Location.Y);
                Build(GetItem(point));
            }
		}
	}

	class PlaceBuilding : IOrderGenerator
	{
		public readonly Player Owner;
		public readonly string Name;

		public PlaceBuilding( Player owner, string name )
		{
			Owner = owner;
			Name = name;
		}

		public Order Order( Game game, int2 xy )
		{
			// todo: check that space is free
			return new PlaceBuildingOrder( this, xy );
		}

		public void PrepareOverlay(Game game, int2 xy)
		{
			game.worldRenderer.uiOverlay.SetCurrentOverlay(false, xy, 2, 3);
		}
	}

	class PlaceBuildingOrder : Order
	{
		PlaceBuilding building;
		int2 xy;

		public PlaceBuildingOrder(PlaceBuilding building, int2 xy)
		{
			this.building = building;
			this.xy = xy;
		}

		public override void Apply(Game game)
		{
			game.world.AddFrameEndTask(_ =>
			{
				Func<int2, Player, Building> newBuilding;
				if (game.buildingCreation.TryGetValue(building.Name, out newBuilding))
				{
					Log.Write("Player \"{0}\" builds {1}", building.Owner.PlayerName, building.Name);
					game.world.Add(newBuilding(xy, building.Owner));
				}
				game.controller.orderGenerator = null;
				game.worldRenderer.uiOverlay.KillOverlay();
			});
		}
	}
}
