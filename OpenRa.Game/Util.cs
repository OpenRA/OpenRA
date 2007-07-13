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
		public static float U(Sprite s, float u)
		{
			float u0 = (float)(s.origin.X + 0.5f) / (float)s.sheet.bitmap.Width;
			float u1 = (float)(s.origin.X + s.size.Width) / (float)s.sheet.bitmap.Width;

			return (u > 0) ? u1 : u0;
		}

		public static float V(Sprite s, float v)
		{
			float v0 = (float)(s.origin.Y + 0.5f) / (float)s.sheet.bitmap.Height;
			float v1 = (float)(s.origin.Y + s.size.Height) / (float)s.sheet.bitmap.Height;

			return (v > 0) ? v1 : v0;
		}

		public static float Constrain(float x, Range<float> range)
		{
			return x < range.Start ? range.Start : x > range.End ? range.End : x;
		}

		static PointF EncodeVertexAttributes(TextureChannel channel, int paletteLine)
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

			return new PointF(paletteLine / 16.0f, channelEncoder(channel));
		}

		public static Vertex MakeVertex(PointF o, float u, float v, Sprite r, int palette)
		{
			float x2 = o.X + r.size.Width;
			float y2 = o.Y + r.size.Height;

			PointF p = EncodeVertexAttributes(r.channel, palette);

			return new Vertex(Lerp(o.X, x2, u), Lerp(o.Y, y2, v), 0, U(r, u), V(r, v),
				p.X, p.Y);
		}

		static float Lerp(float a, float b, float t)
		{
			return (1 - t) * a + t * b;
		}

		public static void CreateQuad(List<Vertex> vertices, List<ushort> indices, PointF o, Sprite r, int palette)
		{
			ushort offset = (ushort)vertices.Count;

			vertices.Add(Util.MakeVertex(o, 0, 0, r, palette));
			vertices.Add(Util.MakeVertex(o, 1, 0, r, palette));
			vertices.Add(Util.MakeVertex(o, 0, 1, r, palette));
			vertices.Add(Util.MakeVertex(o, 1, 1, r, palette));

			indices.Add(offset);
			indices.Add((ushort)(offset + 1));
			indices.Add((ushort)(offset + 2));

			indices.Add((ushort)(offset + 1));
			indices.Add((ushort)(offset + 3));
			indices.Add((ushort)(offset + 2));
		}

		public static void CopyIntoChannel(Sprite dest, byte[] src)
		{
			for (int i = 0; i < dest.size.Width; i++)
				for (int j = 0; j < dest.size.Height; j++)
				{
					Point p = new Point(dest.origin.X + i, dest.origin.Y + j);
					byte b = src[i + dest.size.Width * j];
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
