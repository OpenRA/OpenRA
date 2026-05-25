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

namespace OpenRA.Widgets
{
	public struct EdgeInsets : IEquatable<EdgeInsets>
	{
		public int Top, Right, Bottom, Left;

		public readonly int Horizontal => Left + Right;
		public readonly int Vertical => Top + Bottom;

		public static readonly EdgeInsets Zero = new(0);

		public EdgeInsets(int all)
		{
			Top = Right = Bottom = Left = all;
		}

		public EdgeInsets(int vertical, int horizontal)
		{
			Top = Bottom = vertical;
			Left = Right = horizontal;
		}

		public EdgeInsets(int top, int right, int bottom, int left)
		{
			Top = top;
			Right = right;
			Bottom = bottom;
			Left = left;
		}

		public static bool TryParse(string value, out EdgeInsets result)
		{
			result = Zero;

			if (string.IsNullOrWhiteSpace(value))
				return false;

			var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			switch (parts.Length)
			{
				case 1:
					if (Exts.TryParseInt32Invariant(parts[0], out var all))
					{
						result = new EdgeInsets(all);
						return true;
					}

					return false;
				case 2:
					if (Exts.TryParseInt32Invariant(parts[0], out var vertical) &&
						Exts.TryParseInt32Invariant(parts[1], out var horizontal))
					{
						result = new EdgeInsets(vertical, horizontal);
						return true;
					}

					return false;
				case 4:
					if (Exts.TryParseInt32Invariant(parts[0], out var top) &&
						Exts.TryParseInt32Invariant(parts[1], out var right) &&
						Exts.TryParseInt32Invariant(parts[2], out var bottom) &&
						Exts.TryParseInt32Invariant(parts[3], out var left))
					{
						result = new EdgeInsets(top, right, bottom, left);
						return true;
					}

					return false;
				default:
					return false;
			}
		}

		public readonly bool Equals(EdgeInsets other)
		{
			return Top == other.Top && Right == other.Right && Bottom == other.Bottom && Left == other.Left;
		}

		public override readonly bool Equals(object obj)
		{
			return obj is EdgeInsets other && Equals(other);
		}

		public override readonly int GetHashCode()
		{
			return HashCode.Combine(Top, Right, Bottom, Left);
		}

		public static bool operator ==(EdgeInsets left, EdgeInsets right) => left.Equals(right);
		public static bool operator !=(EdgeInsets left, EdgeInsets right) => !left.Equals(right);

		public override readonly string ToString()
		{
			if (Top == Right && Right == Bottom && Bottom == Left)
				return $"{Top}";
			if (Top == Bottom && Left == Right)
				return $"{Top}, {Left}";
			return $"{Top}, {Right}, {Bottom}, {Left}";
		}
	}
}
