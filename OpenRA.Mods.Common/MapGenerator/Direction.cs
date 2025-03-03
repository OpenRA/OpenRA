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
using System.Collections.Immutable;
using System.Linq;

namespace OpenRA.Mods.Common.MapGenerator
{
	/// <summary>
	/// Utilities for simple directions and adjacency. Note that coordinate systems might not agree
	/// as to which directions are conceptually left/right or up/down.
	/// </summary>
	public static class Direction
	{
		/// <summary>No direction.</summary>
		public const int None = -1;

		/// <summary>+X ("right").</summary>
		public const int R = 0;

		/// <summary>+X+Y ("right down").</summary>
		public const int RD = 1;

		/// <summary>+Y ("down").</summary>
		public const int D = 2;

		/// <summary>-X+Y ("left down").</summary>
		public const int LD = 3;

		/// <summary>-X ("left").</summary>
		public const int L = 4;

		/// <summary>-X-Y ("left up").</summary>
		public const int LU = 5;

		/// <summary>-Y ("up").</summary>
		public const int U = 6;

		/// <summary>+X-Y ("right up").</summary>
		public const int RU = 7;

		/// <summary>Bitmask right.</summary>
		public const int MR = 1 << R;

		/// <summary>Bitmask right-down.</summary>
		public const int MRD = 1 << RD;

		/// <summary>Bitmask down.</summary>
		public const int MD = 1 << D;

		/// <summary>Bitmask left-down.</summary>
		public const int MLD = 1 << LD;

		/// <summary>Bitmask left.</summary>
		public const int ML = 1 << L;

		/// <summary>Bitmask left-up.</summary>
		public const int MLU = 1 << LU;

		/// <summary>Bitmask up.</summary>
		public const int MU = 1 << U;

		/// <summary>Bitmask right-up.</summary>
		public const int MRU = 1 << RU;

		/// <summary>Adjacent offsets with directions, excluding diagonals.</summary>
		public static readonly ImmutableArray<(int2, int)> Spread4D =
		[
			(new int2(1, 0), R),
			(new int2(0, 1), D),
			(new int2(-1, 0), L),
			(new int2(0, -1), U)
		];

		/// <summary>Adjacent offsets, excluding diagonals.</summary>
		public static readonly ImmutableArray<int2> Spread4 =
			Spread4D.Select(((int2 XY, int _) v) => v.XY).ToImmutableArray();

		/// <summary>
		/// Adjacent offsets, excluding diagonals. Assumes that CVec(1, 0)
		/// corresponds to Direction.R.
		/// </summary>
		public static readonly ImmutableArray<CVec> Spread4CVec =
			Spread4.Select(xy => new CVec(xy.X, xy.Y)).ToImmutableArray();

		/// <summary>Adjacent offsets with directions, including diagonals.</summary>
		public static readonly ImmutableArray<(int2, int)> Spread8D =
		[
			(new int2(1, 0), R),
			(new int2(1, 1), RD),
			(new int2(0, 1), D),
			(new int2(-1, 1), LD),
			(new int2(-1, 0), L),
			(new int2(-1, -1), LU),
			(new int2(0, -1), U),
			(new int2(1, -1), RU)
		];

		/// <summary>Adjacent offsets, including diagonals.</summary>
		public static readonly ImmutableArray<int2> Spread8 =
			Spread8D.Select(((int2 XY, int _) v) => v.XY).ToImmutableArray();

		/// <summary>
		/// Adjacent offsets, including diagonals. Assumes that CVec(1, 0)
		/// corresponds to Direction.R.
		/// </summary>
		public static readonly ImmutableArray<CVec> Spread8CVec =
			Spread8.Select(xy => new CVec(xy.X, xy.Y)).ToImmutableArray();

		/// <summary>Convert a non-none direction to an int2 offset.</summary>
		public static int2 ToInt2(int d)
		{
			if (d >= 0 && d < 8)
				return Spread8[d];
			else
				throw new ArgumentException("bad direction");
		}

