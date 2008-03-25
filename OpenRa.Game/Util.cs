using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace OpenRa.Game
{
	static class Util
	{
		static float[] channelSelect = { 0.75f, 0.25f, -0.25f, -0.75f };

		static float2 EncodeVertexAttributes(TextureChannel channel, int paletteLine)
		{
			return new float2(paletteLine / 16.0f, channelSelect[(int)channel]);
		}

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

		static Vertex MakeVertex(float2 o, int k, Sprite r, float2 attrib)
		{
			return new Vertex(
				KLerp( o, r.size, k ),
				r.FastMapTextureCoords(k), 
				attrib);
		}

		public static string[] ReadAllLines(Stream s)
		{
			List<string> result = new List<string>();
			using (StreamReader reader = new StreamReader(s))
				while (!reader.EndOfStream)
					result.Add(reader.ReadLine());

			return result.ToArray();
		}

		public static T[] MakeArray<T>(int count, Converter<int, T> f)
		{
			T[] result = new T[count];
			for (int i = 0; i < count; i++)
				result[i] = f(i);

			return result;
		}

		public static void CreateQuad(List<Vertex> vertices, List<ushort> indices, float2 o, Sprite r, int palette)
		{
			ushort offset = (ushort)vertices.Count;
            float2 attrib = EncodeVertexAttributes(r.channel, palette);

            Vertex[] v = new Vertex[]
            {
                Util.MakeVertex(o, 0, r, attrib),
                Util.MakeVertex(o, 1, r, attrib),
                Util.MakeVertex(o, 2, r, attrib),
                Util.MakeVertex(o, 3, r, attrib),
            };

            vertices.AddRange(v);

            ushort[] i = new ushort[]
            {
                offset, (ushort)(offset + 1), (ushort)(offset + 2), (ushort)(offset + 1), (ushort)(offset + 3), (ushort)(offset + 2)
            };

            indices.AddRange(i);
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
						uint * t = (uint*)bits.Scan0.ToPointer();
						int stride = bits.Stride >> 2;

						for (int j = 0; j < height; j++)
						{
							unsafe
							{
								uint* p = t;
								for (int i = 0; i < width; i++, p++)
									*p = (*p & ~mask) | ((mask & ((uint)*s++) << shift));
								t += stride;
							}
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
