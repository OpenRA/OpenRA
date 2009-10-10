using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;

namespace OpenRa.Game.Graphics
{
	static class Util
	{
		static float2 KLerp(float2 o, float2 d, int k)
		{
			switch (k)
			{
				case 0: return o;
				case 1: return new float2(o.X + d.X, o.Y);
				case 2: return new float2(o.X, o.Y + d.Y);
				case 3: return new float2(o.X + d.X, o.Y + d.Y);
				default: throw new InvalidOperationException();
			}
		}

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

		public static void FastCreateQuad(Vertex[] vertices, ushort[] indices, float2 o, Sprite r, int palette, int nv, int ni)
		{
			float2 attrib = new float2(palette / 8.0f, channelSelect[(int)r.channel]);

			vertices[nv] = new Vertex(KLerp(o, r.size, 0), r.FastMapTextureCoords(0), attrib);
			vertices[nv + 1] = new Vertex(KLerp(o, r.size, 1), r.FastMapTextureCoords(1), attrib);
			vertices[nv + 2] = new Vertex(KLerp(o, r.size, 2), r.FastMapTextureCoords(2), attrib);
			vertices[nv + 3] = new Vertex(KLerp(o, r.size, 3), r.FastMapTextureCoords(3), attrib);

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
	}
}
