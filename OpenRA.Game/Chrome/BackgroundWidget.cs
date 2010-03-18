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

using OpenRA.Graphics;

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
			
			Game.chrome.renderer.Device.DisableScissor();
		
			
			base.Draw();
		}
	}
}