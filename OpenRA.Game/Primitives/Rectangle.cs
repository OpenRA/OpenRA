#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public struct Rectangle
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
			return left.Location == right.Location && left.Size == right.Size;
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

		public int Left { get { return X; } }
		public int Right { get { return X + Width; } }
		public int Top { get { return Y; } }
		public int Bottom { get { return Y + Height; } }
		public bool IsEmpty { get { return X == 0 && Y == 0 && Width == 0 && Height == 0; } }
		public int2 Location { get { return new int2(X, Y); } }
		public Size Size { get { return new Size(Width, Height); } }

		public bool Contains(int x, int y)
		{
			return x >= Left && x < Right && y >= Top && y < Bottom;
		}

		public bool Contains(int2 pt)
		{
			return Contains(pt.X, pt.Y);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Rectangle))
				return false;

			return this == (Rectangle)obj;
		}

		public override int GetHashCode()
		{
			return Height + Width ^ X + Y;
		}

		public bool IntersectsWith(Rectangle rect)
		{
			return Left < rect.Right && Right > rect.Left && Top < rect.Bottom && Bottom > rect.Top;
		}

		bool IntersectsWithInclusive(Rectangle r)
		{
			return Left <= r.Right && Right >= r.Left && Top <= r.Bottom && Bottom >= r.Top;
		}

		public static Rectangle Intersect(Rectangle a, Rectangle b)
		{
			if (!a.IntersectsWithInclusive(b))
				return Empty;

			return FromLTRB(Math.Max(a.Left, b.Left), Math.Max(a.Top, b.Top), Math.Min(a.Right, b.Right), Math.Min(a.Bottom, b.Bottom));
		}

		public bool Contains(Rectangle rect)
		{
			return rect == Intersect(this, rect);
		}

		public override string ToString()
		{
			return string.Format("{{X={0},Y={1},Width={2},Height={3}}}", X, Y, Width, Height);
		}
	}
}
