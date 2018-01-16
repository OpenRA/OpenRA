#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;
using SharpFont;

namespace OpenRA.Graphics
{
	public sealed class SpriteFont : IDisposable
	{
		static readonly Library Library = new Library();

		readonly int size;
		readonly SheetBuilder builder;
		readonly Func<string, float> lineWidth;
		readonly Face face;
		readonly Cache<Pair<char, Color>, GlyphInfo> glyphs;

		float deviceScale;

		public SpriteFont(string name, byte[] data, int size, float scale, SheetBuilder builder)
		{
			if (builder.Type != SheetType.BGRA)
				throw new ArgumentException("The sheet builder must create BGRA sheets.", "builder");

			deviceScale = scale;
			this.size = size;
			this.builder = builder;

			face = new Face(Library, data, 0);
			face.SetPixelSizes((uint)(size * deviceScale), (uint)(size * deviceScale));

			glyphs = new Cache<Pair<char, Color>, GlyphInfo>(CreateGlyph, Pair<char, Color>.EqualityComparer);

			// PERF: Cache these delegates for Measure calls.
			Func<char, float> characterWidth = character => glyphs[Pair.New(character, Color.White)].Advance;
			lineWidth = line => line.Sum(characterWidth) / deviceScale;

			if (size <= 24)
				PrecacheColor(Color.White, name);
		}

		public void SetScale(float scale)
		{
			deviceScale = scale;
			face.SetPixelSizes((uint)(size * deviceScale), (uint)(size * deviceScale));
			glyphs.Clear();
		}

		void PrecacheColor(Color c, string name)
		{
			using (new PerfTimer("PrecacheColor {0} {1}px {2}".F(name, size, c.Name)))
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
			DrawTextWithContrast(text, location, fg, WidgetUtils.GetContrastColor(fg, bgDark, bgLight), offset);
		}

		public void DrawTextWithShadow(string text, float2 location, Color fg, Color bg, int offset)
		{
			if (offset != 0)
				DrawText(text, location + new float2(offset, offset), bg);

			DrawText(text, location, fg);
		}

		public void DrawTextWithShadow(string text, float2 location, Color fg, Color bgDark, Color bgLight, int offset)
		{
			DrawTextWithShadow(text, location, fg, WidgetUtils.GetContrastColor(fg, bgDark, bgLight), offset);
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
			try
			{
				face.LoadChar(c.First, LoadFlags.Default, LoadTarget.Normal);
			}
			catch (FreeTypeException)
			{
				return new GlyphInfo
				{
					Sprite = null,
					Advance = 0,
					Offset = int2.Zero
				};
			}

			face.Glyph.RenderGlyph(RenderMode.Normal);

			var size = new Size((int)face.Glyph.Metrics.Width, (int)face.Glyph.Metrics.Height);
			var s = builder.Allocate(size);

			var g = new GlyphInfo
			{
				Sprite = s,
				Advance = (float)face.Glyph.Metrics.HorizontalAdvance,
				Offset = new int2(face.Glyph.BitmapLeft, -face.Glyph.BitmapTop)
			};

			// A new bitmap is generated each time this property is accessed, so we do need to dispose it.
			using (var bitmap = face.Glyph.Bitmap)
			{
				unsafe
				{
					var p = (byte*)bitmap.Buffer;
					var dest = s.Sheet.GetData();
					var destStride = s.Sheet.Size.Width * 4;

					for (var j = 0; j < s.Size.Y; j++)
					{
						for (var i = 0; i < s.Size.X; i++)
						{
							if (p[i] != 0)
							{
								var q = destStride * (j + s.Bounds.Top) + 4 * (i + s.Bounds.Left);
								var pmc = Util.PremultiplyAlpha(Color.FromArgb(p[i], c.Second));

								dest[q] = pmc.B;
								dest[q + 1] = pmc.G;
								dest[q + 2] = pmc.R;
								dest[q + 3] = pmc.A;
							}
						}

						p += bitmap.Pitch;
					}
				}
			}

			s.Sheet.CommitBufferedData();

			return g;
		}

		public void Dispose()
		{
			face.Dispose();
		}
	}

	class GlyphInfo
	{
		public float Advance;
		public int2 Offset;
		public Sprite Sprite;
	}
}
