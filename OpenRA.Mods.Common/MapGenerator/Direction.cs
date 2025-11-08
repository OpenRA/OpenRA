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
	/// as to which directions are conceptually left/right or up/down. Direction is typically used
	/// with the CPos coordinate system.
	/// </summary>
	public enum Direction
	{
		/// <summary>No direction.</summary>
		None = -1,

		/// <summary>+X ("right").</summary>
		R = 0,

		/// <summary>+X+Y ("right down").</summary>
		RD = 1,

		/// <summary>+Y ("down").</summary>
		D = 2,

		/// <summary>-X+Y ("left down").</summary>
		LD = 3,

		/// <summary>-X ("left").</summary>
		L = 4,

		/// <summary>-X-Y ("left up").</summary>
		LU = 5,

		/// <summary>-Y ("up").</summary>
		U = 6,

		/// <summary>+X-Y ("right up").</summary>
		RU = 7,
	}

	[Flags]
	public enum DirectionMask
	{
		None = 0,

		/// <summary>Bitmask right.</summary>
		MR = 1 << Direction.R,

		/// <summary>Bitmask right-down.</summary>
		MRD = 1 << Direction.RD,

		/// <summary>Bitmask down.</summary>
		MD = 1 << Direction.D,

		/// <summary>Bitmask left-down.</summary>
		MLD = 1 << Direction.LD,

		/// <summary>Bitmask left.</summary>
		ML = 1 << Direction.L,

		/// <summary>Bitmask left-up.</summary>
		MLU = 1 << Direction.LU,

		/// <summary>Bitmask up.</summary>
		MU = 1 << Direction.U,

		/// <summary>Bitmask right-up.</summary>
		MRU = 1 << Direction.RU,
	}

	public static class DirectionExts
	{
		/// <summary>Adjacent offsets with directions, excluding diagonals.</summary>
		public static readonly ImmutableArray<(int2, Direction)> Spread4D =
		[
			(new int2(1, 0), Direction.R),
			(new int2(0, 1), Direction.D),
			(new int2(-1, 0), Direction.L),
			(new int2(0, -1), Direction.U)
		];

		/// <summary>Adjacent offsets, excluding diagonals.</summary>
		public static readonly ImmutableArray<int2> Spread4 =
			Spread4D.Select(((int2 XY, Direction _) v) => v.XY).ToImmutableArray();

		/// <summary>
		/// Adjacent offsets, excluding diagonals. Assumes that CVec(1, 0)
		/// corresponds to Direction.R.
		/// </summary>
		public static readonly ImmutableArray<CVec> Spread4CVec =
			Spread4.Select(xy => new CVec(xy.X, xy.Y)).ToImmutableArray();

		/// <summary>Adjacent offsets with directions, including diagonals.</summary>
		public static readonly ImmutableArray<(int2, Direction)> Spread8D =
		[
			(new int2(1, 0), Direction.R),
			(new int2(1, 1), Direction.RD),
			(new int2(0, 1), Direction.D),
			(new int2(-1, 1), Direction.LD),
			(new int2(-1, 0), Direction.L),
			(new int2(-1, -1), Direction.LU),
			(new int2(0, -1), Direction.U),
			(new int2(1, -1), Direction.RU)
		];

		/// <summary>Adjacent offsets, including diagonals.</summary>
		public static readonly ImmutableArray<int2> Spread8 =
			Spread8D.Select(((int2 XY, Direction _) v) => v.XY).ToImmutableArray();

		/// <summary>
		/// Adjacent offsets, including diagonals. Assumes that CVec(1, 0)
		/// corresponds to Direction.R.
		/// </summary>
		public static readonly ImmutableArray<CVec> Spread8CVec =
			Spread8.Select(xy => new CVec(xy.X, xy.Y)).ToImmutableArray();

		/// <summary>Convert a non-none direction to an int2 offset.</summary>
		public static int2 ToInt2(this Direction direction)
		{
			if (direction >= Direction.R && direction <= Direction.RU)
				return Spread8[(int)direction];
			else
				throw new ArgumentException("bad direction");
		}

		/// <summary>
		/// Convert a non-none direction to a CVec offset. Assumes that
		/// CVec(1, 0) corresponds to Direction.R.
		/// </summary>
		public static CVec ToCVec(this Direction direction)
		{
			if (direction >= Direction.R && direction <= Direction.RU)
				return Spread8CVec[(int)direction];
			else
				throw new ArgumentException("bad direction");
		}

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a direction.
		/// The direction is based purely on the signs of the inputs.
		/// Supplying a zero-offset will throw.
		/// </summary>
		public static Direction FromOffset(int dx, int dy)
		{
			if (dx > 0)
			{
				if (dy > 0)
					return Direction.RD;
				else if (dy < 0)
					return Direction.RU;
				else
					return Direction.R;
			}
			else if (dx < 0)
			{
				if (dy > 0)
					return Direction.LD;
				else if (dy < 0)
					return Direction.LU;
				else
					return Direction.L;
			}
			else
			{
				if (dy > 0)
					return Direction.D;
				else if (dy < 0)
					return Direction.U;
				else
					throw new ArgumentException("Bad direction");
			}
		}

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a direction.
		/// The direction with the closest angle wins. Keep inputs to 1000000 or less.
		/// Supplying a zero-offset will throw.
		/// </summary>
		public static Direction ClosestFrom(int dx, int dy)
		{
			if (dx == 0 && dy == 0)
				throw new ArgumentException("bad direction");

			var absX = Math.Abs(dx);
			var absY = Math.Abs(dy);
			var min = Math.Min(absX, absY);
			var max = Math.Max(absX, absY);

			// 408 / 985 is an approximation of tan(Pi / 8 radians), or tan(22.5 degrees)
			if (408 * max < 985 * min)
			{
				// Diagonal
				return FromOffset(dx, dy);
			}
			else
			{
				// Cardinal
				if (absX > absY)
					return FromOffsetNonDiagonal(dx, 0);
				else
					return FromOffsetNonDiagonal(0, dy);
			}
		}

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a direction.
		/// Supplying a zero-offset will throw.
		/// </summary>
		public static Direction FromInt2(int2 delta)
			=> FromOffset(delta.X, delta.Y);

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a direction.
		/// Supplying a zero-offset will throw. Assumes that CVec(1, 0)
		/// corresponds to Direction.R.
		/// </summary>
		public static Direction FromCVec(CVec delta)
			=> FromOffset(delta.X, delta.Y);

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a direction.
		/// Supplying a zero-offset will throw. Assumes that CVec(1, 0)
		/// corresponds to Direction.R.
		/// </summary>
		public static Direction ClosestFromCVec(CVec delta)
			=> ClosestFrom(delta.X, delta.Y);

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a non-diagonal direction.
		/// Supplying a zero-offset will throw.
		/// </summary>
		public static Direction FromOffsetNonDiagonal(int dx, int dy)
		{
			if (dx - dy > 0 && dx + dy >= 0)
				return Direction.R;
			if (dy + dx > 0 && dy - dx >= 0)
				return Direction.D;
			if (-dx + dy > 0 && -dx - dy >= 0)
				return Direction.L;
			if (-dy - dx > 0 && -dy + dx >= 0)
				return Direction.U;
			throw new ArgumentException("bad direction");
		}

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a
		/// non-diagonal direction. Supplying a zero-offset will throw.
		/// </summary>
		public static Direction FromInt2NonDiagonal(int2 delta)
			=> FromOffsetNonDiagonal(delta.X, delta.Y);

		/// <summary>
		/// Convert an offset (of arbitrary non-zero magnitude) to a
		/// non-diagonal direction. Supplying a zero-offset will throw. Assumes
		/// that CVec(1, 0) corresponds to Direction.R.
		/// </summary>
		public static Direction FromCVecNonDiagonal(CVec delta)
			=> FromOffsetNonDiagonal(delta.X, delta.Y);

		/// <summary>Return the opposite direction.</summary>
		public static Direction Reverse(this Direction direction)
		{
			if (direction != Direction.None)
				return (Direction)((int)direction ^ 4);
			else
				return Direction.None;
		}

		/// <summary>Converts the direction to a mask value.</summary>
		public static DirectionMask ToMask(this Direction direction)
		{
			if (direction >= Direction.R && direction <= Direction.RU)
				return (DirectionMask)(1 << (int)direction);
			else
				return DirectionMask.None;
		}
	}

	public static class DirectionMaskExts
	{
		/// <summary>Count the number of set bits (directions) in a direction mask.</summary>
		public static int Count(this DirectionMask mask)
		{
			return int.PopCount((int)mask);
		}

		/// <summary>Finds the only direction set in a direction mask or returns None.</summary>
		public static Direction ToDirection(this DirectionMask mask)
		{
			var d = int.Log2((int)mask);
			if (1 << d == (int)mask)
				return (Direction)d;
			else
				return Direction.None;
		}

		/// <summary>True if diagonal, false if horizontal/vertical, throws otherwise.</summary>
		public static bool IsDiagonal(this Direction direction)
		{
			if (direction >= Direction.R && direction <= Direction.RU)
				return ((int)direction & 1) == 1;
			else
				throw new ArgumentException("None or bad direction");
		}
	}
}
