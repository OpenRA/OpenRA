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
using OpenRA.Primitives;
using OpenRA.Support;
using SharpFont;

namespace OpenRA.Graphics
{
	public class SpriteFont
	{
		static Library library = new Library();
		static SheetBuilder builder;

		readonly int size;
		string name;

		public SpriteFont(string name, int size)
		{
			this.size = size;
			this.name = name;

			face = new Face(library, name);
			face.SetPixelSizes((uint)size, (uint)size);

			glyphs = new Cache<Pair<char, Color>, GlyphInfo>(CreateGlyph, Pair<char, Color>.EqualityComparer);

			// setup a SheetBuilder for our private use
			// TODO: SheetBuilder state is leaked between mod switches
			if (builder == null)
				builder = new SheetBuilder(SheetType.BGRA);

			PrecacheColor(Color.White);
			PrecacheColor(Color.Red);
		}

		void PrecacheColor(Color c)
		{
			using (new PerfTimer("PrecacheColor {0} {1}px {2}".F(name, size, c.Name)))
				for (var n = (char)0x20; n < (char)0x7f; n++)
					if (glyphs[Pair.New(n, c)] == null)
						throw new InvalidOperationException();
		}

		public void DrawText(string text, float2 location, Color c)
		{
			location.Y += size;	// baseline vs top

			var p = location;
			foreach (var s in text)
			{
				if (s == '\n')
				{
					location.Y += size;
					p = location;
					continue;
				}

				var g = glyphs[Pair.New(s, c)];
				Game.Renderer.RgbaSpriteRenderer.DrawSprite(g.Sprite,
					new float2(
						(int)Math.Round(p.X + g.Offset.X, 0),
						p.Y + g.Offset.Y));
				p.X += g.Advance;
			}
		}

		public void DrawTextWithContrast(string text, float2 location, Color fg, Color bg, int offset)
		{
			if (offset > 0)
			{
				DrawText(text, location + new float2(-offset, 0), bg);
				DrawText(text, location + new float2(offset, 0), bg);
				DrawText(text, location + new float2(0, -offset), bg);
				DrawText(text, location + new float2(0, offset), bg);
			}

			DrawText(text, location, fg);
		}

		public int2 Measure(string text)
		{
			var lines = text.Split('\n');
			return new int2((int)Math.Ceiling(lines.Max(s => s.Sum(a => glyphs[Pair.New(a, Color.White)].Advance))), lines.Length * size);
		}

		Cache<Pair<char, Color>, GlyphInfo> glyphs;
		Face face;

		GlyphInfo CreateGlyph(Pair<char, Color> c)
		{
			face.LoadChar(c.First, LoadFlags.Default, LoadTarget.Normal);
			face.Glyph.RenderGlyph(RenderMode.Normal);

			var size = new Size((int)face.Glyph.Metrics.Width >> 6, (int)face.Glyph.Metrics.Height >> 6);
			var s = builder.Allocate(size);

			var g = new GlyphInfo
			{
				Sprite = s,
				Advance = (int)face.Glyph.Metrics.HorizontalAdvance / 64f,
				Offset = { X = face.Glyph.BitmapLeft, Y = -face.Glyph.BitmapTop }
			};

			// A new bitmap is generated each time this property is accessed, so we do need to dispose it.
			using (var bitmap = face.Glyph.Bitmap)
				unsafe
				{
					var p = (byte*)bitmap.Buffer;
					var dest = s.Sheet.GetData();
					var destStride = s.Sheet.Size.Width * 4;

					for (var j = 0; j < s.Size.Y; j++)
					{
						for (var i = 0; i < s.Size.X; i++)
							if (p[i] != 0)
							{
								var q = destStride * (j + s.Bounds.Top) + 4 * (i + s.Bounds.Left);
								dest[q] = c.Second.B;
								dest[q + 1] = c.Second.G;
								dest[q + 2] = c.Second.R;
								dest[q + 3] = p[i];
							}

						p += bitmap.Pitch;
					}
				}

			s.Sheet.CommitData();

			return g;
		}
	}

	class GlyphInfo
	{
		public float Advance;
		public int2 Offset;
		public Sprite Sprite;
	}
}
