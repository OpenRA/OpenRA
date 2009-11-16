using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenRa.FileFormats;
using OpenRa.Game.Graphics;
using System.Linq;

namespace OpenRa.Game
{
	using GRegion = OpenRa.Game.Graphics.Region;

	class Sidebar
	{
		Player player;

		SpriteRenderer spriteRenderer, clockRenderer;
		Renderer renderer;
		Sprite blank;
		Animation ready;
		Animation cantBuild;
		readonly GRegion region;
		public float Width { get { return spriteWidth * 2; } }

		Dictionary<string, Sprite> sprites = new Dictionary<string,Sprite>();
		const int spriteWidth = 64, spriteHeight = 48;

		static string[] groups = new string[] { "Building", "Defense", "Vehicle", "Ship", "Infantry", "Plane" };

		Dictionary<string, Animation> clockAnimations = new Dictionary<string,Animation>();		//group->clockAnimation
		
		List<SidebarItem> items = new List<SidebarItem>();
		
		public Sidebar( Renderer renderer, Player player )
		{
			this.player = player;
			this.renderer = renderer;
			region = GRegion.Create(Game.viewport, DockStyle.Right, /*128*/0, Paint, MouseHandler);
			region.UseScissor = false;
			region.AlwaysWantMovement = true;
			Game.viewport.AddRegion( region );
			spriteRenderer = new SpriteRenderer(renderer, false);
			clockRenderer = new SpriteRenderer(renderer, true);

			for( int i = 0 ; i < groups.Length ; i++ )
				LoadSprites( groups[ i ] );

			foreach (string group in groups)
			{
				clockAnimations.Add( group, new Animation( "clock" ) );
				clockAnimations[ group ].PlayFetchIndex( "idle", ClockAnimFrame( group ) );
			}

			blank = SheetBuilder.Add(new Size((int)spriteWidth, (int)spriteHeight), 16);
			ready = new Animation("pips");
			ready.PlayRepeating("ready");

			cantBuild = new Animation("clock");
			cantBuild.PlayFetchIndex("idle", () => 0);
		}

		const int NumClockFrames = 54;
		Func<int> ClockAnimFrame( string group )
		{
			return () =>
				{
					var producing = player.Producing( group );
					if( producing == null ) return 0;
					return ( producing.TotalTime - producing.RemainingTime ) * NumClockFrames / producing.TotalTime;
				};
		}

		public void Build( SidebarItem item )
		{
			if( item == null ) return;

			if( item.IsStructure )
				Game.controller.orderGenerator = new PlaceBuilding( player,
					item.Tag.ToLowerInvariant() );
		}

		void LoadSprites( string group )
		{
			foreach( var u in Rules.Categories[ group ] )
			{
				var unit = Rules.UnitInfo[ u ];

				if( unit.TechLevel != -1 )
					sprites.Add( unit.Name, SpriteSheetBuilder.LoadSprite( unit.Name + "icon", ".shp" ) );
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

			foreach (var i in Rules.TechTree.BuildableItems(player, "Building"))
			{
				Sprite sprite;
				if (!sprites.TryGetValue(i, out sprite)) continue;
				items.Add(new SidebarItem(sprite, i, true, buildPos));
				buildPos += spriteHeight;
			}

			foreach( var i in Rules.TechTree.BuildableItems( player, "Vehicle", "Infantry", "Ship", "Plane" ) )
			{
				Sprite sprite;
				if( !sprites.TryGetValue( i, out sprite ) ) continue;
				items.Add( new SidebarItem( sprite, i, false, unitPos ) );
				unitPos += spriteHeight;
			}
		}

		void Paint()
		{
			PopulateItemList();

			foreach( var i in items )			/* draw the buttons */
				i.Paint(spriteRenderer, region.Location);
			spriteRenderer.Flush();

			foreach( var i in items )			/* draw the status overlays */
			{
				var group = Rules.UnitCategory[ i.Tag ];
				var producing = player.Producing( group );
				if( producing != null && producing.Item == i.Tag )
				{
					clockAnimations[ group ].Tick();
					clockRenderer.DrawSprite( clockAnimations[ group ].Image, region.Location.ToFloat2() + i.location, 0 );

					if (producing.Done)
					{
						ready.Play("ready");
						clockRenderer.DrawSprite(ready.Image, region.Location.ToFloat2() + i.location + new float2((64 - ready.Image.size.X) / 2, 2), 0);
					}
					else if (producing.Paused)
					{
						ready.Play("hold");
						clockRenderer.DrawSprite(ready.Image, region.Location.ToFloat2() + i.location + new float2((64 - ready.Image.size.X) / 2, 2), 0);
					}
				}
				else if (producing != null)
					clockRenderer.DrawSprite(cantBuild.Image, region.Location.ToFloat2() + i.location, 0);
			}

			Fill(region.Size.Y + region.Location.Y, new float2(region.Location.X, buildPos + region.Location.Y));
			Fill(region.Size.Y + region.Location.Y, new float2(region.Location.X + spriteWidth, unitPos + region.Location.Y));

			spriteRenderer.Flush();
			clockRenderer.Flush();

			if (mouseOverItem != null)
			{
				/* draw the sidebar help for this item */
				/* todo: draw a solid background of the appropriate color */
				var ui = Rules.UnitInfo[mouseOverItem.Tag];
				var text = string.Format(ui.Cost > 0 ? "{0} ($ {1})" : "{0}",	/* abilities! */
					ui.Description, ui.Cost);

				var size = renderer.MeasureText(text);

				var pos = region.Position + mouseOverItem.location.ToInt2() -new int2(size.X+ 10, 0);
				renderer.DrawText( text, pos + new int2(0,-1), Color.Black );
				renderer.DrawText(text, pos + new int2(0, 1), Color.Black);
				renderer.DrawText(text, pos + new int2(1, 0), Color.Black);
				renderer.DrawText(text, pos + new int2(-1, 0), Color.Black);
				renderer.DrawText(text, pos , Color.White);
			}
		}

		public SidebarItem GetItem(float2 point)
		{
			foreach (SidebarItem i in items)
				if (i.Clicked(point))
					return i;

			return null;
		}

		SidebarItem mouseOverItem;

		void MouseHandler(MouseInput mi)
		{
			var point = mi.Location.ToFloat2();
			var item = GetItem( point );
			mouseOverItem = item;
			if( item == null )
				return;

			string group = Rules.UnitCategory[item.Tag];
			var producing = player.Producing(group);

			if( mi.Button == MouseButtons.Left && mi.Event == MouseInputEvent.Down )
            {
				if (producing == null)
				{
					Game.controller.AddOrder( Order.StartProduction( player, item.Tag ) );
					Game.PlaySound("abldgin1.aud", false);
				}
				else if (producing.Item == item.Tag)
				{
					if (producing.Done)
						Build(item);
					else
						Game.controller.AddOrder( Order.PauseProduction( player, item.Tag, false ) );
				}
				else
				{
					Game.PlaySound("progres1.aud", false);
				}
            }
			else if (mi.Button == MouseButtons.Right && mi.Event == MouseInputEvent.Down)
			{
				if( producing == null ) return;
				if (item.Tag != producing.Item) return;

				if (producing.Paused || producing.Done)
				{
					Game.PlaySound("cancld1.aud", false);
					Game.controller.AddOrder( Order.CancelProduction( player, item.Tag ) );
				}
				else
				{
					Game.PlaySound("onhold1.aud", false);
					Game.controller.AddOrder( Order.PauseProduction( player, item.Tag, true ) );
				}
			}
		}
	}
}
