using System;
using System.Collections.Generic;
using System.Text;
using OpenRa.FileFormats;
using System.Drawing;

namespace OpenRa.Game
{
	static class Util
	{
		public static float U(SheetRectangle<Sheet> s, float u)
		{
			float u0 = (float)(s.origin.X + 0.5f) / (float)s.sheet.bitmap.Width;
			float u1 = (float)(s.origin.X + s.size.Width) / (float)s.sheet.bitmap.Width;

			return (u > 0) ? u1 : u0;// (1 - u) * u0 + u * u1;
		}

		public static float V(SheetRectangle<Sheet> s, float v)
		{
			float v0 = (float)(s.origin.Y + 0.5f) / (float)s.sheet.bitmap.Height;
			float v1 = (float)(s.origin.Y + s.size.Height) / (float)s.sheet.bitmap.Height;

			return (v > 0) ? v1 : v0;// return (1 - v) * v0 + v * v1;
		}

		public static Vertex MakeVertex(PointF o, float u, float v, SheetRectangle<Sheet> r)
		{
			float x2 = o.X + r.size.Width;
			float y2 = o.Y + r.size.Height;

			return new Vertex(Lerp(o.X, x2, u), Lerp(o.Y, y2, v), 0, U(r, u), V(r, v));
		}

		static float Lerp(float a, float b, float t)
		{
			return (1 - t) * a + t * b;
		}

		public static void CreateQuad(List<Vertex> vertices, List<ushort> indices, PointF o, SheetRectangle<Sheet> r)
		{
			ushort offset = (ushort)vertices.Count;

			vertices.Add(Util.MakeVertex(o, 0, 0, r));
			vertices.Add(Util.MakeVertex(o, 1, 0, r));
			vertices.Add(Util.MakeVertex(o, 0, 1, r));
			vertices.Add(Util.MakeVertex(o, 1, 1, r));

			indices.Add(offset);
			indices.Add((ushort)(offset + 1));
			indices.Add((ushort)(offset + 2));

			indices.Add((ushort)(offset + 1));
			indices.Add((ushort)(offset + 3));
			indices.Add((ushort)(offset + 2));
		}
	}
}
