#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace OpenRa.Graphics
{
	static class Util
	{
		public static string[] ReadAllLines(Stream s)
		{
			List<string> result = new List<string>();
			using (StreamReader reader = new StreamReader(s))
				while(!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					if( !string.IsNullOrEmpty( line ) && line[0] != '#' )
						result.Add( line );
				}

			return result.ToArray();
		}

		public static T[] MakeArray<T>(int count, Converter<int, T> f)
		{
			T[] result = new T[count];
			for (int i = 0; i < count; i++)
				result[i] = f(i);

			return result;
		}

		public static readonly int2[] directions =
			new int2[] {
				new int2( -1, -1 ),
				new int2( -1,  0 ),
				new int2( -1,  1 ),
				new int2(  0, -1 ),
				new int2(  0,  1 ),
				new int2(  1, -1 ),
				new int2(  1,  0 ),
				new int2(  1,  1 ),
			};

		static float[] channelSelect = { 0.75f, 0.25f, -0.25f, -0.75f };

		public static void FastCreateQuad(Vertex[] vertices, ushort[] indices, float2 o, Sprite r, int palette, int nv, int ni, float2 size)
		{
			float2 attrib = new float2(palette / 16.0f, channelSelect[(int)r.channel]);

			vertices[nv] = new Vertex(o, 
				r.FastMapTextureCoords(0), attrib);
			vertices[nv + 1] = new Vertex(new float2(o.X + size.X, o.Y), 
				r.FastMapTextureCoords(1), attrib);
			vertices[nv + 2] = new Vertex(new float2(o.X, o.Y + size.Y), 
				r.FastMapTextureCoords(2), attrib);
			vertices[nv + 3] = new Vertex(new float2(o.X + size.X, o.Y + size.Y), 
				r.FastMapTextureCoords(3), attrib);

			indices[ni] = (ushort)(nv);
			indices[ni + 1] = indices[ni + 3] = (ushort)(nv + 1);
			indices[ni + 2] = indices[ni + 5] = (ushort)(nv + 2);
			indices[ni + 4] = (ushort)(nv + 3);
		}

		public static void FastCopyIntoChannel(Sprite dest, byte[] src)
		{
			var bitmap = dest.sheet.Bitmap;
			BitmapData bits = null;
			uint[] channelMasks = { 0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000 };
			int[] shifts = { 16, 8, 0, 24 };

			uint mask = channelMasks[(int)dest.channel];
			int shift = shifts[(int)dest.channel];

			try
			{
				bits = bitmap.LockBits(dest.bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

				int width = dest.bounds.Width;
				int height = dest.bounds.Height;

				unsafe
				{
					fixed (byte* srcbase = &src[0])
					{
						byte* s = srcbase;
						uint* t = (uint*)bits.Scan0.ToPointer();
						int stride = bits.Stride >> 2;

						for (int j = 0; j < height; j++)
						{
							uint* p = t;
							for (int i = 0; i < width; i++, p++)
								*p = (*p & ~mask) | ((mask & ((uint)*s++) << shift));
							t += stride;
						}
					}
				}
			}
			finally
			{
				bitmap.UnlockBits(bits);
			}
		}

		public static Color Lerp(float t, Color a, Color b)
		{
			return Color.FromArgb(
				LerpChannel(t, a.A, b.A),
				LerpChannel(t, a.R, b.R),
				LerpChannel(t, a.G, b.G),
				LerpChannel(t, a.B, b.B));
		}

		public static int LerpChannel(float t, int a, int b)
		{
			return (int)((1 - t) * a + t * b);
		}
	}
}