		/// <summary>
		/// Convert a non-none direction to a CVec offset. Assumes that
		/// CVec(1, 0) corresponds to Direction.R.
		/// </summary>
		public static CVec ToCVec(int d)
		{
			if (d >= 0 && d < 8)
				return Spread8CVec[d];
			else
				throw new ArgumentException("bad direction");
		}

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a direction.
		/// Supplying a zero-offset will throw.
		/// </summary>
		public static int FromOffset(int dx, int dy)
		{
			if (dx > 0)
			{
				if (dy > 0)
					return RD;
				else if (dy < 0)
					return RU;
				else
					return R;
			}
			else if (dx < 0)
			{
				if (dy > 0)
					return LD;
				else if (dy < 0)
					return LU;
				else
					return L;
			}
			else
			{
				if (dy > 0)
					return D;
				else if (dy < 0)
					return U;
				else
					throw new ArgumentException("Bad direction");
			}
		}

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a direction.
		/// Supplying a zero-offset will throw.
		/// </summary>
		public static int FromInt2(int2 delta)
			=> FromOffset(delta.X, delta.Y);

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a direction.
		/// Supplying a zero-offset will throw. Assumes that CVec(1, 0)
		/// corresponds to Direction.R.
		/// </summary>
		public static int FromCVec(CVec delta)
			=> FromOffset(delta.X, delta.Y);

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a non-diagonal direction.
		/// Supplying a zero-offset will throw.
		/// </summary>
		public static int FromOffsetNonDiagonal(int dx, int dy)
		{
			if (dx - dy > 0 && dx + dy >= 0)
				return R;
			if (dy + dx > 0 && dy - dx >= 0)
				return D;
			if (-dx + dy > 0 && -dx - dy >= 0)
				return L;
			if (-dy - dx > 0 && -dy + dx >= 0)
				return U;
			throw new ArgumentException("bad direction");
		}

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a
		/// non-diagonal direction. Supplying a zero-offset will throw.
		/// </summary>
		public static int FromInt2NonDiagonal(int2 delta)
			=> FromOffsetNonDiagonal(delta.X, delta.Y);

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a
		/// non-diagonal direction. Supplying a zero-offset will throw. Assumes
		/// that CVec(1, 0) corresponds to Direction.R.
		/// </summary>
		public static int FromCVecNonDiagonal(CVec delta)
			=> FromOffsetNonDiagonal(delta.X, delta.Y);

		/// <summary>Return the opposite direction.</summary>
		public static int Reverse(int direction)
		{
			if (direction == None)
				return None;
			return direction ^ 4;
		}

		/// <summary>Convert a direction to a short string, like "None", "R", "RD", etc.</summary>
		public static string ToString(int direction)
		{
			switch (direction)
			{
				case None: return "None";
				case R: return "R";
				case RD: return "RD";
				case D: return "D";
				case LD: return "LD";
				case L: return "L";
				case LU: return "LU";
				case U: return "U";
				case RU: return "RU";
				default: throw new ArgumentException("bad direction");
			}
		}

		/// <summary>Count the number of set bits in a direction mask.</summary>
		public static int Count(int dm)
		{
			var count = 0;
			for (var m = dm; m != 0; m >>= 1)
				if ((m & 1) == 1)
					count++;

			return count;
		}

		/// <summary>Finds the only direction set in a direction mask or returns NONE.</summary>
		public static int FromMask(int mask)
		{
			switch (mask)
			{
				case MR: return R;
				case MRD: return RD;
				case MD: return D;
				case MLD: return LD;
				case ML: return L;
				case MLU: return LU;
				case MU: return U;
				case MRU: return RU;
				default: return None;
			}
		}

		/// <summary>True if diagonal, false if horizontal/vertical, throws otherwise.</summary>
		public static bool IsDiagonal(int direction)
		{
			switch (direction)
			{
				case R:
				case D:
				case L:
				case U:
					return false;
				case RD:
				case LD:
				case LU:
				case RU:
					return true;
				default:
					throw new ArgumentException("NONE or bad direction");
			}
		}
	}
}
