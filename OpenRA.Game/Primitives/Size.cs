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
	public readonly struct Size : IEquatable<Size>
	{
		public readonly int Width;
		public readonly int Height;

		public static Size operator +(Size left, Size right)
		{
			return new Size(left.Width + right.Width, left.Height + right.Height);
		}

		public static bool operator ==(Size left, Size right)
		{
			return left.Width == right.Width && left.Height == right.Height;
		}

		public static bool operator !=(Size left, Size right)
		{
			return !(left == right);
		}

		public static Size operator -(Size sz1, Size sz2)
		{
			return new Size(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
		}

		public Size(int width, int height)
		{
			Width = width;
			Height = height;
		}

		public bool IsEmpty => Width == 0 && Height == 0;

		public bool Equals(Size other)
		{
			return this == other;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Size))
				return false;

			return this == (Size)obj;
		}

		public override int GetHashCode()
		{
			return Width ^ Height;
		}

		public override string ToString()
		{
			return $"{{Width={Width}, Height={Height}}}";
		}
	}
}
