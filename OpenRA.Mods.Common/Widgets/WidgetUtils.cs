#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Widgets
{
	public static class WidgetUtils
	{
		public static Sprite GetChromeImage(World world, string name)
		{
			return ChromeProvider.GetImage("chrome-" + world.LocalPlayer.Faction.InternalName, name);
		}

		public static void DrawRGBA(Sprite s, float2 pos)
		{
			Game.Renderer.RgbaSpriteRenderer.DrawSprite(s, pos);
		}

		public static void DrawSHPCentered(Sprite s, float2 pos, PaletteReference p)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(s, pos - 0.5f * s.Size, p);
		}

		public static void DrawSHPCentered(Sprite s, float2 pos, PaletteReference p, float scale)
		{
			Game.Renderer.SpriteRenderer.DrawSprite(s, pos - 0.5f * scale * s.Size, p, scale * s.Size);
		}

		public static void DrawPanel(string collection, Rectangle bounds)
		{
			var sprites = ChromeProvider.GetPanelImages(collection);
			if (sprites != null)
				DrawPanel(bounds, sprites);
		}

		public static void FillRectWithSprite(Rectangle r, Sprite s)
		{
			var scale = s.Size.X / s.Bounds.Width;
			for (var x = (float)r.Left; x < r.Right; x += s.Size.X)
			{
				for (var y = (float)r.Top; y < r.Bottom; y += s.Size.Y)
				{
					var ss = s;
					var dx = r.Right - x;
					var dy = r.Bottom - y;
					if (dx < s.Size.X || dy < s.Size.Y)
					{
						var rr = new Rectangle(
							s.Bounds.Left,
							s.Bounds.Top,
							Math.Min(s.Bounds.Width, (int)(dx / scale)),
							Math.Min(s.Bounds.Height, (int)(dy / scale)));
						ss = new Sprite(s.Sheet, rr, s.Channel, scale);
					}

					DrawRGBA(ss, new float2(x, y));
				}
			}
		}

		public static void FillRectWithColor(Rectangle r, Color c)
		{
			// Offset to the edges of the pixels
			var tl = new float2(r.Left - 0.5f, r.Top - 0.5f);
			var br = new float2(r.Right - 0.5f, r.Bottom - 0.5f);
			Game.Renderer.RgbaColorRenderer.FillRect(tl, br, c);
		}

		public static void FillRectWithColor(Rectangle r, Color topLeftColor, Color topRightColor, Color bottomRightColor, Color bottomLeftColor)
		{
			var tl = new float2(r.Left - 0.5f, r.Top - 0.5f);
			var br = new float2(r.Right - 0.5f, r.Bottom - 0.5f);

			var tr = new float3(br.X, tl.Y, 0);
			var bl = new float3(tl.X, br.Y, 0);

			Game.Renderer.RgbaColorRenderer.FillRect(tl, tr, br, bl, topLeftColor, topRightColor, bottomRightColor, bottomLeftColor);
		}

		public static void FillEllipseWithColor(Rectangle r, Color c)
		{
			var tl = new float2(r.Left, r.Top);
			var br = new float2(r.Right, r.Bottom);
			Game.Renderer.RgbaColorRenderer.FillEllipse(tl, br, c);
		}

		public static Rectangle InflateBy(this Rectangle rect, int l, int t, int r, int b)
		{
			return Rectangle.FromLTRB(rect.Left - l, rect.Top - t,
				rect.Right + r, rect.Bottom + b);
		}

		/// <summary>
		/// Fill a rectangle with sprites defining a panel layout.
		/// Draw order is center, borders, corners to allow mods to define fancy border and corner overlays.
		/// </summary>
		/// <param name="bounds">Rectangle to fill.</param>
		/// <param name="sprites">Nine sprites defining the panel: TL, T, TR, L, C, R, BL, B, BR.</param>
		public static void DrawPanel(Rectangle bounds, Sprite[] sprites)
		{
			if (sprites.Length != 9)
				return;

			var marginTop = sprites[1] == null ? 0 : (int)sprites[1].Size.Y;
			var marginLeft = sprites[3] == null ? 0 : (int)sprites[3].Size.X;
			var marginRight = sprites[5] == null ? 0 : (int)sprites[5].Size.X;
			var marginBottom = sprites[7] == null ? 0 : (int)sprites[7].Size.Y;
			var marginWidth = marginRight + marginLeft;
			var marginHeight = marginBottom + marginTop;

			// Center
			if (sprites[4] != null)
				FillRectWithSprite(new Rectangle(bounds.Left + marginLeft, bounds.Top + marginTop,
					bounds.Width - marginWidth, bounds.Height - marginHeight), sprites[4]);

			// Left edge
			if (sprites[3] != null)
				FillRectWithSprite(new Rectangle(bounds.Left, bounds.Top + marginTop,
						marginLeft, bounds.Height - marginHeight), sprites[3]);

			// Right edge
			if (sprites[5] != null)
				FillRectWithSprite(new Rectangle(bounds.Right - marginRight, bounds.Top + marginTop,
					marginLeft, bounds.Height - marginHeight), sprites[5]);

			// Top edge
			if (sprites[1] != null)
				FillRectWithSprite(new Rectangle(bounds.Left + marginLeft, bounds.Top,
					bounds.Width - marginWidth, marginTop), sprites[1]);

			// Bottom edge
			if (sprites[7] != null)
				FillRectWithSprite(new Rectangle(bounds.Left + marginLeft, bounds.Bottom - marginBottom,
					bounds.Width - marginWidth, marginTop), sprites[7]);

			// Top-left corner
			if (sprites[0] != null)
				DrawRGBA(sprites[0], new float2(bounds.Left, bounds.Top));

			// Top-right corner
			if (sprites[2] != null)
				DrawRGBA(sprites[2], new float2(bounds.Right - sprites[2].Size.X, bounds.Top));

			// Bottom-left corner
			if (sprites[6] != null)
				DrawRGBA(sprites[6], new float2(bounds.Left, bounds.Bottom - sprites[6].Size.Y));

			// Bottom-right corner
			if (sprites[8] != null)
				DrawRGBA(sprites[8], new float2(bounds.Right - sprites[8].Size.X, bounds.Bottom - sprites[8].Size.Y));
		}

		public static string FormatTime(int ticks, int timestep)
		{
			return FormatTime(ticks, true, timestep);
		}

		public static string FormatTime(int ticks, bool leadingMinuteZero, int timestep)
		{
			var seconds = (int)Math.Ceiling(ticks * timestep / 1000f);
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
					if (font.Measure(line).X <= width)
						continue;

					// Scan forwards until we find the last word that fits
					// This guarantees a small bound on the amount of string we need to search before a linebreak
					var start = 0;
					while (true)
					{
						var spaceIndex = line.IndexOf(' ', start);
						if (spaceIndex == -1)
							break;

						var fragmentWidth = font.Measure(line.Substring(0, spaceIndex)).X;
						if (fragmentWidth > width)
							break;

						start = spaceIndex + 1;
					}

					if (start > 0)
					{
						lines[i] = line.Substring(0, start - 1);
						lines.Insert(i + 1, line.Substring(start));
					}
				}

				return string.Join("\n", lines);
			}

			return text;
		}

		public static string TruncateText(string text, int width, SpriteFont font)
		{
			var trimmedWidth = font.Measure(text).X;
			if (trimmedWidth <= width)
				return text;

			var trimmed = text;
			while (trimmedWidth > width && trimmed.Length > 3)
			{
				trimmed = text.Substring(0, trimmed.Length - 4) + "...";
				trimmedWidth = font.Measure(trimmed).X;
			}

			return trimmed;
		}

		public static void TruncateLabelToTooltip(LabelWithTooltipWidget label, string text)
		{
			var truncatedText = TruncateText(text, label.Bounds.Width, Game.Renderer.Fonts[label.Font]);

			label.GetText = () => truncatedText;

			if (text != truncatedText)
				label.GetTooltipText = () => text;
			else
				label.GetTooltipText = null;
		}

		public static void TruncateButtonToTooltip(ButtonWidget button, string text)
		{
			var truncatedText = TruncateText(text, button.Bounds.Width - button.LeftMargin - button.RightMargin, Game.Renderer.Fonts[button.Font]);

			button.GetText = () => truncatedText;

			if (text != truncatedText)
				button.GetTooltipText = () => text;
			else
				button.GetTooltipText = null;
		}
	}

	public class CachedTransform<T, U>
	{
		readonly Func<T, U> transform;

		bool initialized;
		T lastInput;
		U lastOutput;

		public CachedTransform(Func<T, U> transform)
		{
			this.transform = transform;
		}

		public U Update(T input)
		{
			if (initialized && ((input == null && lastInput == null) || (input != null && input.Equals(lastInput))))
				return lastOutput;

			lastInput = input;
			lastOutput = transform(input);
			initialized = true;

			return lastOutput;
		}
	}
}
