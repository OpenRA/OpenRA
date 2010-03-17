using OpenRA.Graphics;
using System.Drawing;
using System.Collections.Generic;

namespace OpenRA.Widgets
{
	class ButtonWidget : Widget
	{
		public readonly string Text = "";
		public bool Depressed = false;
		public int VisualHeight = 1;
		public override bool HandleInput(MouseInput mi)
		{
			if (Game.chrome.selectedWidget == this)
				Depressed = (GetEventBounds().Contains(mi.Location.X,mi.Location.Y)) ? true : false;
			
			// Relinquish focus
			if (Game.chrome.selectedWidget == this && mi.Event == MouseInputEvent.Up)
			{
				Game.chrome.selectedWidget = null;
				Depressed = false;
			}
			
			// Are we able to handle this event?
			if (!Visible || !GetEventBounds().Contains(mi.Location.X,mi.Location.Y))
				return base.HandleInput(mi);
			
			// Give button focus only while the mouse is down
			// This is a bit of a hack: it will become cleaner soonish
			// It will also steal events from any potential children
			// We also want to play a click sound
			if (mi.Event == MouseInputEvent.Down)
			{
				Game.chrome.selectedWidget = this;
				Depressed = true;
				return true;
			}
			
			return base.HandleInput(mi);
		}
		
		public override void Draw()
		{
			if (!Visible)
			{
				base.Draw();
				return;
			}
		
			string collection = (Depressed) ? "dialog3" : "dialog2";
			int2 stateOffset = (Depressed) ? new int2(VisualHeight,VisualHeight) : new int2(0,0);
			Game.chrome.renderer.Device.EnableScissor(Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height);
			
			string[] images = { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
			var ss = Graphics.Util.MakeArray(9, n => ChromeProvider.GetImage(Game.chrome.renderer, collection,images[n]));
			
			for( var x = Bounds.Left + (int)ss[2].size.X; x < Bounds.Right - (int)ss[3].size.X; x += (int)ss[8].size.X )
				for( var y = Bounds.Top + (int)ss[0].size.Y; y < Bounds.Bottom - (int)ss[1].size.Y; y += (int)ss[8].size.Y )
					Game.chrome.rgbaRenderer.DrawSprite(ss[8], new float2(x, y), "chrome");

			//draw borders
			for (var y = Bounds.Top + (int)ss[0].size.Y; y < Bounds.Bottom - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
			{
				Game.chrome.rgbaRenderer.DrawSprite(ss[2], new float2(Bounds.Left, y), "chrome");
				Game.chrome.rgbaRenderer.DrawSprite(ss[3], new float2(Bounds.Right - ss[3].size.X, y), "chrome");
			}

			for (var x = Bounds.Left + (int)ss[2].size.X; x < Bounds.Right - (int)ss[3].size.X; x += (int)ss[0].size.X)
			{
				Game.chrome.rgbaRenderer.DrawSprite(ss[0], new float2(x, Bounds.Top), "chrome");
				Game.chrome.rgbaRenderer.DrawSprite(ss[1], new float2(x, Bounds.Bottom - ss[1].size.Y), "chrome");
			}

			Game.chrome.rgbaRenderer.DrawSprite(ss[4], new float2(Bounds.Left, Bounds.Top), "chrome");
			Game.chrome.rgbaRenderer.DrawSprite(ss[5], new float2(Bounds.Right - ss[5].size.X, Bounds.Top), "chrome");
			Game.chrome.rgbaRenderer.DrawSprite(ss[6], new float2(Bounds.Left, Bounds.Bottom - ss[6].size.Y), "chrome");
			Game.chrome.rgbaRenderer.DrawSprite(ss[7], new float2(Bounds.Right - ss[7].size.X, Bounds.Bottom - ss[7].size.Y), "chrome");
			Game.chrome.rgbaRenderer.Flush();
			
			Game.chrome.renderer.BoldFont.DrawText(Game.chrome.rgbaRenderer, Text, new int2(Bounds.X+Bounds.Width/2, Bounds.Y+Bounds.Height/2) - new int2(Game.chrome.renderer.BoldFont.Measure(Text).X / 2, Game.chrome.renderer.BoldFont.Measure(Text).Y/2) + stateOffset, Color.White);
			
			Game.chrome.renderer.Device.DisableScissor();
			
			base.Draw();
		}
	}
}