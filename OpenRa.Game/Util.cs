using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;
using BluntDirectX.Direct3D;

namespace OpenRa.Game
{
	static class Util
	{
		static float2 EncodeVertexAttributes(TextureChannel channel, int paletteLine)
		{
			Converter<TextureChannel, float> channelEncoder = delegate(TextureChannel c)
			{
				switch (c)
				{
					case TextureChannel.Red: return 0.75f;
					case TextureChannel.Green: return 0.25f;
					case TextureChannel.Blue: return -0.25f;
					case TextureChannel.Alpha: return -0.75f;
					default:
						throw new ArgumentException();
				}
			};

			return new float2(paletteLine / 16.0f, channelEncoder(channel));
		}

		public static Vertex MakeVertex(float2 o, float2 uv, Sprite r, int palette)
		{
			return new Vertex(
				float2.Lerp( o, o + new float2(r.bounds.Size), uv ),
				r.MapTextureCoords(uv), 
				EncodeVertexAttributes(r.channel, palette));
		}

		static float Lerp(float a, float b, float t)
		{
			return (1 - t) * a + t * b;
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

		public static void CopyIntoChannel(Sprite dest, byte[] src)
		{
			for (int i = 0; i < dest.bounds.Width; i++)
				for (int j = 0; j < dest.bounds.Height; j++)
				{
					Point p = new Point(dest.bounds.Left + i, dest.bounds.Top + j);
					byte b = src[i + dest.bounds.Width * j];
					Color original = dest.sheet.bitmap.GetPixel(p.X, p.Y);
					dest.sheet.bitmap.SetPixel(p.X, p.Y, ReplaceChannel(original, dest.channel, b));
				}
		}

		static Color ReplaceChannel(Color o, TextureChannel channel, byte p)
		{
			switch (channel)
			{
				case TextureChannel.Red: return Color.FromArgb(o.A, p, o.G, o.B);
				case TextureChannel.Green: return Color.FromArgb(o.A, o.R, p, o.B);
				case TextureChannel.Blue: return Color.FromArgb(o.A, o.R, o.G, p);
				case TextureChannel.Alpha: return Color.FromArgb(p, o.R, o.G, o.B);

				default:
					throw new ArgumentException();
			}
		}
	}
}
