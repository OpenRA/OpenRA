#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
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
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(s, pos);
		}

		public static void DrawSHPCentered(Sprite s, float2 pos, WorldRenderer wr)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(s, pos - 0.5f * s.Size, wr.Palette("chrome"));
		}

		public static void DrawSHPCentered(Sprite s, float2 pos, WorldRenderer wr, float scale)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(s, pos - 0.5f * scale * s.Size, wr.Palette("chrome"), scale * s.Size);
		}

		public static void DrawPanel(string collection, Rectangle bounds)
		{
			DrawPanelPartial(collection, bounds, PanelSides.All);
		}

		public static void FillRectWithSprite(Rectangle r, Sprite s)
		{
			for (var x = r.Left; x < r.Right; x += (int)s.Size.X)
				for (var y = r.Top; y < r.Bottom; y += (int)s.Size.Y)
				{
					var ss = s;
					var left = new int2(r.Right - x, r.Bottom - y);
					if (left.X < (int)s.Size.X || left.Y < (int)s.Size.Y)
					{
						var rr = new Rectangle(s.Bounds.Left,
							s.Bounds.Top,
							Math.Min(left.X, (int)s.Size.X),
							Math.Min(left.Y, (int)s.Size.Y));
						ss = new Sprite(s.Sheet, rr, s.Channel);
					}

					DrawRGBA(ss, new float2(x, y));
				}
		}

		public static void FillRectWithColor(Rectangle r, Color c)
		{
			Game.Renderer.LineRenderer.FillRect(new RectangleF(r.X, r.Y, r.Width, r.Height), c);
		}

		public static void FillEllipseWithColor(Rectangle r, Color c)
		{
			Game.Renderer.LineRenderer.FillEllipse(new RectangleF(r.X, r.Y, r.Width, r.Height), c);
		}

		public static int[] GetBorderSizes(string collection)
		{
			var images = new[] { "border-t", "border-b", "border-l", "border-r" };
			var ss = images.Select(i => ChromeProvider.GetImage(collection, i)).ToList();
			return new[] { (int)ss[0].Size.Y, (int)ss[1].Size.Y, (int)ss[2].Size.X, (int)ss[3].Size.X };
		}

		static bool HasFlags(this PanelSides a, PanelSides b) { return (a & b) == b; }
		public static Rectangle InflateBy(this Rectangle rect, int l, int t, int r, int b)
		{
			return Rectangle.FromLTRB(rect.Left - l, rect.Top - t,
				rect.Right + r, rect.Bottom + b);
		}

		public static void DrawPanelPartial(string collection, Rectangle bounds, PanelSides ps)
		{
			DrawPanelPartial(bounds, ps,
				ChromeProvider.GetImage(collection, "border-t"),
				ChromeProvider.GetImage(collection, "border-b"),
				ChromeProvider.GetImage(collection, "border-l"),
				ChromeProvider.GetImage(collection, "border-r"),
				ChromeProvider.GetImage(collection, "corner-tl"),
				ChromeProvider.GetImage(collection, "corner-tr"),
				ChromeProvider.GetImage(collection, "corner-bl"),
				ChromeProvider.GetImage(collection, "corner-br"),
				ChromeProvider.GetImage(collection, "background"));
		}

		public static void DrawPanelPartial(Rectangle bounds, PanelSides ps,
			Sprite borderTop,
			Sprite borderBottom,
			Sprite borderLeft,
			Sprite borderRight,
			Sprite cornerTopLeft,
			Sprite cornerTopRight,
			Sprite cornerBottomLeft,
			Sprite cornerBottomRight,
			Sprite background)
		{
			var marginLeft = borderLeft == null ? 0 : (int)borderLeft.Size.X;
			var marginTop = borderTop == null ? 0 : (int)borderTop.Size.Y;
			var marginRight = borderRight == null ? 0 : (int)borderRight.Size.X;
			var marginBottom = borderBottom == null ? 0 : (int)borderBottom.Size.Y;
			var marginWidth = marginRight + marginLeft;
			var marginHeight = marginBottom + marginTop;

			// Background
			if (ps.HasFlags(PanelSides.Center) && background != null)
				FillRectWithSprite(new Rectangle(bounds.Left + marginLeft, bounds.Top + marginTop,
					bounds.Width - marginWidth, bounds.Height - marginHeight),
					background);

			// Left border
			if (ps.HasFlags(PanelSides.Left) && borderLeft != null)
				FillRectWithSprite(new Rectangle(bounds.Left, bounds.Top + marginTop,
					marginLeft, bounds.Height - marginHeight),
					borderLeft);

			// Right border
			if (ps.HasFlags(PanelSides.Right) && borderRight != null)
				FillRectWithSprite(new Rectangle(bounds.Right - marginRight, bounds.Top + marginTop,
					marginLeft, bounds.Height - marginHeight),
					borderRight);

			// Top border
			if (ps.HasFlags(PanelSides.Top) && borderTop != null)
				FillRectWithSprite(new Rectangle(bounds.Left + marginLeft, bounds.Top,
					bounds.Width - marginWidth, marginTop),
					borderTop);

			// Bottom border
			if (ps.HasFlags(PanelSides.Bottom) && borderBottom != null)
				FillRectWithSprite(new Rectangle(bounds.Left + marginLeft, bounds.Bottom - marginBottom,
					bounds.Width - marginWidth, marginTop),
					borderBottom);

			if (ps.HasFlags(PanelSides.Left | PanelSides.Top) && cornerTopLeft != null)
				DrawRGBA(cornerTopLeft, new float2(bounds.Left, bounds.Top));
			if (ps.HasFlags(PanelSides.Right | PanelSides.Top) && cornerTopRight != null)
				DrawRGBA(cornerTopRight, new float2(bounds.Right - cornerTopRight.Size.X, bounds.Top));
			if (ps.HasFlags(PanelSides.Left | PanelSides.Bottom) && cornerBottomLeft != null)
				DrawRGBA(cornerBottomLeft, new float2(bounds.Left, bounds.Bottom - cornerBottomLeft.Size.Y));
			if (ps.HasFlags(PanelSides.Right | PanelSides.Bottom) && cornerBottomRight != null)
				DrawRGBA(cornerBottomRight, new float2(bounds.Right - cornerBottomRight.Size.X, bounds.Bottom - cornerBottomRight.Size.Y));
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
				var lines = text.Split('\n').ToList();

				for (var i = 0; i < lines.Count; i++)
				{
					var line = lines[i];
					var m = font.Measure(line);

					if (m.X <= width)
						continue;

					var bestSpaceIndex = -1;
					var start = line.Length - 1;

					while (m.X > width)
					{
						var spaceIndex = line.LastIndexOf(' ', start);
						if (spaceIndex == -1)
							break;
						bestSpaceIndex = spaceIndex;

						start = spaceIndex - 1;
						m = font.Measure(line.Substring(0, spaceIndex));
					}

					if (bestSpaceIndex != -1)
					{
						lines[i] = line.Substring(0, bestSpaceIndex);
						lines.Insert(i + 1, line.Substring(bestSpaceIndex + 1));
					}
				}

				return string.Join("\n", lines);
			}

			return text;
		}

		public static Action Once(Action a) { return () => { if (a != null) { a(); a = null; } }; }

		public static string ChooseInitialMap(string initialUid)
		{
			if (string.IsNullOrEmpty(initialUid) || Game.ModData.MapCache[initialUid].Status != MapStatus.Available)
			{
				Func<MapPreview, bool> isIdealMap = m =>
				{
					if (m.Status != MapStatus.Available || !m.Map.Visibility.HasFlag(MapVisibility.Lobby))
						return false;

					// Other map types may have confusing settings or gameplay
					if (m.Type != "Conquest")
						return false;

					// Maps with bots disabled confuse new players
					if (m.Map.Players.Any(s => !s.Value.AllowBots))
						return false;

					// Large maps expose unfortunate performance problems
					if (m.Bounds.Width > 128 || m.Bounds.Height > 128)
						return false;

					return true;
				};

				var selected = Game.ModData.MapCache.Where(m => isIdealMap(m)).RandomOrDefault(Game.CosmeticRandom) ??
					Game.ModData.MapCache.First(m => m.Status == MapStatus.Available && m.Map.Visibility.HasFlag(MapVisibility.Lobby));
				return selected.Uid;
			}

			return initialUid;
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
