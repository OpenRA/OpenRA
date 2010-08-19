#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.FileFormats;
using Tao.FreeType;

namespace OpenRA.Graphics
{
	public class SpriteFont
	{
		int size;

		public SpriteFont(string name, int size)
		{
			this.size = size;

			if (0 != FT.FT_New_Face(library, name, 0, out face))
				throw new InvalidOperationException("FT_New_Face failed");

			FT.FT_Set_Pixel_Sizes(face, 0, (uint)size);
			glyphs = new Cache<Pair<char, Color>, GlyphInfo>(CreateGlyph);

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

		public void DrawText( string text, float2 location, Color c )
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
						p.Y + g.Offset.Y), 
					"chrome");
				p.X += g.Advance;
			}
		}

		public int2 Measure(string text)
		{
			return new int2((int)text.Split( '\n' ).Max( s => s.Sum(a => glyphs[Pair.New(a, Color.White)].Advance)), text.Split('\n').Count()*size);
		}

		Cache<Pair<char,Color>, GlyphInfo> glyphs;
		IntPtr face;

		GlyphInfo CreateGlyph(Pair<char,Color> c)
		{
			var index = FT.FT_Get_Char_Index(face, (uint)c.First);
			FT.FT_Load_Glyph(face, index, FT.FT_LOAD_RENDER);

			var _face = (FT_FaceRec)Marshal.PtrToStructure(face, typeof(FT_FaceRec));
			var _glyph = (FT_GlyphSlotRec)Marshal.PtrToStructure(_face.glyph, typeof(FT_GlyphSlotRec));

			var s = builder.Allocate(new Size(_glyph.metrics.width >> 6, _glyph.metrics.height >> 6));

			var g = new GlyphInfo
			{
				Sprite = s,
				Advance = _glyph.metrics.horiAdvance / 64f,
				Offset = { X = _glyph.bitmap_left, Y = -_glyph.bitmap_top }
			};

			unsafe
			{
				var p = (byte*)_glyph.bitmap.buffer;
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

					p += _glyph.bitmap.pitch;
				}
			}

			return g;
		}

		static SpriteFont()
		{
			FT.FT_Init_FreeType(out library);
		}

		static IntPtr library;
		static SheetBuilder builder;
	}

	class GlyphInfo
	{
		public float Advance;
		public int2 Offset;
		public Sprite Sprite;
	}
}
