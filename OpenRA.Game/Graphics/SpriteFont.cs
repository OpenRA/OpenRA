#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.FileFormats;
using SharpFont;

namespace OpenRA.Graphics
{
	public class SpriteFont
	{
		int size;

		public SpriteFont(string name, int size)
		{
			this.size = size;

			face = library.NewFace(name, 0);
			face.SetPixelSizes((uint)size, (uint)size);

			glyphs = new Cache<Pair<char, Color>, GlyphInfo>(CreateGlyph, 
			         Pair<char,Color>.EqualityComparer);

			// setup a 1-channel SheetBuilder for our private use
			if (builder == null) builder = new SheetBuilder(TextureChannel.Alpha);

			PrecacheColor(Color.White);
			PrecacheColor(Color.Red);
		}

		void PrecacheColor(Color c)
		{
			// precache glyphs for U+0020 - U+007f
			for (var n = (char)0x20; n < (char)0x7f; n++)
				if (glyphs[Pair.New(n, c)] == null)
					throw new InvalidOperationException();
		}

		public void DrawText (string text, float2 location, Color c)
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
			return new int2((int)text.Split( '\n' ).Max( s => s.Sum(a => glyphs[Pair.New(a, Color.White)].Advance)), text.Split('\n').Count()*size);
		}

		Cache<Pair<char,Color>, GlyphInfo> glyphs;
		Face face;

		GlyphInfo CreateGlyph(Pair<char, Color> c)
		{
			uint index = face.GetCharIndex(c.First);
			face.LoadGlyph(index, LoadFlags.Default, LoadTarget.Normal);
			face.Glyph.RenderGlyph(RenderMode.Normal);

			var s = builder.Allocate(
				new Size((int)face.Glyph.Metrics.Width >> 6,
			         (int)face.Glyph.Metrics.Height >> 6));

			var g = new GlyphInfo
			{
				Sprite = s,
				Advance = (int)face.Glyph.Metrics.HorizontalAdvance / 64f,
				Offset = { X = face.Glyph.BitmapLeft, Y = -face.Glyph.BitmapTop }
			};

			unsafe
			{
				var p = (byte*)face.Glyph.Bitmap.Buffer;
				var dest = s.sheet.Data;
				var destStride = s.sheet.Size.Width * 4;

				for (var j = 0; j < s.size.Y; j++)
				{
					for (var i = 0; i < s.size.X; i++)
						if (p[i] != 0)
						{
							var q = destStride * (j + s.bounds.Top) + 4 * (i + s.bounds.Left);
							dest[q] = c.Second.B;
							dest[q + 1] = c.Second.G;
							dest[q + 2] = c.Second.R;
							dest[q + 3] = p[i];
						}

					p += face.Glyph.Bitmap.Pitch;
				}
			}
			return g;
		}

		static SpriteFont()
		{
			library = new Library();  
		}

		static Library library;
		static SheetBuilder builder;
	}

	class GlyphInfo
	{
		public float Advance;
		public int2 Offset;
		public Sprite Sprite;
	}
}
