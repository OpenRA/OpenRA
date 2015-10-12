#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace OpenRA
{
	[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Mimic a built-in type alias.")]
	public struct int2
	{
		public readonly int X, Y;
		public int2(int x, int y) { this.X = x; this.Y = y; }
		public int2(Point p) { X = p.X; Y = p.Y; }
		public int2(Size p) { X = p.Width; Y = p.Height; }

		public static int2 operator +(int2 a, int2 b) { return new int2(a.X + b.X, a.Y + b.Y); }
		public static int2 operator -(int2 a, int2 b) { return new int2(a.X - b.X, a.Y - b.Y); }
		public static int2 operator *(int a, int2 b) { return new int2(a * b.X, a * b.Y); }
		public static int2 operator *(int2 b, int a) { return new int2(a * b.X, a * b.Y); }
		public static int2 operator /(int2 a, int b) { return new int2(a.X / b, a.Y / b); }

		public static int2 operator -(int2 a) { return new int2(-a.X, -a.Y); }

		public static bool operator ==(int2 me, int2 other) { return me.X == other.X && me.Y == other.Y; }
		public static bool operator !=(int2 me, int2 other) { return !(me == other); }

		public int2 Sign() { return new int2(Math.Sign(X), Math.Sign(Y)); }
		public int2 Abs() { return new int2(Math.Abs(X), Math.Abs(Y)); }
		public int LengthSquared { get { return X * X + Y * Y; } }
		public int Length { get { return Exts.ISqrt(LengthSquared); } }
		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public int2 WithX(int newX)
		{
			return new int2(newX, Y);
		}

		public int2 WithY(int newY)
		{
			return new int2(X, newY);
		}

		public static int2 Max(int2 a, int2 b) { return new int2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static int2 Min(int2 a, int2 b) { return new int2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public override bool Equals(object obj)
		{
			var o = obj as int2?;
			return o != null && o == this;
		}

		public static readonly int2 Zero = new int2(0, 0);
		public Point ToPoint() { return new Point(X, Y); }
		public PointF ToPointF() { return new PointF(X, Y); }
		public float2 ToFloat2() { return new float2(X, Y); }

		public override string ToString() { return "{0},{1}".F(X, Y); }

		// Change endianness of a uint32
		public static uint Swap(uint orig)
		{
			return (uint)((orig & 0xff000000) >> 24) | ((orig & 0x00ff0000) >> 8) | ((orig & 0x0000ff00) << 8) | ((orig & 0x000000ff) << 24);
		}

		public static int Lerp(int a, int b, int mul, int div)
		{
			return a + (b - a) * mul / div;
		}

		public static int2 Lerp(int2 a, int2 b, int mul, int div)
		{
			return a + (b - a) * mul / div;
		}

		public int2 Clamp(Rectangle r)
		{
			return new int2(Math.Min(r.Right, Math.Max(X, r.Left)),
							Math.Min(r.Bottom, Math.Max(Y, r.Top)));
		}

		public static int Dot(int2 a, int2 b) { return a.X * b.X + a.Y * b.Y; }
	}
}
