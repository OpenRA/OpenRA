#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using OpenRA.Primitives;

namespace OpenRA
{
	[SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Mimic a built-in type alias.")]
	public readonly struct Int2 : IEquatable<Int2>
	{
		public readonly int X, Y;
		public Int2(int x, int y) { X = x; Y = y; }
		public Int2(Size p) { X = p.Width; Y = p.Height; }

		public static Int2 operator +(Int2 a, Int2 b) { return new Int2(a.X + b.X, a.Y + b.Y); }
		public static Int2 operator +(Int2 a, Size b) { return new Int2(a.X + b.Width, a.Y + b.Height); }
		public static Int2 operator -(Int2 a, Int2 b) { return new Int2(a.X - b.X, a.Y - b.Y); }
		public static Int2 operator *(int a, Int2 b) { return new Int2(a * b.X, a * b.Y); }
		public static Int2 operator *(Int2 b, int a) { return new Int2(a * b.X, a * b.Y); }
		public static Int2 operator /(Int2 a, int b) { return new Int2(a.X / b, a.Y / b); }

		public static Int2 operator -(Int2 a) { return new Int2(-a.X, -a.Y); }

		public static bool operator ==(Int2 me, Int2 other) { return me.X == other.X && me.Y == other.Y; }
		public static bool operator !=(Int2 me, Int2 other) { return !(me == other); }

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public bool Equals(Int2 other) { return this == other; }
		public override bool Equals(object obj) { return obj is Int2 && Equals((Int2)obj); }

		public override string ToString() { return X + "," + Y; }

		public Int2 Sign() { return new Int2(Math.Sign(X), Math.Sign(Y)); }
		public Int2 Abs() { return new Int2(Math.Abs(X), Math.Abs(Y)); }
		public int LengthSquared => X * X + Y * Y;
		public int Length => Exts.ISqrt(LengthSquared);

		public Int2 WithX(int newX)
		{
			return new Int2(newX, Y);
		}

		public Int2 WithY(int newY)
		{
			return new Int2(X, newY);
		}

		public static Int2 Max(Int2 a, Int2 b) { return new Int2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static Int2 Min(Int2 a, Int2 b) { return new Int2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public static readonly Int2 Zero = new Int2(0, 0);
		public float2 ToFloat2() { return new float2(X, Y); }

		// Change endianness of a uint32
		public static uint Swap(uint orig)
		{
			return ((orig & 0xff000000) >> 24) | ((orig & 0x00ff0000) >> 8) | ((orig & 0x0000ff00) << 8) | ((orig & 0x000000ff) << 24);
		}

		public static int Lerp(int a, int b, int mul, int div)
		{
			return a + (b - a) * mul / div;
		}

		public static Int2 Lerp(Int2 a, Int2 b, int mul, int div)
		{
			return a + (b - a) * mul / div;
		}

		public Int2 Clamp(Rectangle r)
		{
			return new Int2(Math.Min(r.Right, Math.Max(X, r.Left)),
							Math.Min(r.Bottom, Math.Max(Y, r.Top)));
		}

		public static int Dot(Int2 a, Int2 b) { return a.X * b.X + a.Y * b.Y; }
	}
}
