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
		
		public static void FillRectWithSprite(Rectangle r, Sprite s)
		{
			for (var x = r.Left; x < r.Right; x += (int)s.size.X)
				for (var y = r.Top; y < r.Bottom; y += (int)s.size.Y)
				{
					var ss = s;
					var left = new int2(r.Right - x, r.Bottom - y);
					if (left.X < (int)s.size.X || left.Y < (int)s.size.Y)
					{
						Rectangle rr = new Rectangle(s.bounds.Left,s.bounds.Top,Math.Min(left.X,(int)s.size.X),Math.Min(left.Y,(int)s.size.Y));
						ss = new Sprite(s.sheet,rr,s.channel);
					}
					DrawRGBA(ss, new float2(x, y));
				}
		}
	
		public static void DrawRightTooltip(string collection, int2 tl, int2 m, int2 br, Action a)
		{
			var images = new[] { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background"};
			var ss = images.Select(i => ChromeProvider.GetImage(Game.chrome.renderer, collection, i)).ToArray();
		
			// Draw the background for the left part		
			FillRectWithSprite(new Rectangle(tl.X + (int)ss[2].size.X,
			                                 tl.Y + (int)ss[0].size.Y,
			                                 m.X + (int)ss[2].size.X - tl.X - (int)ss[2].size.X,
			                                 m.Y - (int)ss[1].size.Y - tl.Y - (int)ss[0].size.Y),
			                   ss[8]);
			
			// Background for the right part
			FillRectWithSprite(new Rectangle(m.X + (int)ss[2].size.X,
                                 			tl.Y + (int)ss[0].size.Y,
                                 			br.X - (int)ss[3].size.X - m.X - (int)ss[2].size.X,
                                 			br.Y - (int)ss[1].size.Y - tl.Y - (int)ss[0].size.Y),
                   				ss[8]);
			
			// Patch the hole
			FillRectWithSprite(new Rectangle(m.X,
                                 			m.Y-(int)ss[1].size.Y,
                                 			(int)ss[2].size.X,
                                 			(int)ss[1].size.Y - 1),
                   				ss[8]);
			
			// Top border
			FillRectWithSprite(new Rectangle(tl.X + (int)ss[2].size.X,
                                 			tl.Y,
                                 			br.X - (int)ss[3].size.X - tl.X - (int)ss[2].size.X,
                                 			(int)ss[0].size.Y),
                   				ss[0]);

			// Right border
			FillRectWithSprite(new Rectangle(br.X - (int)ss[3].size.X,
                                 			tl.Y + (int)ss[0].size.Y,
                                 			(int)ss[3].size.X,
                                 			br.Y - (int)ss[1].size.Y - tl.Y - (int)ss[0].size.Y),
                   				ss[3]);

			// Bottom border
			FillRectWithSprite(new Rectangle(m.X + (int)ss[2].size.X,
                                 			br.Y - (int)ss[1].size.Y,
                                 			br.X - (int)ss[3].size.X - m.X - (int)ss[2].size.X,
                                 			(int)ss[1].size.Y),
                   				ss[1]);
			// Left border
			FillRectWithSprite(new Rectangle(tl.X,
                                 			tl.Y + (int)ss[0].size.Y,
                                 			(int)ss[2].size.X,
                                 			m.Y - (int)ss[1].size.Y - tl.Y - (int)ss[0].size.Y),
                   				ss[2]);
			
			// Left-bottom border
			FillRectWithSprite(new Rectangle(tl.X + (int)ss[2].size.X,
                                 			m.Y - (int)ss[1].size.Y,
                                 			m.X - (int)ss[2].size.X - tl.X,
                                 			(int)ss[1].size.Y),
                   				ss[1]);
			
			// Bottom-left border
			FillRectWithSprite(new Rectangle(m.X,
                                 			m.Y - 1,
                                 			(int)ss[2].size.X,
                                 			br.Y - (int)ss[1].size.Y - m.Y + 1),
                   				ss[2]);
			
			// TL corner
			DrawRGBA(ss[4], new float2(tl.X,tl.Y));
			
			// TR corner
			DrawRGBA(ss[5], new float2(br.X- (int)ss[3].size.X,tl.Y));
			
			// LBL corner
			DrawRGBA(ss[6], new float2(tl.X,m.Y - (int)ss[2].size.X));
			
			// RBL corner
			DrawRGBA(ss[6], new float2(m.X,br.Y - (int)ss[7].size.Y));

			// BR corner
			DrawRGBA(ss[7], new float2(br.X - (int)ss[7].size.X, br.Y - (int)ss[7].size.Y));
			
			Game.chrome.rgbaRenderer.Flush();
			if (a != null) a();
		}
	}
}
