using OpenRA.Graphics;
using System.Drawing;
using System.Collections.Generic;

namespace OpenRA.Widgets
{
	class LabelWidget : Widget
	{
		public readonly string Text = null;
		public readonly string Align = "Left";
		
		public override void Draw(SpriteRenderer rgbaRenderer, Renderer renderer)
		{		
			if (Visible)
			{
				Rectangle r = Bounds;
				renderer.Device.EnableScissor(r.Left, r.Top, r.Width, r.Height);
				
				int2 bounds = renderer.BoldFont.Measure(Text);
				int2 position = new int2(X,Y);
				
				if (Align == "Center")
					position = new int2(X+Width/2, Y+Height/2) - new int2(bounds.X / 2, bounds.Y/2);
				
				
				renderer.BoldFont.DrawText(rgbaRenderer, Text, position, Color.White);
				renderer.Device.DisableScissor();
			}
			base.Draw(rgbaRenderer,renderer);
		}
	}
}