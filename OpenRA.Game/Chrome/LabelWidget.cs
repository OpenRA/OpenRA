using OpenRA.Graphics;
using System.Drawing;
using System.Collections.Generic;

namespace OpenRA.Widgets
{
	class LabelWidget : Widget
	{
		public readonly string Text = null;
		
		public override void Draw(SpriteRenderer rgbaRenderer, Renderer renderer)
		{		
			Rectangle r = Bounds;
			renderer.Device.EnableScissor(r.Left, r.Top, r.Width, r.Height);
			renderer.BoldFont.DrawText(rgbaRenderer, Text, new int2(X+Width/2, Y+Height/2) - new int2(renderer.BoldFont.Measure(Text).X / 2, renderer.BoldFont.Measure(Text).Y/2), Color.White);
			renderer.Device.DisableScissor();
			base.Draw(rgbaRenderer,renderer);
		}
	}
}