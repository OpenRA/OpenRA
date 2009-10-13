using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using OpenRa.TechTree;
using System.Linq;

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

		public GRegion Region { get { return region; } }
		public float Width { get { return spriteWidth * 2; } }

		Dictionary<string, Sprite> sprites = new Dictionary<string,Sprite>();
		const int spriteWidth = 64, spriteHeight = 48;

		static string[] groups = new string[] { "building", "vehicle", "boat", "infantry", "plane" };

		Dictionary<string, string> itemGroups = new Dictionary<string,string>();				//item->group
		Dictionary<string, Animation> clockAnimations = new Dictionary<string,Animation>();		//group->clockAnimation
		Dictionary<string, SidebarItem> selectedItems = new Dictionary<string,SidebarItem>();	//group->selectedItem
		
		List<SidebarItem> items = new List<SidebarItem>();
		
		public Sidebar( Renderer renderer, Game game )
		{
			this.techTree = game.LocalPlayer.TechTree;
            this.techTree.BuildableItemsChanged += PopulateItemList;
			this.game = game;
			region = GRegion.Create(game.viewport, DockStyle.Right, 128, Paint, MouseHandler);
			game.viewport.AddRegion( region );
			spriteRenderer = new SpriteRenderer(renderer, false);
			clockRenderer = new SpriteRenderer(renderer, true);

			LoadSprites("buildings.txt");
			LoadSprites("vehicles.txt");
			LoadSprites("infantry.txt");

			foreach (string s in groups)
			{
				clockAnimations.Add(s, new Animation("clock"));
				clockAnimations[s].PlayRepeating("idle");
				selectedItems.Add(s, null);
			}

			blank = SheetBuilder.Add(new Size((int)spriteWidth, (int)spriteHeight), 16);
		}

		public void Build(SidebarItem item)
		{
			if (item != null)
				game.controller.orderGenerator = new PlaceBuilding(game.LocalPlayer, item.techTreeItem.tag.ToLowerInvariant());
		}

		void LoadSprites(string filename)
		{
			foreach (string line in Util.ReadAllLines(FileSystem.Open(filename)))
			{
				string key = line.Substring(0, line.IndexOf(','));
				int secondComma = line.IndexOf(',', line.IndexOf(',') + 1);
				string group = line.Substring(secondComma + 1, line.Length - secondComma - 1);
				sprites.Add( key, SpriteSheetBuilder.LoadSprite( key + "icon", ".shp" ) );
				itemGroups.Add(key, group);
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

			foreach (string g in groups) selectedItems[g] = null;
		}

		void Paint()
		{
			foreach (SidebarItem i in items)
				i.Paint(spriteRenderer, region.Location);

			Fill(region.Size.Y + region.Location.Y, new float2(region.Location.X, buildPos + region.Location.Y));
			Fill(region.Size.Y + region.Location.Y, new float2(region.Location.X + spriteWidth, unitPos + region.Location.Y));

			spriteRenderer.Flush();

			foreach (var kvp in selectedItems)
			{
				if (kvp.Value != null)
				{
					clockRenderer.DrawSprite(clockAnimations[kvp.Key].Image, region.Location.ToFloat2() + kvp.Value.location, 0);
					clockAnimations[kvp.Key].Tick(1);
				}
			}
			
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
				var point = mi.Location.ToFloat2();
				var item = GetItem(point);
				if (item != null)
				{
					string group = itemGroups[item.techTreeItem.tag];
					if (selectedItems[group] == null)
					{
						selectedItems[group] = item;
						Build(item);
					}
				}
            }
			else if( mi.Button == MouseButtons.Right && mi.Event == MouseInputEvent.Down )
			{
				var point = mi.Location.ToFloat2();
				var item = GetItem(point);
				if( item != null )
				{
					string group = itemGroups[ item.techTreeItem.tag ];
					selectedItems[ group ] = null;
				}
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

		public IEnumerable<Order> Order( Game game, int2 xy )
		{
			// todo: check that space is free
			yield return new PlaceBuildingOrder( this, xy );
		}

		public void PrepareOverlay(Game game, int2 xy)
		{
			game.worldRenderer.uiOverlay.SetCurrentOverlay(xy, Name);
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

		public override void Apply(Game game, bool leftMouseButton)
		{
			if (leftMouseButton)
			{
				game.world.AddFrameEndTask(_ =>
				{
					Log.Write("Player \"{0}\" builds {1}", building.Owner.PlayerName, building.Name);
					game.world.Add(new Actor(building.Name, xy, building.Owner));

					game.controller.orderGenerator = null;
					game.worldRenderer.uiOverlay.KillOverlay();
				});
			}
			else
			{
				game.world.AddFrameEndTask(_ =>
				{
					game.controller.orderGenerator = null;
					game.worldRenderer.uiOverlay.KillOverlay();
				});
			}
		}
	}
}
