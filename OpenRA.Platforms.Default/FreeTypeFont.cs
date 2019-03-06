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

using OpenRA.Primitives;
using SharpFont;

namespace OpenRA.Platforms.Default
{
	public sealed class FreeTypeFont : IFont
	{
		static readonly Library Library = new Library();
		readonly Face face;

		public FreeTypeFont(byte[] data)
		{
			face = new Face(Library, data, 0);
		}

		public FontGlyph CreateGlyph(char c, int size, float deviceScale)
		{
			try
			{
				var scaledSize = (uint) (size * deviceScale);
				face.SetPixelSizes(scaledSize, scaledSize);
			
				face.LoadChar(c, LoadFlags.Default, LoadTarget.Normal);
				face.Glyph.RenderGlyph(RenderMode.Normal);

				var glyphSize = new Size((int)face.Glyph.Metrics.Width, (int)face.Glyph.Metrics.Height);

				var g = new FontGlyph
				{
					Advance = (float)face.Glyph.Metrics.HorizontalAdvance,
					Offset = new int2(face.Glyph.BitmapLeft, -face.Glyph.BitmapTop),
					Size = glyphSize,
					Data = new byte[glyphSize.Width * glyphSize.Height]
				};

				// A new bitmap is generated each time this property is accessed, so we do need to dispose it.
				using (var bitmap = face.Glyph.Bitmap)
				{
					unsafe
					{
						var p = (byte*)bitmap.Buffer;
						var k = 0;
						for (var j = 0; j < glyphSize.Height; j++)
						{
							for (var i = 0; i < glyphSize.Width; i++)
								g.Data[k++] = p[i];

							p += bitmap.Pitch;
						}
					}
				}

				return g;
			}
			catch (FreeTypeException)
			{
				return new FontGlyph
				{
					Offset = int2.Zero,
					Size = new Size(0, 0),
					Advance = 0,
					Data = null
				};
			}
		}

		public void Dispose()
		{
			face.Dispose();
		}
	}
}
