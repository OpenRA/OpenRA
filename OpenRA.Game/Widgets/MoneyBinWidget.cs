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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Widgets
{
	class MoneyBinWidget : Widget
	{
		const int chromeButtonGap = 2;

		/* legacy crap!!! */
		List<Pair<Rectangle, Action<MouseInput>>> buttons = new List<Pair<Rectangle, Action<MouseInput>>>();
		void AddButton(Rectangle r, Action<MouseInput> b) { buttons.Add(Pair.New(r, b)); }

		public override void Draw(World world)
		{
			var digitCollection = "digits-" + world.LocalPlayer.Country.Race;
			var chromeCollection = "chrome-" + world.LocalPlayer.Country.Race;

			Game.chrome.renderer.RgbaSpriteRenderer.DrawSprite(
				ChromeProvider.GetImage(Game.chrome.renderer, chromeCollection, "moneybin"),
				new float2(Bounds.Left, 0), "chrome");

			var moneyDigits = world.LocalPlayer.DisplayCash.ToString();
			var x = Bounds.Right - 65;

			foreach (var d in moneyDigits.Reverse())
			{
				Game.chrome.renderer.RgbaSpriteRenderer.DrawSprite(
					ChromeProvider.GetImage(Game.chrome.renderer, digitCollection, (d - '0').ToString()),
					new float2(x, 6), "chrome");
				x -= 14;
			}

			var origin = new int2(Game.viewport.Width - 200, 2);

			foreach (var cb in world.WorldActor.traits.WithInterface<IChromeButton>())
			{
				var state = cb.Enabled ? cb.Pressed ? "pressed" : "normal" : "disabled";
				var image = ChromeProvider.GetImage(Game.chrome.renderer, cb.Image + "-button", state);

				origin.X -= (int)image.size.X + chromeButtonGap;

				var button = cb;
				var rect = new Rectangle(origin.X, origin.Y, (int)image.size.X, (int)image.size.Y);
				AddButton(rect, _ => { if (button.Enabled) button.OnClick(); });

				if (rect.Contains(Game.chrome.lastMousePos.ToPoint()))
				{
					rect = rect.InflateBy(3, 3, 3, 3);
					var pos = new int2(rect.Left, rect.Top);
					var m = pos + new int2(rect.Width, rect.Height);
					var br = pos + new int2(rect.Width, rect.Height + 20);

					var u = Game.chrome.renderer.RegularFont.Measure(cb.LongDesc.Replace("\\n", "\n"));

					br.X -= u.X;
					br.Y += u.Y;
					br += new int2(-15, 25);

					var images = new[] { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
					var ss = images.Select(i => ChromeProvider.GetImage(Game.chrome.renderer, "dialog4", i)).ToArray();
					
					WidgetUtils.DrawPanelPartial("dialog4", rect
						.InflateBy(0, 0, 0, (int)ss[1].size.Y), 
						PanelSides.Top | PanelSides.Left | PanelSides.Right);

					WidgetUtils.DrawPanelPartial("dialog4", new Rectangle(br.X, m.Y, pos.X - br.X, br.Y - m.Y)
						.InflateBy(0, 0, (int)ss[3].size.X, 0),
						PanelSides.Top | PanelSides.Left | PanelSides.Bottom);

					WidgetUtils.DrawPanelPartial("dialog4", new Rectangle(pos.X, m.Y, m.X - pos.X, br.Y - m.Y)
						.InflateBy((int)ss[2].size.X, (int)ss[0].size.Y, 0, 0),
						PanelSides.Right | PanelSides.Bottom);

					pos.X = br.X + 8;
					pos.Y = m.Y + 8;
					Game.chrome.renderer.BoldFont.DrawText(cb.Description, pos, Color.White);

					if (cb.LongDesc != null)
					{
						pos += new int2(0, 20);
						Game.chrome.renderer.RegularFont.DrawText(cb.LongDesc.Replace("\\n", "\n"), pos, Color.White);
					}
				}

				Game.chrome.renderer.RgbaSpriteRenderer.DrawSprite(image, origin, "chrome");
			}

			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
		}

		public override bool HandleInput(MouseInput mi)
		{
			// Are we able to handle this event?
			if (!Visible || !GetEventBounds().Contains(mi.Location.X, mi.Location.Y))
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
	}
}
