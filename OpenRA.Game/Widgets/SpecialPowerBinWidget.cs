#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using OpenRA;
using OpenRA.Traits;
using OpenRA.Graphics;
using OpenRA.FileFormats;
using System;
namespace OpenRA.Widgets
{
	class SpecialPowerBinWidget : Widget
	{
		static Dictionary<string, Sprite> spsprites;
		Animation ready;
		Animation clock;
		readonly List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle,Action<MouseInput>>>();
		string chromeCollection;
		
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
		
		public override bool HandleInput(MouseInput mi)
		{
			// Are we able to handle this event?
			if (!Visible || !GetEventBounds().Contains(mi.Location.X,mi.Location.Y))
				return base.HandleInput(mi);
			
			if (base.HandleInput(mi))
				return true;
			
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
		
		public override void Draw(World world)
		{		
			if (!Visible)
			{
				base.Draw(world);
				return;
			}
			buttons.Clear();
			
			var powers = world.LocalPlayer.PlayerActor.traits.WithInterface<SupportPower>();
			var numPowers = powers.Count(p => p.IsAvailable);
			if (numPowers == 0) return;
			
			SupportPower tooltipItem = null;
			int2 tooltipPos = int2.Zero;											

			WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world, "specialbin-top"),new float2(Bounds.X,Bounds.Y));
			for (var i = 1; i < numPowers; i++)
				WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world,"specialbin-middle"), new float2(Bounds.X, Bounds.Y + i * 51));
			WidgetUtils.DrawRGBA(WidgetUtils.GetChromeImage(world,"specialbin-bottom"), new float2(Bounds.X, Bounds.Y + numPowers * 51));
			Game.chrome.rgbaRenderer.Flush();
			
			// Hack Hack Hack
			Bounds.Width = 69;
			Bounds.Height = 10 + numPowers * 51 + 21;
			
			var y = Bounds.Y + 10;
			foreach (var sp in powers)
			{
				var image = spsprites[sp.Info.Image];
				if (sp.IsAvailable)
				{
					var drawPos = new float2(Bounds.X + 5, y);
					var rect = new Rectangle(Bounds.X + 5, y, 64, 48);

					if (rect.Contains(Game.chrome.lastMousePos.ToPoint()))
					{
						var pos = drawPos.ToInt2();
						var tooltipBounds = new Rectangle(pos.X-3,pos.Y-3,350,54);
						WidgetUtils.DrawPanel("dialog4", tooltipBounds, null);
						
						pos += new int2(70, 5);
						Game.chrome.renderer.BoldFont.DrawText(Game.chrome.rgbaRenderer, sp.Info.Description, pos, Color.White);
			
						var timer = "Charge Time: {0}".F(FormatTime(sp.RemainingTime));
						Game.chrome.renderer.BoldFont.DrawText(Game.chrome.rgbaRenderer, timer, drawPos.ToInt2() + new int2((int)tooltipBounds.Width - Game.chrome.renderer.BoldFont.Measure(timer).X - 10, 0), Color.White);
			
						if (sp.Info.LongDesc != null)
						{
							pos += new int2(0, 25);
							Game.chrome.renderer.RegularFont.DrawText(Game.chrome.rgbaRenderer, sp.Info.LongDesc.Replace("\\n", "\n"), pos, Color.White);
						}
			
						Game.chrome.rgbaRenderer.Flush();
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
			Game.chrome.shpRenderer.Flush();
			base.Draw(world);
		}
		
		Action<MouseInput> HandleSupportPower(SupportPower sp)
		{
			return mi => { if (mi.Button == MouseButton.Left) sp.Activate(); };
		}
				
		string FormatTime(int ticks)
		{
			var seconds = ticks / 25;
			var minutes = seconds / 60;

			return "{0:D2}:{1:D2}".F(minutes, seconds % 60);
		}
	}
}