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
	public struct Rectangle : IEquatable<Rectangle>
	{
		// TODO: Make these readonly: this will require a lot of changes to the UI logic
		public int X;
		public int Y;
		public int Width;
		public int Height;
		public static readonly Rectangle Empty;

		public static Rectangle FromLTRB(int left, int top, int right, int bottom)
		{
			return new Rectangle(left, top, right - left, bottom - top);
		}

		public static Rectangle Union(Rectangle a, Rectangle b)
		{
			return FromLTRB(Math.Min(a.Left, b.Left), Math.Min(a.Top, b.Top), Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom));
		}

		public static bool operator ==(Rectangle left, Rectangle right)
		{
			return left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;
		}

		public static bool operator !=(Rectangle left, Rectangle right)
		{
			return !(left == right);
		}

		public Rectangle(int x, int y, int width, int height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public Rectangle(int2 location, Size size)
		{
			X = location.X;
			Y = location.Y;
			Width = size.Width;
			Height = size.Height;
		}

		public readonly int Left => X;
		public readonly int Right => X + Width;
		public readonly int Top => Y;
		public readonly int Bottom => Y + Height;
		public readonly bool IsEmpty => X == 0 && Y == 0 && Width == 0 && Height == 0;
		public readonly int2 Location => new(X, Y);
		public readonly Size Size => new(Width, Height);

		public readonly int2 TopLeft => Location;
		public readonly int2 TopRight => new(X + Width, Y);
		public readonly int2 BottomLeft => new(X, Y + Height);
		public readonly int2 BottomRight => new(X + Width, Y + Height);

		public readonly bool Contains(int x, int y)
		{
			return x >= Left && x < Right && y >= Top && y < Bottom;
		}

		public readonly bool Contains(int2 pt)
		{
			return Contains(pt.X, pt.Y);
		}

		public readonly bool Equals(Rectangle other)
		{
			return this == other;
		}

		public override readonly bool Equals(object obj)
		{
			if (obj is not Rectangle)
				return false;

			return this == (Rectangle)obj;
		}

		public override readonly int GetHashCode()
		{
			return Height + Width ^ X + Y;
		}

		public readonly bool IntersectsWith(Rectangle rect)
		{
			return Left < rect.Right && Right > rect.Left && Top < rect.Bottom && Bottom > rect.Top;
		}

		readonly bool IntersectsWithInclusive(Rectangle r)
		{
			return Left <= r.Right && Right >= r.Left && Top <= r.Bottom && Bottom >= r.Top;
		}

		public static Rectangle Intersect(Rectangle a, Rectangle b)
		{
			if (!a.IntersectsWithInclusive(b))
				return Empty;

			return FromLTRB(Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top), Math.Min(a.Right, b.Right), Math.Min(a.Bottom, b.Bottom));
		}

		public readonly bool Contains(Rectangle rect)
		{
			return rect == Intersect(this, rect);
		}

		public static Rectangle operator *(int a, Rectangle b) { return new Rectangle(a * b.X, a * b.Y, a * b.Width, a * b.Height); }

		public override readonly string ToString()
		{
			return $"{X},{Y},{Width},{Height}";
		}
	}
}
