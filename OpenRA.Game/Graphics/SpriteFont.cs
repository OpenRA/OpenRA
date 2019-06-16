#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Graphics
{
	public sealed class SpriteFont : IDisposable
	{
		public int TopOffset { get; private set; }
		readonly int size;
		readonly SheetBuilder builder;
		readonly Func<string, float> lineWidth;
		readonly IFont font;
		readonly Cache<Pair<char, Color>, GlyphInfo> glyphs;

		float deviceScale;

		public SpriteFont(string name, byte[] data, int size, int ascender, float scale, SheetBuilder builder)
		{
			if (builder.Type != SheetType.BGRA)
				throw new ArgumentException("The sheet builder must create BGRA sheets.", "builder");

			deviceScale = scale;
			this.size = size;
			this.builder = builder;

			font = Game.Renderer.CreateFont(data);

			glyphs = new Cache<Pair<char, Color>, GlyphInfo>(CreateGlyph, Pair<char, Color>.EqualityComparer);

			// PERF: Cache these delegates for Measure calls.
			Func<char, float> characterWidth = character => glyphs[Pair.New(character, Color.White)].Advance;
			lineWidth = line => line.Sum(characterWidth) / deviceScale;

			if (size <= 24)
				PrecacheColor(Color.White, name);

			TopOffset = size - ascender;
		}

		public void SetScale(float scale)
		{
			deviceScale = scale;
			glyphs.Clear();
		}

		void PrecacheColor(Color c, string name)
		{
			using (new PerfTimer("PrecacheColor {0} {1}px {2}".F(name, size, c)))
				for (var n = (char)0x20; n < (char)0x7f; n++)
					if (glyphs[Pair.New(n, c)] == null)
						throw new InvalidOperationException();
		}

		public void DrawText(string text, float2 location, Color c)
		{
			// Offset from the baseline position to the top-left of the glyph for rendering
			location += new float2(0, size);

			var p = location;
			foreach (var s in text)
			{
				if (s == '\n')
				{
					location += new float2(0, size);
					p = location;
					continue;
				}

				var g = glyphs[Pair.New(s, c)];
				if (g.Sprite != null)
					Game.Renderer.RgbaSpriteRenderer.DrawSprite(g.Sprite,
						new float2(
							(int)Math.Round(p.X * deviceScale + g.Offset.X, 0) / deviceScale,
							p.Y + g.Offset.Y / deviceScale),
						g.Sprite.Size / deviceScale);

				p += new float2(g.Advance / deviceScale, 0);
			}
		}

		float3 Rotate(float3 v, float sina, float cosa, float2 offset)
		{
			return new float3(
				v.X * cosa - v.Y * sina + offset.X,
				v.X * sina + v.Y * cosa + offset.Y,
				0);
		}

		public void DrawText(string text, float2 location, Color c, float angle)
		{
			// Offset from the baseline position to the top-left of the glyph for rendering
			var offset = new float2(0, size);
			var cosa = (float)Math.Cos(-angle);
			var sina = (float)Math.Sin(-angle);

			var p = offset;
			foreach (var s in text)
			{
				if (s == '\n')
				{
					offset += new float2(0, size);
					p = offset;
					continue;
				}

				var g = glyphs[Pair.New(s, c)];
				if (g.Sprite != null)
				{
					var tl = new float3(
						(int)Math.Round(p.X * deviceScale + g.Offset.X, 0) / deviceScale,
						p.Y + g.Offset.Y / deviceScale, 0);
					var br = tl + g.Sprite.Size / deviceScale;
					var tr = new float3(br.X, tl.Y, 0);
					var bl = new float3(tl.X, br.Y, 0);

					Game.Renderer.RgbaSpriteRenderer.DrawSprite(g.Sprite,
						Rotate(tl, sina, cosa, location),
						Rotate(tr, sina, cosa, location),
						Rotate(br, sina, cosa, location),
						Rotate(bl, sina, cosa, location));
				}

				p += new float2(g.Advance / deviceScale, 0);
			}
		}

		public void DrawTextWithContrast(string text, float2 location, Color fg, Color bg, int offset)
		{
			if (offset > 0)
			{
				DrawText(text, location + new float2(-offset / deviceScale, 0), bg);
				DrawText(text, location + new float2(offset / deviceScale, 0), bg);
				DrawText(text, location + new float2(0, -offset / deviceScale), bg);
				DrawText(text, location + new float2(0, offset / deviceScale), bg);
			}

			DrawText(text, location, fg);
		}

		public void DrawTextWithContrast(string text, float2 location, Color fg, Color bgDark, Color bgLight, int offset)
		{
			DrawTextWithContrast(text, location, fg, GetContrastColor(fg, bgDark, bgLight), offset);
		}

		public void DrawTextWithShadow(string text, float2 location, Color fg, Color bg, int offset)
		{
			if (offset != 0)
				DrawText(text, location + new float2(offset, offset), bg);

			DrawText(text, location, fg);
		}

		public void DrawTextWithShadow(string text, float2 location, Color fg, Color bgDark, Color bgLight, int offset)
		{
			DrawTextWithShadow(text, location, fg, GetContrastColor(fg, bgDark, bgLight), offset);
		}

		public void DrawTextWithShadow(string text, float2 location, Color fg, Color bg, int offset, float angle)
		{
			if (offset != 0)
				DrawText(text, location + new float2(offset, offset), bg, angle);

			DrawText(text, location, fg, angle);
		}

		public void DrawTextWithShadow(string text, float2 location, Color fg, Color bgDark, Color bgLight, int offset, float angle)
		{
			DrawTextWithShadow(text, location, fg, GetContrastColor(fg, bgDark, bgLight), offset, angle);
		}

		public int2 Measure(string text)
		{
			if (string.IsNullOrEmpty(text))
				return new int2(0, size);

			var lines = text.Split('\n');
			return new int2((int)Math.Ceiling(lines.Max(lineWidth)), lines.Length * size);
		}

		GlyphInfo CreateGlyph(Pair<char, Color> c)
		{
			var glyph = font.CreateGlyph(c.First, size, deviceScale);

			if (glyph.Data == null)
			{
				return new GlyphInfo
				{
					Sprite = null,
					Advance = 0,
					Offset = int2.Zero
				};
			}

			var s = builder.Allocate(glyph.Size);
			var g = new GlyphInfo
			{
				Sprite = s,
				Advance = glyph.Advance,
				Offset = glyph.Offset
			};

			var dest = s.Sheet.GetData();
			var destStride = s.Sheet.Size.Width * 4;

			for (var j = 0; j < s.Size.Y; j++)
			{
				for (var i = 0; i < s.Size.X; i++)
				{
					var p = glyph.Data[j * glyph.Size.Width + i];
					if (p != 0)
					{
						var q = destStride * (j + s.Bounds.Top) + 4 * (i + s.Bounds.Left);
						var pmc = Util.PremultiplyAlpha(Color.FromArgb(p, c.Second));

						dest[q] = pmc.B;
						dest[q + 1] = pmc.G;
						dest[q + 2] = pmc.R;
						dest[q + 3] = pmc.A;
					}
				}
			}

			s.Sheet.CommitBufferedData();

			return g;
		}

		static Color GetContrastColor(Color fgColor, Color bgDark, Color bgLight)
		{
			return fgColor == Color.White || fgColor.GetBrightness() > 0.33 ? bgDark : bgLight;
		}

		public void Dispose()
		{
			font.Dispose();
		}
	}

	class GlyphInfo
	{
		public float Advance;
		public int2 Offset;
		public Sprite Sprite;
	}
}
