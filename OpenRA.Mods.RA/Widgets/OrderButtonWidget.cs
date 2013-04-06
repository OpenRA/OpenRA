#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets
{
	public class OrderButtonWidget : ButtonWidget
	{
		public Func<bool> Enabled = () => true;
		public Func<bool> Pressed = () => false;

		public string Image, Description, LongDesc = "";

		public Func<string> GetImage, GetDescription, GetLongDesc;

		public OrderButtonWidget()
		{
			GetImage = () => Enabled() ? Pressed() ? "pressed" : "normal" : "disabled";
			GetDescription = () => Key != null ? "{0} ({1})".F(Description, Key.ToUpper()) : Description;
			GetLongDesc = () => LongDesc;
		}

		public override void Draw()
		{
			var image = ChromeProvider.GetImage(Image + "-button", GetImage());
			var rect = new Rectangle(RenderBounds.X, RenderBounds.Y, (int)image.size.X, (int)image.size.Y);

			if (rect.Contains(Viewport.LastMousePos))
			{
					rect = rect.InflateBy(3, 3, 3, 3);
					var pos = new int2(rect.Left, rect.Top);
					var m = pos + new int2(rect.Width, rect.Height);
					var br = pos + new int2(rect.Width, rect.Height + 20);

					var u = Game.Renderer.Fonts["Regular"].Measure(GetLongDesc().Replace("\\n", "\n"));

					br.X -= u.X;
					br.Y += u.Y;
					br += new int2(-15, 25);

					var border = WidgetUtils.GetBorderSizes("dialog4");

					WidgetUtils.DrawPanelPartial("dialog4", rect
						.InflateBy(0, 0, 0, border[1]),
						PanelSides.Top | PanelSides.Left | PanelSides.Right | PanelSides.Center);

					WidgetUtils.DrawPanelPartial("dialog4", new Rectangle(br.X, m.Y, pos.X - br.X, br.Y - m.Y)
						.InflateBy(0, 0, border[3], 0),
						PanelSides.Top | PanelSides.Left | PanelSides.Bottom | PanelSides.Center);

					WidgetUtils.DrawPanelPartial("dialog4", new Rectangle(pos.X, m.Y, m.X - pos.X, br.Y - m.Y)
						.InflateBy(border[2], border[0], 0, 0),
						PanelSides.Right | PanelSides.Bottom | PanelSides.Center);

					pos.X = br.X + 8;
					pos.Y = m.Y + 8;
					Game.Renderer.Fonts["Bold"].DrawText(GetDescription(), pos, Color.White);

					pos += new int2(0, 20);
					Game.Renderer.Fonts["Regular"].DrawText(GetLongDesc().Replace("\\n", "\n"), pos, Color.White);
			}

			Game.Renderer.RgbaSpriteRenderer.DrawSprite(image, new int2(RenderBounds.X, RenderBounds.Y));
		}
	}
}

