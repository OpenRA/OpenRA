using OpenRA.Graphics;
using System.Drawing;
using System.Collections.Generic;

namespace OpenRA.Widgets
{
	class LabelWidget : Widget
	{
		public string Text = "";
		public string Align = "Left";
		
		public override void Draw()
		{		
			if (!Visible)
			{
				base.Draw();
				return;
			}
		
			Rectangle r = Bounds;
			Game.chrome.renderer.Device.EnableScissor(r.Left, r.Top, r.Width, r.Height);
			
			int2 bounds = Game.chrome.renderer.BoldFont.Measure(Text);
			int2 position = new int2(Bounds.X,Bounds.Y);
			
			if (Align == "Center")
				position = new int2(Bounds.X+Bounds.Width/2, Bounds.Y+Bounds.Height/2) - new int2(bounds.X / 2, bounds.Y/2);
			
			
			Game.chrome.renderer.BoldFont.DrawText(Game.chrome.rgbaRenderer, Text, position, Color.White);
			Game.chrome.renderer.Device.DisableScissor();
			base.Draw();
		}
	}
}