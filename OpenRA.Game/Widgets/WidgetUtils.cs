#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Widgets
{
	public static class WidgetUtils
	{
		public static Sprite GetChromeImage(World world, string name)
		{
			return ChromeProvider.GetImage("chrome-" + world.LocalPlayer.Country.Race, name);
		}

		public static void DrawRGBA(Sprite s, float2 pos)
		{
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(s,pos);
		}

		public static void DrawSHP(Sprite s, float2 pos, WorldRenderer wr)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(s, pos, wr.Palette("chrome"));
		}

		public static void DrawSHP(Sprite s, float2 pos, WorldRenderer wr, float2 size)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(s, pos, wr.Palette("chrome"), size);
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
						var rr = new Rectangle(s.bounds.Left,
							s.bounds.Top,
							Math.Min(left.X,(int)s.size.X),
							Math.Min(left.Y,(int)s.size.Y));
						ss = new Sprite(s.sheet,rr,s.channel);
					}
					DrawRGBA(ss, new float2(x, y));
				}
		}

		public static void FillRectWithColor(Rectangle r, Color c)
		{
			Game.Renderer.LineRenderer.FillRect(new RectangleF(r.X, r.Y, r.Width, r.Height), c);
		}

		public static int[] GetBorderSizes(string collection)
		{
			var images = new[] { "border-t", "border-b", "border-l", "border-r" };
			var ss = images.Select(i => ChromeProvider.GetImage(collection, i)).ToArray();
			return new[] { (int)ss[0].size.Y, (int)ss[1].size.Y, (int)ss[2].size.X, (int)ss[3].size.X };
		}

		static bool HasFlags(this PanelSides a, PanelSides b) { return (a & b) == b; }
		public static Rectangle InflateBy(this Rectangle rect, int l, int t, int r, int b)
		{
			return Rectangle.FromLTRB(rect.Left - l, rect.Top - t,
				rect.Right + r, rect.Bottom + b);
		}

		public static void DrawPanelPartial(string collection, Rectangle bounds, PanelSides ps)
		{
			var images = new[] { "border-t", "border-b", "border-l", "border-r", "corner-tl", "corner-tr", "corner-bl", "corner-br", "background" };
			var ss = images.Select(i => ChromeProvider.GetImage(collection, i)).ToArray();
			DrawPanelPartial(ss, bounds, ps);
		}

		public static void DrawPanelPartial(Sprite[] ss, Rectangle bounds, PanelSides ps)
		{
			// Background
			if (ps.HasFlags(PanelSides.Center))
				FillRectWithSprite(new Rectangle(bounds.Left + (int)ss[2].size.X,
								 bounds.Top + (int)ss[0].size.Y,
								 bounds.Right - (int)ss[3].size.X - bounds.Left - (int)ss[2].size.X,
								 bounds.Bottom - (int)ss[1].size.Y - bounds.Top - (int)ss[0].size.Y),
				   ss[8]);

			// Left border
			if (ps.HasFlags(PanelSides.Left))
				FillRectWithSprite(new Rectangle(bounds.Left,
									 bounds.Top + (int)ss[0].size.Y,
									 (int)ss[2].size.X,
									 bounds.Bottom - (int)ss[1].size.Y - bounds.Top - (int)ss[0].size.Y),
					   ss[2]);

			// Right border
			if (ps.HasFlags(PanelSides.Right))
				FillRectWithSprite(new Rectangle(bounds.Right - (int)ss[3].size.X,
									 bounds.Top + (int)ss[0].size.Y,
									 (int)ss[2].size.X,
									 bounds.Bottom - (int)ss[1].size.Y - bounds.Top - (int)ss[0].size.Y),
					   ss[3]);

			// Top border
			if (ps.HasFlags(PanelSides.Top))
				FillRectWithSprite(new Rectangle(bounds.Left + (int)ss[2].size.X,
									 bounds.Top,
									 bounds.Right - (int)ss[3].size.X - bounds.Left - (int)ss[2].size.X,
									 (int)ss[0].size.Y),
					   ss[0]);

			// Bottom border
			if (ps.HasFlags(PanelSides.Bottom))
				FillRectWithSprite(new Rectangle(bounds.Left + (int)ss[2].size.X,
									bounds.Bottom - (int)ss[1].size.Y,
									 bounds.Right - (int)ss[3].size.X - bounds.Left - (int)ss[2].size.X,
									 (int)ss[0].size.Y),
					   ss[1]);

			if (ps.HasFlags(PanelSides.Left | PanelSides.Top))
				DrawRGBA(ss[4], new float2(bounds.Left, bounds.Top));
			if (ps.HasFlags(PanelSides.Right | PanelSides.Top))
				DrawRGBA(ss[5], new float2(bounds.Right - ss[5].size.X, bounds.Top));
			if (ps.HasFlags(PanelSides.Left | PanelSides.Bottom))
				DrawRGBA(ss[6], new float2(bounds.Left, bounds.Bottom - ss[6].size.Y));
			if (ps.HasFlags(PanelSides.Right | PanelSides.Bottom))
				DrawRGBA(ss[7], new float2(bounds.Right - ss[7].size.X, bounds.Bottom - ss[7].size.Y));
		}

		public static string FormatTime(int ticks)
		{
			return FormatTime(ticks, true);
		}

		public static string FormatTime(int ticks, bool leadingMinuteZero)
		{
			var seconds = (int)Math.Ceiling(ticks / 25f);
			return FormatTimeSeconds(seconds, leadingMinuteZero);
		}

		public static string FormatTimeSeconds(int seconds)
		{
			return FormatTimeSeconds(seconds, true);
		}

		public static string FormatTimeSeconds(int seconds, bool leadingMinuteZero)
		{
			var minutes = seconds / 60;

			if (minutes >= 60)
				return "{0:D}:{1:D2}:{2:D2}".F(minutes / 60, minutes % 60, seconds % 60);
			if (leadingMinuteZero)
				return "{0:D2}:{1:D2}".F(minutes, seconds % 60);
			return "{0:D}:{1:D2}".F(minutes, seconds % 60);
		}

		public static string WrapText(string text, int width, SpriteFont font)
		{
			var textSize = font.Measure(text);
			if (textSize.X > width)
			{
				var lines = text.Split('\n');
				var newLines = new List<string>();
				var i = 0;
				var line = lines[i++];

				for(;;)
				{
					newLines.Add(line);
					var m = font.Measure(line);
					var spaceIndex = 0;
					var start = line.Length - 1;

					if (m.X <= width)
					{
						if (i < lines.Length - 1)
						{
							line = lines[i++];
							continue;
						}
						else
							break;
					}

					while (m.X > width)
					{
						if (-1 == (spaceIndex = line.LastIndexOf(' ', start)))
							break;
						start = spaceIndex - 1;
						m = font.Measure(line.Substring(0, spaceIndex));
					}

					if (spaceIndex != -1)
					{
						newLines.RemoveAt(newLines.Count - 1);
						newLines.Add(line.Substring(0, spaceIndex));
						line = line.Substring(spaceIndex + 1);
					}
					else if (i < lines.Length - 1)
					{
						line = lines[i++];
						continue;
					}
					else
						break;
				}
				return newLines.JoinWith("\n");
			}
			return text;
		}

		public static Action Once( Action a ) { return () => { if (a != null) { a(); a = null; } }; }

		public static string ActiveModVersion()
		{
			var mod = Game.modData.Manifest.Mods[0];
			return Mod.AllMods[mod].Version;
		}

		public static string ActiveModTitle()
		{
			var mod = Game.modData.Manifest.Mods[0];
			return Mod.AllMods[mod].Title;
		}

		public static string ActiveModId()
		{
			var mod = Game.modData.Manifest.Mods[0];
			return Mod.AllMods[mod].Id;
		}

		public static string ChooseInitialMap(string map)
		{
			var availableMaps = Game.modData.AvailableMaps;
			if (string.IsNullOrEmpty(map) || !availableMaps.ContainsKey(map))
				return availableMaps.First(m => m.Value.Selectable).Key;

			return map;
		}
	}

	[Flags]
	public enum PanelSides
	{
		Left = 1,
		Top = 2,
		Right = 4,
		Bottom = 8,
		Center = 16,

		Edges = Left | Top | Right | Bottom,
		All = Edges | Center,
	}
}
