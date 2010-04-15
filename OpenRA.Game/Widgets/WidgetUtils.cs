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
			Game.chrome.renderer.RgbaSpriteRenderer.DrawSprite(s,pos,"chrome");
		}
		
		public static void DrawSHP(Sprite s, float2 pos)
		{
			Game.chrome.renderer.WorldSpriteRenderer.DrawSprite(s,pos,"chrome");
		}

		public static void DrawPanel(string collection, Rectangle Bounds)
		{
			DrawPanelPartial(collection, Bounds, PanelSides.All);
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

		public static int[] GetBorderSizes(string collection)
		{
			var images = new[] { "border-t", "border-b", "border-l", "border-r" };
			var ss = images.Select(i => ChromeProvider.GetImage(Game.chrome.renderer, "dialog4", i)).ToArray();
			return new[] { (int)ss[0].size.Y, (int)ss[1].size.Y, (int)ss[2].size.X, (int)ss[3].size.X };
		}

		static bool HasFlags(this PanelSides a, PanelSides b) { return (a & b) == b; }
		public static Rectangle InflateBy(this Rectangle rect, int l, int t, int r, int b)
		{
			return Rectangle.FromLTRB(rect.Left - l, rect.Top - t,
				rect.Right + r, rect.Bottom + b);
		}

		public static void DrawPanelPartial(string collection, Rectangle Bounds, PanelSides ps)
		{
			var images = new[] { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
			var ss = images.Select(i => ChromeProvider.GetImage(Game.chrome.renderer, collection, i)).ToArray();

			// Background
			FillRectWithSprite(new Rectangle(Bounds.Left + (int)ss[2].size.X,
								 Bounds.Top + (int)ss[0].size.Y,
								 Bounds.Right - (int)ss[3].size.X - Bounds.Left - (int)ss[2].size.X,
								 Bounds.Bottom - (int)ss[1].size.Y - Bounds.Top - (int)ss[0].size.Y),
				   ss[8]);

			// Left border
			if (ps.HasFlags(PanelSides.Left))
				FillRectWithSprite(new Rectangle(Bounds.Left,
									 Bounds.Top + (int)ss[0].size.Y,
									 (int)ss[2].size.X,
									 Bounds.Bottom - (int)ss[1].size.Y - Bounds.Top - (int)ss[0].size.Y),
					   ss[2]);

			// Right border
			if (ps.HasFlags(PanelSides.Right))
				FillRectWithSprite(new Rectangle(Bounds.Right - (int)ss[3].size.X,
									 Bounds.Top + (int)ss[0].size.Y,
									 (int)ss[2].size.X,
									 Bounds.Bottom - (int)ss[1].size.Y - Bounds.Top - (int)ss[0].size.Y),
					   ss[3]);

			// Top border
			if (ps.HasFlags(PanelSides.Top))
				FillRectWithSprite(new Rectangle(Bounds.Left + (int)ss[2].size.X,
									 Bounds.Top,
									 Bounds.Right - (int)ss[3].size.X - Bounds.Left - (int)ss[2].size.X,
									 (int)ss[0].size.Y),
					   ss[0]);

			// Bottom border
			if (ps.HasFlags(PanelSides.Bottom))
				FillRectWithSprite(new Rectangle(Bounds.Left + (int)ss[2].size.X,
									Bounds.Bottom - (int)ss[1].size.Y,
									 Bounds.Right - (int)ss[3].size.X - Bounds.Left - (int)ss[2].size.X,
									 (int)ss[0].size.Y),
					   ss[1]);

			if (ps.HasFlags(PanelSides.Left | PanelSides.Top))
				DrawRGBA(ss[4], new float2(Bounds.Left, Bounds.Top));
			if (ps.HasFlags(PanelSides.Right | PanelSides.Top))
				DrawRGBA(ss[5], new float2(Bounds.Right - ss[5].size.X, Bounds.Top));
			if (ps.HasFlags(PanelSides.Left | PanelSides.Bottom))
				DrawRGBA(ss[6], new float2(Bounds.Left, Bounds.Bottom - ss[6].size.Y));
			if (ps.HasFlags(PanelSides.Right | PanelSides.Bottom))
				DrawRGBA(ss[7], new float2(Bounds.Right - ss[7].size.X, Bounds.Bottom - ss[7].size.Y));

			Game.chrome.renderer.RgbaSpriteRenderer.Flush();
		}
	}

	[Flags]
	public enum PanelSides
	{
		Left = 1,
		Top = 2,
		Right = 4,
		Bottom = 8,

		All = Left | Top | Right | Bottom
	}
}
