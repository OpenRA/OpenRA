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

namespace OpenRA.Primitives
{
	public readonly struct ReadOnlyRectangle : IEquatable<ReadOnlyRectangle>
	{
		public readonly int2 TopLeft;
		public readonly int2 BottomRight;
		public static readonly ReadOnlyRectangle Empty;

		public static ReadOnlyRectangle FromLTRB(int left, int top, int right, int bottom)
		{
			return new ReadOnlyRectangle(left, top, right - left, bottom - top);
		}

		public static ReadOnlyRectangle Union(ReadOnlyRectangle a, ReadOnlyRectangle b)
		{
			return FromLTRB(Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top), Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom));
		}

		public static bool operator ==(ReadOnlyRectangle left, ReadOnlyRectangle right)
		{
			return left.TopLeft == right.TopLeft && left.BottomRight == right.BottomRight;
		}

		public static bool operator !=(ReadOnlyRectangle left, ReadOnlyRectangle right)
		{
			return !(left == right);
		}

		public ReadOnlyRectangle(Rectangle r)
		{
			TopLeft = r.TopLeft;
			BottomRight = r.BottomRight;
		}

		public ReadOnlyRectangle(int x, int y, int width, int height)
		{
			TopLeft = new(x, y);
			BottomRight = new(x + width, y + height);
		}

		public ReadOnlyRectangle(int2 location, Size size)
		{
			TopLeft = new(location.X, location.Y);
			BottomRight = new(location.X + size.Width, location.Y + size.Height);
		}

		public readonly int Left => TopLeft.X;
		public readonly int Right => BottomRight.X;
		public readonly int Top => TopLeft.Y;
		public readonly int Bottom => BottomRight.Y;
		public readonly bool IsEmpty => TopLeft.X == 0 && TopLeft.Y == 0 && BottomRight.X == 0 && BottomRight.Y == 0;
		public readonly int2 Location => TopLeft;
		public readonly Size Size => new(BottomRight.X - TopLeft.X, BottomRight.Y - TopLeft.Y);

		public readonly int X => TopLeft.X;
		public readonly int Y => TopLeft.Y;
		public readonly int Width => BottomRight.X - TopLeft.X;
		public readonly int Height => BottomRight.Y - TopLeft.Y;
		public readonly int2 TopRight => new(BottomRight.X, TopLeft.Y);
		public readonly int2 BottomLeft => new(TopLeft.X, BottomRight.Y);

		public readonly bool Contains(int x, int y)
		{
			return x >= TopLeft.X && x < BottomRight.X && y >= TopLeft.Y && y < BottomRight.Y;
		}

		public readonly bool Contains(int2 pt)
		{
			return Contains(pt.X, pt.Y);
		}

		public readonly bool Equals(ReadOnlyRectangle other)
		{
			return this == other;
		}

		public override readonly bool Equals(object obj)
		{
			if (obj is not ReadOnlyRectangle)
				return false;

			return this == (ReadOnlyRectangle)obj;
		}

		public override readonly int GetHashCode()
		{
			return Height + Width ^ X + Y;
		}

		public readonly bool IntersectsWith(ReadOnlyRectangle rect)
		{
			return Left < rect.Right && Right > rect.Left && Top < rect.Bottom && Bottom > rect.Top;
		}

		readonly bool IntersectsWithInclusive(ReadOnlyRectangle r)
		{
			return Left <= r.Right && Right >= r.Left && Top <= r.Bottom && Bottom >= r.Top;
		}

		public static ReadOnlyRectangle Intersect(ReadOnlyRectangle a, ReadOnlyRectangle b)
		{
			if (!a.IntersectsWithInclusive(b))
				return Empty;

			return FromLTRB(Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top), Math.Min(a.Right, b.Right), Math.Min(a.Bottom, b.Bottom));
		}

		public readonly bool Contains(ReadOnlyRectangle rect)
		{
			return rect == Intersect(this, rect);
		}

		public static ReadOnlyRectangle operator *(int a, ReadOnlyRectangle b) { return new ReadOnlyRectangle(a * b.X, a * b.Y, a * b.Width, a * b.Height); }

		public override readonly string ToString()
		{
			return $"{X},{Y},{Width},{Height}";
		}
	}
}
