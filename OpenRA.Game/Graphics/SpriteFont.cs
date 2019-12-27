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
		readonly Cache<Tuple<char, Color, int>, Sprite> contrastGlyphs;
		readonly Cache<int, float[]> dilationElements;

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
			contrastGlyphs = new Cache<Tuple<char, Color, int>, Sprite>(CreateContrastGlyph);
			dilationElements = new Cache<int, float[]>(CreateCircularWeightMap);

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
			contrastGlyphs.Clear();
		}

		void PrecacheColor(Color c, string name)
		{
			using (new PerfTimer("PrecacheColor {0} {1}px {2}".F(name, size, c)))
				for (var n = (char)0x20; n < (char)0x7f; n++)
					if (glyphs[Pair.New(n, c)] == null)
						throw new InvalidOperationException();
		}

		void DrawTextContrast(string text, float2 location, Color contrastColor, int contrastOffset)
		{
			// Offset from the baseline position to the top-left of the glyph for rendering
			location += new float2(0, size);

			var screenContrast = (int)(contrastOffset * deviceScale);
			var p = location;
			foreach (var s in text)
			{
				if (s == '\n')
				{
					location += new float2(0, size);
					p = location;
					continue;
				}

				// Color of source glyph doesn't matter, so use black
				var g = glyphs[Pair.New(s, Color.Black)];
				if (g.Sprite != null)
				{
					var contrastSprite = contrastGlyphs[Tuple.Create(s, contrastColor, screenContrast)];
					Game.Renderer.RgbaSpriteRenderer.DrawSprite(contrastSprite,
						new float2(
								(int)Math.Round(p.X * deviceScale + g.Offset.X - screenContrast, 0) / deviceScale,
								p.Y + (g.Offset.Y - screenContrast) / deviceScale),
						contrastSprite.Size / deviceScale);
				}

				p += new float2(g.Advance / deviceScale, 0);
			}
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
				DrawTextContrast(text, location, bg, offset);

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

		float[] CreateCircularWeightMap(int r)
		{
			// Create circular weight maps that are used by CreateContrastGlyph for
			// both the structuring element and to weight the resulting pixel value.
			// The output is a 2 * r + 1 square array giving the pixel intersection
			// with a circle of radius (r + 0.5).
			//
			// Example output for r=1:
			// 0.60 1.00 0.60
			// 1.00 1.00 1.00
			// 0.60 1.00 0.60
			//
			// Example output for r=3:
			// 0.00 0.44 0.80 1.00 0.80 0.44 0.00
			// 0.44 1.00 1.00 1.00 1.00 1.00 0.44
			// 0.80 1.00 1.00 1.00 1.00 1.00 0.80
			// 1.00 1.00 1.00 1.00 1.00 1.00 1.00
			// 0.80 1.00 1.00 1.00 1.00 1.00 0.80
			// 0.44 1.00 1.00 1.00 1.00 1.00 0.44
			// 0.00 0.44 0.80 1.00 0.80 0.44 0.00
			var stride = 2 * r + 1;
			var elem = new float[stride * stride];

			for (var j = 0; j <= 2 * r; j++)
			{
				for (var i = 0; i <= 2 * r; i++)
				{
					var di = i - r;
					var dj = j - r;

					// No intersection with circle
					if (di * di + dj * dj > (r + 1) * (r + 1))
						continue;

					// Fully contained within circle
					if (di * di + dj * dj < (r - 1) * (r - 1))
					{
						elem[j * stride + i] = 1;
						continue;
					}

					// Approximate sub-pixel intersection using a 5x5 grid
					for (var jj = 0; jj < 5; jj++)
					{
						for (var ii = 0; ii < 5; ii++)
						{
							var si = di - (float)Math.Sign(di) * ii / 5;
							var sj = dj - (float)Math.Sign(dj) * jj / 5;
							if (si * si + sj * sj <= r * r)
								elem[j * stride + i] += 0.04f;
						}
					}
				}
			}

			return elem;
		}

		Sprite CreateContrastGlyph(Tuple<char, Color, int> c)
		{
			// Source glyph color doesn't matter, so use black
			var glyph = glyphs[Pair.New(c.Item1, Color.Black)];
			var color = c.Item2;
			var r = c.Item3;

			var size = new Size(glyph.Sprite.Bounds.Width + 2 * r, glyph.Sprite.Bounds.Height + 2 * r);

			var s = builder.Allocate(size);
			var dest = s.Sheet.GetData();
			var destStride = s.Sheet.Size.Width * 4;

			var glyphData = glyph.Sprite.Sheet.GetData();
			var glyphStride = glyph.Sprite.Sheet.Size.Width * 4;
			var glyphBounds = glyph.Sprite.Bounds;

			var elem = dilationElements[r];
			var elemStride = 2 * r + 1;

			// Expand the glyph by applying the greyscale dilation operator to the source glyph's alpha channel
			for (var j = 0; j < s.Size.Y; j++)
			{
				for (var i = 0; i < s.Size.X; i++)
				{
					// Apply the weight map to the source glyph and find the largest weighted alpha
					var first = true;
					var alpha = (byte)0;
					for (var wj = 0; wj <= 2 * r; wj++)
					{
						for (var wi = 0; wi <= 2 * r; wi++)
						{
							// Ignore pixels that are outside the source glyph bounds
							var ii = i + wi - 2 * r;
							var jj = j + wj - 2 * r;
							if (ii < 0 || ii >= glyphBounds.Width || jj < 0 || jj >= glyphBounds.Height)
								continue;

							// Weighted alpha for this pixel
							var weighted = (byte)(elem[wj * elemStride + wi] * glyphData[glyphStride * (jj + glyphBounds.Top) + 4 * (ii + glyphBounds.Left) + 3]);
							if (first || weighted > alpha)
							{
								alpha = weighted;
								first = false;
							}
						}
					}

					if (alpha > 0)
					{
						var q = destStride * (j + s.Bounds.Top) + 4 * (i + s.Bounds.Left);
						var pmc = Util.PremultiplyAlpha(Color.FromArgb(alpha, color));
						dest[q] = pmc.B;
						dest[q + 1] = pmc.G;
						dest[q + 2] = pmc.R;
						dest[q + 3] = pmc.A;
					}
				}
			}

			s.Sheet.CommitBufferedData();
			return s;
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
