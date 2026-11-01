#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class RgbaColorRenderer
	{
		static readonly Vector3 Offset = new(0.5f, 0.5f, 0f);

		readonly SpriteRenderer parent;
		readonly Vertex[] vertices = new Vertex[4];

		public RgbaColorRenderer(SpriteRenderer parent)
		{
			this.parent = parent;
		}

		public void DrawLine(in Vector3 start, in Vector3 end, float width, Color startColor, Color endColor, BlendMode blendMode = BlendMode.Alpha)
		{
			var direction = end - start;
			var delta = direction / direction.AsVector2().Length();
			var corner = width / 2 * new Vector3(-delta.Y, delta.X, delta.Z);

			var sc = Util.PremultiplyAlpha(startColor).ToVector4();
			var ec = Util.PremultiplyAlpha(endColor).ToVector4();

			vertices[0] = new Vertex(start - corner + Offset, sc.X, sc.Y, sc.Z, sc.W, 0);
			vertices[1] = new Vertex(start + corner + Offset, sc.X, sc.Y, sc.Z, sc.W, 0);
			vertices[2] = new Vertex(end + corner + Offset, ec.X, ec.Y, ec.Z, ec.W, 0);
			vertices[3] = new Vertex(end - corner + Offset, ec.X, ec.Y, ec.Z, ec.W, 0);

			parent.DrawRGBAQuad(vertices, blendMode);
		}

		public void DrawLine(in Vector2 start, in Vector2 end, float width, Color color, BlendMode blendMode = BlendMode.Alpha)
		{
			DrawLine(start.AsVector3(), end.AsVector3(), width, color, blendMode);
		}

		public void DrawLine(in Vector3 start, in Vector3 end, float width, Color color, BlendMode blendMode = BlendMode.Alpha)
		{
			var direction = end - start;
			var delta = direction / direction.AsVector2().Length();
			var corner = width / 2 * new Vector3(-delta.Y, delta.X, 0);

			var c = Util.PremultiplyAlpha(color).ToVector4();

			vertices[0] = new Vertex(start - corner + Offset, c.X, c.Y, c.Z, c.W, 0);
			vertices[1] = new Vertex(start + corner + Offset, c.X, c.Y, c.Z, c.W, 0);
			vertices[2] = new Vertex(end + corner + Offset, c.X, c.Y, c.Z, c.W, 0);
			vertices[3] = new Vertex(end - corner + Offset, c.X, c.Y, c.Z, c.W, 0);
			parent.DrawRGBAQuad(vertices, blendMode);
		}

		/// <summary>
		/// Calculate the 2D intersection of two lines.
		/// Will behave badly if the lines are parallel.
		/// Z position is the average of a and b (ignores actual intersection point if it exists).
		/// </summary>
		static Vector3 IntersectionOf(in Vector3 a, in Vector2 da, in Vector3 b, in Vector2 db)
		{
			var crossA = Vector2.Cross(a.AsVector2(), da);
			var crossB = Vector2.Cross(b.AsVector2(), db);

			var num = da * crossB - db * crossA;
			var invD = 1f / Vector2.Cross(da, db);

			return new Vector3(num * invD, (a.Z + b.Z) * 0.5f);
		}

		void DrawDisconnectedLine(IEnumerable<Vector3> points, float width, Color color, BlendMode blendMode)
		{
			using (var e = points.GetEnumerator())
			{
				if (!e.MoveNext())
					return;

				var lastPoint = e.Current;
				while (e.MoveNext())
				{
					var point = e.Current;
					DrawLine(lastPoint, point, width, color, blendMode);
					lastPoint = point;
				}
			}
		}

		void DrawConnectedLine(ReadOnlySpan<Vector2> points, float width, Color color, bool closed, BlendMode blendMode)
		{
			if (points.Length < 2)
				return;

			var points3D = points.Length <= 1024 ? stackalloc Vector3[points.Length] : new Vector3[points.Length];
			for (var i = 0; i < points.Length; i++)
				points3D[i] = points[i].AsVector3();

			DrawConnectedLine(points3D, width, color, closed, blendMode);
		}

		void DrawConnectedLine(ReadOnlySpan<Vector3> points, float width, Color color, bool closed, BlendMode blendMode)
		{
			// Not a line
			if (points.Length < 2)
				return;

			// Single segment
			if (points.Length == 2)
			{
				DrawLine(points[0], points[1], width, color, blendMode);
				return;
			}

			var c = Util.PremultiplyAlpha(color).ToVector4();

			var start = points[0];
			var end = points[1];
			var delta = end - start;
			var dir = delta / delta.AsVector2().Length();
			var corner = width / 2 * new Vector3(-dir.Y, dir.X, dir.Z);
			var dir2 = dir.AsVector2();

			// Corners for start of line segment
			var ca = start - corner;
			var cb = start + corner;

			// Segment is part of closed loop
			if (closed)
			{
				var prev = points[^1];
				var prevDelta = start - prev;
				var prevDir = prevDelta / prevDelta.AsVector2().Length();
				var prevDir2 = prevDir.AsVector2();
				var prevCorner = width / 2 * new Vector3(-prevDir.Y, prevDir.X, prevDir.Z);
				ca = IntersectionOf(start - prevCorner, prevDir2, start - corner, dir2);
				cb = IntersectionOf(start + prevCorner, prevDir2, start + corner, dir2);
			}

			var limit = closed ? points.Length : points.Length - 1;
			for (var i = 0; i < limit; i++)
			{
				var next = points[(i + 2) % points.Length];
				var nextDelta = next - end;
				var nextDir = nextDelta / nextDelta.AsVector2().Length();
				var nextDir2 = nextDir.AsVector2();
				var nextCorner = width / 2 * new Vector3(-nextDir.Y, nextDir.X, nextDir.Z);

				// Vertices for the corners joining start-end to end-next
				var cc = closed || i < limit - 1 ? IntersectionOf(end + corner, dir2, end + nextCorner, nextDir2) : end + corner;
				var cd = closed || i < limit - 1 ? IntersectionOf(end - corner, dir2, end - nextCorner, nextDir2) : end - corner;

				// Fill segment
				vertices[0] = new Vertex(ca + Offset, c.X, c.Y, c.Z, c.W, 0);
				vertices[1] = new Vertex(cb + Offset, c.X, c.Y, c.Z, c.W, 0);
				vertices[2] = new Vertex(cc + Offset, c.X, c.Y, c.Z, c.W, 0);
				vertices[3] = new Vertex(cd + Offset, c.X, c.Y, c.Z, c.W, 0);
				parent.DrawRGBAQuad(vertices, blendMode);

				// Advance line segment
				end = next;
				dir2 = nextDir2;
				corner = nextCorner;

				ca = cd;
				cb = cc;
			}
		}

		public void DrawLine(IEnumerable<Vector3> points, float width, Color color, bool connectSegments = false, BlendMode blendMode = BlendMode.Alpha)
		{
			if (!connectSegments)
				DrawDisconnectedLine(points, width, color, blendMode);
			else
				DrawConnectedLine(points as Vector3[] ?? points.ToArray(), width, color, false, blendMode);
		}

		public void DrawPolygon(ReadOnlySpan<Vector3> vertices, float width, Color color, BlendMode blendMode = BlendMode.Alpha)
		{
			DrawConnectedLine(vertices, width, color, true, blendMode);
		}

		public void DrawPolygon(ReadOnlySpan<Vector2> vertices, float width, Color color, BlendMode blendMode = BlendMode.Alpha)
		{
			DrawConnectedLine(vertices, width, color, true, blendMode);
		}

		public void DrawRect(in Vector3 tl, in Vector3 br, float width, Color color, BlendMode blendMode = BlendMode.Alpha)
		{
			var tr = new Vector3(br.X, tl.Y, tl.Z);
			var bl = new Vector3(tl.X, br.Y, br.Z);
			DrawPolygon([tl, tr, br, bl], width, color, blendMode);
		}

		public void FillRect(in Vector3 tl, in Vector3 br, Color color, BlendMode blendMode = BlendMode.Alpha)
		{
			var tr = new Vector3(br.X, tl.Y, tl.Z);
			var bl = new Vector3(tl.X, br.Y, br.Z);
			FillRect(tl, tr, br, bl, color, blendMode);
		}

		public void FillRect(in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 d, Color color, BlendMode blendMode = BlendMode.Alpha)
		{
			var cv = Util.PremultiplyAlpha(color).ToVector4();

			vertices[0] = new Vertex(a + Offset, cv.X, cv.Y, cv.Z, cv.W, 0);
			vertices[1] = new Vertex(b + Offset, cv.X, cv.Y, cv.Z, cv.W, 0);
			vertices[2] = new Vertex(c + Offset, cv.X, cv.Y, cv.Z, cv.W, 0);
			vertices[3] = new Vertex(d + Offset, cv.X, cv.Y, cv.Z, cv.W, 0);
			parent.DrawRGBAQuad(vertices, blendMode);
		}

		public void FillRect(in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 d,
			Color topLeftColor, Color topRightColor, Color bottomRightColor, Color bottomLeftColor, BlendMode blendMode = BlendMode.Alpha)
		{
			vertices[0] = VertexWithColor(a + Offset, topLeftColor);
			vertices[1] = VertexWithColor(b + Offset, topRightColor);
			vertices[2] = VertexWithColor(c + Offset, bottomRightColor);
			vertices[3] = VertexWithColor(d + Offset, bottomLeftColor);

			parent.DrawRGBAQuad(vertices, blendMode);
		}

		static Vertex VertexWithColor(in Vector3 xyz, Color color)
		{
			var c = Util.PremultiplyAlpha(color).ToVector4();
			return new Vertex(xyz, c.X, c.Y, c.Z, c.W, 0);
		}

		public void FillEllipse(in Vector3 tl, in Vector3 br, Color color, BlendMode blendMode = BlendMode.Alpha)
		{
			// TODO: Create an ellipse polygon instead
			var a = (br.X - tl.X) / 2;
			var b = (br.Y - tl.Y) / 2;
			var xc = (br.X + tl.X) / 2;
			var yc = (br.Y + tl.Y) / 2;

			var height = br.Y - tl.Y;
			for (var y = tl.Y; y <= br.Y; y++)
			{
				var z = Util.Lerp(tl.Z, br.Z, (y - tl.Y) / height);
				var t = (y - yc) / b;
				var dx = a * (float)Math.Sqrt(1 - t * t);
				DrawLine(new Vector3(xc - dx, y, z), new Vector3(xc + dx, y, z), 1, color, blendMode);
			}
		}
	}
}
