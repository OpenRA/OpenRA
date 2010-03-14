using OpenRA.Graphics;
using System.Drawing;
using System.Collections.Generic;

namespace OpenRA.Widgets
{
	class ButtonWidget : Widget
	{
		public readonly string Text = null;
		
		public override bool HandleInput(MouseInput mi)
		{
			// TEMPORARY: Define a default mouse button event - quit the game
			if (mi.Event == MouseInputEvent.Down && ClickRect.Contains(mi.Location.X,mi.Location.Y))
			{
				Game.Exit();
				return true;
			}
			
			return base.HandleInput(mi);
		}
		
		public override void Draw(SpriteRenderer rgbaRenderer, Renderer renderer)
		{
			string collection = "dialog2";
			
			Rectangle r = Bounds;
			renderer.Device.EnableScissor(r.Left, r.Top, r.Width, r.Height);
			
			string[] images = { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
			var ss = Graphics.Util.MakeArray(9, n => ChromeProvider.GetImage(renderer, collection,images[n]));
			
			for( var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[8].size.X )
				for( var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[8].size.Y )
					rgbaRenderer.DrawSprite(ss[8], new float2(x, y), "chrome");

			//draw borders
			for (var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
			{
				rgbaRenderer.DrawSprite(ss[2], new float2(r.Left, y), "chrome");
				rgbaRenderer.DrawSprite(ss[3], new float2(r.Right - ss[3].size.X, y), "chrome");
			}

			for (var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[0].size.X)
			{
				rgbaRenderer.DrawSprite(ss[0], new float2(x, r.Top), "chrome");
				rgbaRenderer.DrawSprite(ss[1], new float2(x, r.Bottom - ss[1].size.Y), "chrome");
			}

			rgbaRenderer.DrawSprite(ss[4], new float2(r.Left, r.Top), "chrome");
			rgbaRenderer.DrawSprite(ss[5], new float2(r.Right - ss[5].size.X, r.Top), "chrome");
			rgbaRenderer.DrawSprite(ss[6], new float2(r.Left, r.Bottom - ss[6].size.Y), "chrome");
			rgbaRenderer.DrawSprite(ss[7], new float2(r.Right - ss[7].size.X, r.Bottom - ss[7].size.Y), "chrome");
			rgbaRenderer.Flush();
			
			renderer.BoldFont.DrawText(rgbaRenderer, Text, new int2(X+Width/2, Y+Height/2) - new int2(renderer.BoldFont.Measure(Text).X / 2, renderer.BoldFont.Measure(Text).Y/2), Color.White);
			
			renderer.Device.DisableScissor();
		
			base.Draw(rgbaRenderer,renderer);
		}
	}
}