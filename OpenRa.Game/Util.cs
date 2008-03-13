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

		public static Vertex MakeVertex(float2 o, float2 uv, Sprite r, int palette)
		{
			return new Vertex(
				float2.Lerp( o, o + r.Size, uv ),
				r.MapTextureCoords(uv), 
				EncodeVertexAttributes(r.channel, palette));
		}

		static float Lerp(float a, float b, float t)
		{
			return (1 - t) * a + t * b;
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

		static float2[] uv = 
		{ 
			new float2( 0,0 ),
			new float2( 1,0 ),
			new float2( 0,1 ),
			new float2( 1,1 ),
		};

		public static void CreateQuad(List<Vertex> vertices, List<ushort> indices, float2 o, Sprite r, int palette)
		{
			ushort offset = (ushort)vertices.Count;

			foreach( float2 p in uv )
				vertices.Add(Util.MakeVertex(o, p, r, palette));

			indices.Add(offset);
			indices.Add((ushort)(offset + 1));
			indices.Add((ushort)(offset + 2));

			indices.Add((ushort)(offset + 1));
			indices.Add((ushort)(offset + 3));
			indices.Add((ushort)(offset + 2));
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
