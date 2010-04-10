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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	static class WidgetUtils
	{
		public static Sprite GetChromeImage(World world, string name)
		{
			return ChromeProvider.GetImage(Game.chrome.renderer, "chrome-" + world.LocalPlayer.Country.Race, name);
		}
		
		public static void DrawRGBA(Sprite s, float2 pos)
		{
			Game.chrome.rgbaRenderer.DrawSprite(s,pos,"chrome");
		}
		
		public static void DrawSHP(Sprite s, float2 pos)
		{
			Game.chrome.shpRenderer.DrawSprite(s,pos,"chrome");
		}
		
		public static void DrawPanel(string collection, Rectangle Bounds, Action a)
		{
			var r = Game.chrome.renderer;
			var sr = Game.chrome.rgbaRenderer;


			var images = new[] { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
			var ss = images.Select(i => ChromeProvider.GetImage(Game.chrome.renderer, collection, i)).ToArray();
			
			// Don't draw the background below the bottom/right borders
			r.Device.EnableScissor(Bounds.Left, Bounds.Top, Bounds.Width - (int)ss[3].size.X, Bounds.Height - (int)ss[1].size.Y);
			for (var x = Bounds.Left + (int)ss[2].size.X; x < Bounds.Right - (int)ss[3].size.X; x += (int)ss[8].size.X)
				for (var y = Bounds.Top + (int)ss[0].size.Y; y < Bounds.Bottom - (int)ss[1].size.Y; y += (int)ss[8].size.Y)
					sr.DrawSprite(ss[8], new float2(x, y), "chrome");
			
			sr.Flush();		// because the scissor is changing
			r.Device.EnableScissor(Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height - (int)ss[1].size.Y);

			//draw borders
			for (var y = Bounds.Top + (int)ss[0].size.Y; y < Bounds.Bottom - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
			{
				sr.DrawSprite(ss[2], new float2(Bounds.Left, y), "chrome");
				sr.DrawSprite(ss[3], new float2(Bounds.Right - ss[3].size.X, y), "chrome");
			}

			sr.Flush();		// because the scissor is changing
			r.Device.EnableScissor(Bounds.Left, Bounds.Top, Bounds.Width - (int)ss[3].size.X, Bounds.Height);

			for (var x = Bounds.Left + (int)ss[2].size.X; x < Bounds.Right - (int)ss[3].size.X; x += (int)ss[0].size.X)
			{
				sr.DrawSprite(ss[0], new float2(x, Bounds.Top), "chrome");
				sr.DrawSprite(ss[1], new float2(x, Bounds.Bottom - ss[1].size.Y), "chrome");
			}

			sr.Flush();		// because the scissor is changing
			r.Device.EnableScissor(Bounds.Left, Bounds.Top, Bounds.Width, Bounds.Height);

			sr.DrawSprite(ss[4], new float2(Bounds.Left, Bounds.Top), "chrome");
			sr.DrawSprite(ss[5], new float2(Bounds.Right - ss[5].size.X, Bounds.Top), "chrome");
			sr.DrawSprite(ss[6], new float2(Bounds.Left, Bounds.Bottom - ss[6].size.Y), "chrome");
			sr.DrawSprite(ss[7], new float2(Bounds.Right - ss[7].size.X, Bounds.Bottom - ss[7].size.Y), "chrome");

			if (a != null) a();

			sr.Flush();		// because the scissor is changing
			r.Device.DisableScissor();
		}
	
		public static void DrawRightTooltip(string collection, int2 tl, int2 m, int2 br, Action a)
		{
			var r = Game.chrome.renderer;
			var sr = Game.chrome.rgbaRenderer;
			
			var images = new[] { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background"};
			var ss = images.Select(i => ChromeProvider.GetImage(Game.chrome.renderer, collection, i)).ToArray();
		
			// Draw the background for the left part
			r.Device.EnableScissor(tl.X, tl.Y, m.X-tl.X + (int)ss[2].size.X, m.Y-tl.Y - (int)ss[1].size.Y);
			for (var x = tl.X + (int)ss[2].size.X; x < m.X + (int)ss[2].size.X; x += (int)ss[8].size.X)
				for (var y = tl.Y + (int)ss[0].size.Y; y < m.Y - (int)ss[1].size.Y; y += (int)ss[8].size.Y)
					DrawRGBA(ss[8], new float2(x, y));
			
			// Left border
			for (var y = tl.Y + (int)ss[0].size.Y; y < m.Y - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
				DrawRGBA(ss[2], new float2(tl.X, y));
			
			sr.Flush();
			r.Device.EnableScissor(tl.X, tl.Y, m.X-tl.X, m.Y-tl.Y);

			// bottom-left border
			for (var x = tl.X + (int)ss[2].size.X; x < m.X - (int)ss[2].size.X; x += (int)ss[0].size.X)
				DrawRGBA(ss[1], new float2(x, m.Y - ss[1].size.Y));
			
			// BL corner
			DrawRGBA(ss[6], new float2(tl.X,m.Y - (int)ss[2].size.X));
			
			sr.Flush(); 
			r.Device.EnableScissor(m.X, tl.Y, br.X - m.X - (int)ss[3].size.X, br.Y - tl.Y - (int)ss[1].size.Y);
			
			// Background for the right part
			for (var x = m.X + (int)ss[2].size.X; x < br.X - (int)ss[3].size.X; x += (int)ss[8].size.X)
				for (var y = tl.Y + (int)ss[0].size.Y; y < br.Y - (int)ss[1].size.Y; y += (int)ss[8].size.Y)
					DrawRGBA(ss[8], new float2(x, y));
						
			// Top border
			sr.Flush(); 
			r.Device.EnableScissor(tl.X, tl.Y, br.X - tl.X - (int)ss[3].size.X, (int)ss[0].size.Y);
			for (var x = tl.X + (int)ss[2].size.X; x < br.X - (int)ss[3].size.X; x += (int)ss[1].size.X)
				DrawRGBA(ss[0], new float2(x, tl.Y));
			
			// TL corner
			DrawRGBA(ss[4], new float2(tl.X,tl.Y));
			
			sr.Flush(); 
			r.Device.EnableScissor(br.X - (int)ss[3].size.X, tl.Y, (int)ss[3].size.X, br.Y - tl.Y - (int)ss[1].size.Y);
			
			// Right border
			for (var y = tl.Y + (int)ss[0].size.Y; y < br.Y - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
				DrawRGBA(ss[3], new float2(br.X - (int)ss[3].size.X, y));
			
			// TR corner
			DrawRGBA(ss[5], new float2(br.X- (int)ss[3].size.X,tl.Y));
				
			// Bottom border
			sr.Flush(); 
			r.Device.EnableScissor(m.X, br.Y - (int)ss[1].size.Y, br.X - m.X - (int)ss[3].size.X,(int)ss[1].size.Y);
			for (var x = m.X + (int)ss[2].size.X; x < br.X - (int)ss[3].size.X; x += (int)ss[1].size.X)
				DrawRGBA(ss[1], new float2(x, br.Y - (int)ss[1].size.Y));
			
			// BR corner
			sr.Flush();
			r.Device.DisableScissor();
			DrawRGBA(ss[7], new float2(br.X - (int)ss[7].size.X, br.Y - (int)ss[7].size.Y));
			
			// Left border
			sr.Flush();
			r.Device.EnableScissor(m.X, m.Y-1, (int)ss[2].size.X, br.Y - m.Y - (int)ss[1].size.Y+1);
			for (var y = m.Y-1; y < br.Y - (int)ss[1].size.Y; y += (int)ss[2].size.Y)
				DrawRGBA(ss[2], new float2(m.X, y));
			
			// BL corner
			sr.Flush();
			r.Device.DisableScissor();
			DrawRGBA(ss[6], new float2(m.X,br.Y - (int)ss[7].size.Y));

			// Patch the hole
			sr.Flush();
			r.Device.EnableScissor(m.X, m.Y-(int)ss[1].size.Y, (int)ss[2].size.X, (int)ss[1].size.Y-1);
			for (var x = m.X; x < m.X + (int)ss[2].size.X; x += (int)ss[8].size.X)
				for (var y = m.Y-(int)ss[1].size.Y; y < m.Y-1; y += (int)ss[8].size.Y)
					DrawRGBA(ss[8], new float2(x, y));
		
			if (a != null) a();

			sr.Flush();		// because the scissor is changing
			r.Device.DisableScissor();
		}
	}
}
