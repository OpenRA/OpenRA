using OpenRA.Graphics;
using System.Drawing;
using System.Collections.Generic;

namespace OpenRA.Widgets
{
	class BackgroundWidget : Widget
	{
		public override void Draw()
		{
			if (!Visible)
			{
				base.Draw();
				return;
			}
		
			string collection = "dialog";
			
			Rectangle r = Bounds;
			Game.chrome.renderer.Device.EnableScissor(r.Left, r.Top, r.Width, r.Height);
			
			string[] images = { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
			var ss = Graphics.Util.MakeArray(9, n => ChromeProvider.GetImage(Game.chrome.renderer, collection,images[n]));
			
			for( var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[8].size.X )
				for( var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[8].size.Y )
					Game.chrome.rgbaRenderer.DrawSprite(ss[8], new float2(x, y), "chrome");

			//draw borders
			for (var y = r.Top + (int)ss[0].size.Y; y < r.Bottom - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
			{
				Game.chrome.rgbaRenderer.DrawSprite(ss[2], new float2(r.Left, y), "chrome");
				Game.chrome.rgbaRenderer.DrawSprite(ss[3], new float2(r.Right - ss[3].size.X, y), "chrome");
			}

			for (var x = r.Left + (int)ss[2].size.X; x < r.Right - (int)ss[3].size.X; x += (int)ss[0].size.X)
			{
				Game.chrome.rgbaRenderer.DrawSprite(ss[0], new float2(x, r.Top), "chrome");
				Game.chrome.rgbaRenderer.DrawSprite(ss[1], new float2(x, r.Bottom - ss[1].size.Y), "chrome");
			}

			Game.chrome.rgbaRenderer.DrawSprite(ss[4], new float2(r.Left, r.Top), "chrome");
			Game.chrome.rgbaRenderer.DrawSprite(ss[5], new float2(r.Right - ss[5].size.X, r.Top), "chrome");
			Game.chrome.rgbaRenderer.DrawSprite(ss[6], new float2(r.Left, r.Bottom - ss[6].size.Y), "chrome");
			Game.chrome.rgbaRenderer.DrawSprite(ss[7], new float2(r.Right - ss[7].size.X, r.Bottom - ss[7].size.Y), "chrome");
			Game.chrome.rgbaRenderer.Flush();
			
			Game.chrome.renderer.Device.DisableScissor();
		
			
			base.Draw();
		}
	}
}