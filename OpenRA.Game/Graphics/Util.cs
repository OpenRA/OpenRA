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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenRA.FileFormats.Graphics;

namespace OpenRA.Graphics
{
	public static class Util
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

		static float[] channelSelect = { 0.75f, 0.25f, -0.25f, -0.75f };

		public static void FastCreateQuad(Vertex[] vertices, ushort[] indices, float2 o, Sprite r, int palette, int nv, int ni, float2 size)
		{
			var attrib = new float2(palette / (float)HardwarePalette.MaxPalettes, channelSelect[(int)r.channel]);

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
			var masks = new int[] { 2, 1, 0, 3 };	// hack, our channel order is nuts.
			var data = dest.sheet.Data;
			var srcStride = dest.bounds.Width;
			var destStride = dest.sheet.Size.Width * 4;
			var destOffset = destStride * dest.bounds.Top + dest.bounds.Left * 4 + masks[(int)dest.channel];
			var destSkip = destStride - 4 * srcStride;
			var height = dest.bounds.Height;

			var srcOffset = 0;
			for (var j = 0; j < height; j++)
			{
				for (int i = 0; i < srcStride; i++, srcOffset++)
				{
					data[destOffset] = src[srcOffset];
					destOffset += 4;
				}
				destOffset += destSkip;
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
		
		public static int LerpARGBColor(float t, int c1, int c2)
		{
			int a = LerpChannel(t, (c1 >> 24) & 255, (c2 >> 24) & 255);
			int r = LerpChannel(t, (c1 >> 16) & 255, (c2 >> 16) & 255);
			int g = LerpChannel(t, (c1 >> 8) & 255, (c2 >> 8) & 255);
			int b = LerpChannel(t, c1 & 255, c2 & 255);
			return (a << 24) | (r << 16) | (g << 8) | b;
		}

		public static int LerpChannel(float t, int a, int b)
		{
			return (int)((1 - t) * a + t * b);
		}
		
		public static int NextPowerOf2(int v)
		{
			--v;
			v |= v >> 1;
			v |= v >> 2;
			v |= v >> 4;
			v |= v >> 8;
			++v;
			return v;
		}
	}
}
