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
			GetDescription = () => Description;
			GetLongDesc = () => LongDesc;
		}
		
		public override void DrawInner (World world)
		{
			var image = ChromeProvider.GetImage(Image + "-button", GetImage());
			var rect = new Rectangle(RenderBounds.X, RenderBounds.Y, (int)image.size.X, (int)image.size.Y);
			
			if (rect.Contains(Viewport.LastMousePos.ToPoint()))
			{
					rect = rect.InflateBy(3, 3, 3, 3);
					var pos = new int2(rect.Left, rect.Top);
					var m = pos + new int2(rect.Width, rect.Height);
					var br = pos + new int2(rect.Width, rect.Height + 20);

					var u = Game.Renderer.RegularFont.Measure(GetLongDesc().Replace("\\n", "\n"));

					br.X -= u.X;
					br.Y += u.Y;
					br += new int2(-15, 25);

					var border = WidgetUtils.GetBorderSizes("dialog4");

					WidgetUtils.DrawPanelPartial("dialog4", rect
						.InflateBy(0, 0, 0, border[1]),
						PanelSides.Top | PanelSides.Left | PanelSides.Right);

					WidgetUtils.DrawPanelPartial("dialog4", new Rectangle(br.X, m.Y, pos.X - br.X, br.Y - m.Y)
						.InflateBy(0, 0, border[3], 0),
						PanelSides.Top | PanelSides.Left | PanelSides.Bottom);

					WidgetUtils.DrawPanelPartial("dialog4", new Rectangle(pos.X, m.Y, m.X - pos.X, br.Y - m.Y)
						.InflateBy(border[2], border[0], 0, 0),
						PanelSides.Right | PanelSides.Bottom);

					pos.X = br.X + 8;
					pos.Y = m.Y + 8;
					Game.Renderer.BoldFont.DrawText(GetDescription(), pos, Color.White);

					pos += new int2(0, 20);
					Game.Renderer.RegularFont.DrawText(GetLongDesc().Replace("\\n", "\n"), pos, Color.White);
			}
			
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(image, new int2(RenderBounds.X, RenderBounds.Y));
		}
	}
}

