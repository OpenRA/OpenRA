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
using System.Linq;

namespace OpenRA.Primitives
{
	public readonly struct Polygon
	{
		public static readonly Polygon Empty = new Polygon(Rectangle.Empty);

		public readonly Rectangle BoundingRect;
		public readonly int2[] Vertices;
		readonly bool isRectangle;

		public Polygon(Rectangle bounds)
		{
			BoundingRect = bounds;
			Vertices = new[] { bounds.TopLeft, bounds.BottomLeft, bounds.BottomRight, bounds.TopRight };
			isRectangle = true;
		}

		public Polygon(int2[] vertices)
		{
			if (vertices != null && vertices.Length > 0)
			{
				Vertices = vertices;
				var left = int.MaxValue;
				var right = int.MinValue;
				var top = int.MaxValue;
				var bottom = int.MinValue;
				foreach (var p in vertices)
				{
					left = Math.Min(left, p.X);
					right = Math.Max(right, p.X);
					top = Math.Min(top, p.Y);
					bottom = Math.Max(bottom, p.Y);
				}

				BoundingRect = Rectangle.FromLTRB(left, top, right, bottom);
				isRectangle = false;
			}
			else
			{
				isRectangle = true;
				BoundingRect = Rectangle.Empty;
				Vertices = Exts.MakeArray(4, _ => int2.Zero);
			}
		}

		public bool IsEmpty => BoundingRect.IsEmpty;

		public bool Contains(int2 xy)
		{
			return isRectangle ? BoundingRect.Contains(xy) : Vertices.PolygonContains(xy);
		}

		public bool IntersectsWith(Rectangle rect)
		{
			var intersectsBoundingRect = BoundingRect.Left < rect.Right && BoundingRect.Right > rect.Left && BoundingRect.Top < rect.Bottom && BoundingRect.Bottom > rect.Top;
			if (isRectangle)
				return intersectsBoundingRect;

			// Easy case 1: Rect and bounding box don't intersect
			if (!intersectsBoundingRect)
				return false;

			// Easy case 2: Rect and bounding box intersect in a cross shape
			if ((rect.Left <= BoundingRect.Left && rect.Right >= BoundingRect.Right) || (rect.Top <= BoundingRect.Top && rect.Bottom >= BoundingRect.Bottom))
				return true;

			// Easy case 3: Corner of rect is inside the polygon
			if (Vertices.PolygonContains(rect.TopLeft) || Vertices.PolygonContains(rect.TopRight) || Vertices.PolygonContains(rect.BottomLeft) || Vertices.PolygonContains(rect.BottomRight))
				return true;

			// Easy case 4: Polygon vertex is inside rect
			if (Vertices.Any(p => rect.Contains(p)))
				return true;

			// Hard case: check intersection of every line segment pair
			var rectVertices = new[]
			{
				rect.TopLeft,
				rect.BottomLeft,
				rect.BottomRight,
				rect.TopRight
			};

			for (var i = 0; i < Vertices.Length; i++)
				for (var j = 0; j < 4; j++)
					if (Exts.LinesIntersect(Vertices[i], Vertices[(i + 1) % Vertices.Length], rectVertices[j], rectVertices[(j + 1) % 4]))
						return true;

			return false;
		}

		public override int GetHashCode()
		{
			var code = BoundingRect.GetHashCode();
			foreach (var v in Vertices)
				code = ((code << 5) + code) ^ v.GetHashCode();

			return code;
		}
	}
}
