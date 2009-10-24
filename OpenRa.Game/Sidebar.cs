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
		
		public Sidebar( Renderer renderer )
		{
			this.techTree = Game.LocalPlayer.TechTree;
            this.techTree.BuildableItemsChanged += PopulateItemList;
			region = GRegion.Create(Game.viewport, DockStyle.Right, 128, Paint, MouseHandler);
			Game.viewport.AddRegion( region );
			spriteRenderer = new SpriteRenderer(renderer, false);
			clockRenderer = new SpriteRenderer(renderer, true);

			LoadSprites( "BuildingTypes", "building" );
			LoadSprites( "VehicleTypes", "vehicle" );
			LoadSprites( "InfantryTypes", "infantry" );
			LoadSprites( "ShipTypes", "boat" );
			LoadSprites( "PlaneTypes", "plane" );

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
			{
				if (item.techTreeItem.IsStructure)
					Game.controller.orderGenerator = new PlaceBuilding(Game.LocalPlayer,
						item.techTreeItem.tag.ToLowerInvariant());
				else
					Game.BuildUnit(Game.LocalPlayer, item.techTreeItem.tag.ToLowerInvariant());
			}
		}

		void LoadSprites( string category, string group )
		{
			foreach( var u in Rules.AllRules.GetSection( category ) )
			{
				var unit = Rules.UnitInfo[ u.Key ];

				if( unit.TechLevel != -1 )
					sprites.Add( unit.Name, SpriteSheetBuilder.LoadSprite( unit.Name + "icon", ".shp" ) );
				itemGroups.Add( unit.Name, group );
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
}
