#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	class SpecialPowerBinWidget : Widget
	{
		static Dictionary<string, Sprite> spsprites;
		Animation ready;
		Animation clock;
		readonly List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle,Action<MouseInput>>>();
		
		public SpecialPowerBinWidget() : base() { }
		
		public override void Initialize()
		{
			base.Initialize();

			if (spsprites == null)
				spsprites = Rules.Info.Values.SelectMany( u => u.Traits.WithInterface<SupportPowerInfo>() )
					.ToDictionary(
						u => u.Image,
						u => SpriteSheetBuilder.LoadAllSprites(u.Image)[0]);	
			
			ready = new Animation("pips");
			ready.PlayRepeating("ready");
			clock = new Animation("clock");
		}

		public override Rectangle EventBounds
		{
			get { return buttons.Any() ? buttons.Select(b => b.First).Aggregate(Rectangle.Union) : Bounds; }
		}
		
		public override bool HandleInputInner(MouseInput mi)
		{			
			if (mi.Event == MouseInputEvent.Down)
			{
				var action = buttons.Where(a => a.First.Contains(mi.Location.ToPoint()))
				.Select(a => a.Second).FirstOrDefault();
				if (action == null)
					return false;
		
				action(mi);
				return true;
			}
			
			return false;
		}		
		
		public override void DrawInner(World world)
		{		
			buttons.Clear();
			
			var powers = world.LocalPlayer.PlayerActor.traits.WithInterface<SupportPower>();
			var numPowers = powers.Count(p => p.IsAvailable);
			if (numPowers == 0) return;
			var rectBounds = RenderBounds;
			WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world, "specialbin-top"),new float2(rectBounds.X,rectBounds.Y));
			for (var i = 1; i < numPowers; i++)
				WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world,"specialbin-middle"), new float2(rectBounds.X, rectBounds.Y + i * 51));
			WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world,"specialbin-bottom"), new float2(rectBounds.X, rectBounds.Y + numPowers * 51));

			Game.Renderer.RgbaSpriteRenderer.Flush();
			
			// Hack Hack Hack
			rectBounds.Width = 69;
			rectBounds.Height = 10 + numPowers * 51 + 21;
			
			var y = rectBounds.Y + 10;
			foreach (var sp in powers)
			{
				var image = spsprites[sp.Info.Image];
				if (sp.IsAvailable)
				{
					var drawPos = new float2(rectBounds.X + 5, y);
					var rect = new Rectangle(rectBounds.X + 5, y, 64, 48);

					if (rect.Contains(Widget.LastMousePos.ToPoint()))
					{
						var pos = drawPos.ToInt2();					
						var tl = new int2(pos.X-3,pos.Y-3);
						var m = new int2(pos.X+64+3,pos.Y+48+3);
						var br = tl + new int2(64+3+20,60);
						
						if (sp.Info.LongDesc != null)
							br += Game.Renderer.RegularFont.Measure(sp.Info.LongDesc.Replace("\\n", "\n"));
						else
							br += new int2(300,0);

						var border = WidgetUtils.GetBorderSizes("dialog4");

						WidgetUtils.DrawPanelPartial("dialog4", Rectangle.FromLTRB(tl.X, tl.Y, m.X + border[3], m.Y),
							PanelSides.Left | PanelSides.Top | PanelSides.Bottom);
						WidgetUtils.DrawPanelPartial("dialog4", Rectangle.FromLTRB(m.X - border[2], tl.Y, br.X, m.Y + border[1]),
							PanelSides.Top | PanelSides.Right);
						WidgetUtils.DrawPanelPartial("dialog4", Rectangle.FromLTRB(m.X, m.Y - border[1], br.X, br.Y),
							PanelSides.Left | PanelSides.Right | PanelSides.Bottom);
						
						pos += new int2(77, 5);
						Game.Renderer.BoldFont.DrawText(sp.Info.Description, pos, Color.White);
						
						pos += new int2(0,20);
						Game.Renderer.BoldFont.DrawText(WorldUtils.FormatTime(sp.RemainingTime).ToString(), pos, Color.White);
						Game.Renderer.BoldFont.DrawText("/ {0}".F(WorldUtils.FormatTime(sp.TotalTime)), pos + new int2(45,0), Color.White);			
						
						if (sp.Info.LongDesc != null)
						{
							pos += new int2(0, 20);
							Game.Renderer.RegularFont.DrawText(sp.Info.LongDesc.Replace("\\n", "\n"), pos, Color.White);
						}
					}

					WidgetUtils.DrawSHP(image, drawPos);

					clock.PlayFetchIndex("idle",
						() => (sp.TotalTime - sp.RemainingTime)
							* (clock.CurrentSequence.Length - 1) / sp.TotalTime);
					clock.Tick();

					WidgetUtils.DrawSHP(clock.Image, drawPos);

					if (sp.IsReady)
					{
						ready.Play("ready");
						WidgetUtils.DrawSHP(ready.Image, drawPos + new float2((64 - ready.Image.size.X) / 2, 2));
					}

					buttons.Add(Pair.New(rect,HandleSupportPower(sp)));

					y += 51;
				}
			}
			Game.Renderer.WorldSpriteRenderer.Flush();
		}
		
		Action<MouseInput> HandleSupportPower(SupportPower sp)
		{
			return mi => { if (mi.Button == MouseButton.Left) sp.Activate(); };
		}
	}
}