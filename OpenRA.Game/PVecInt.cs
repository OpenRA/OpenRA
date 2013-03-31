#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Drawing;

namespace OpenRA
{
	/// <summary>
	/// Pixel coordinate vector (fine; integer)
	/// </summary>
	public struct PVecInt
	{
		public readonly int X, Y;

		public PVecInt(int x, int y) { X = x; Y = y; }
		public PVecInt(Size p) { X = p.Width; Y = p.Height; }

		public static readonly PVecInt Zero = new PVecInt(0, 0);
		public static PVecInt OneCell { get { return new PVecInt(Game.CellSize, Game.CellSize); } }

		public static explicit operator PVecInt(int2 a) { return new PVecInt(a.X, a.Y); }

		public static PVecInt FromRadius(int r) { return new PVecInt(r, r); }

		public static PVecInt operator +(PVecInt a, PVecInt b) { return new PVecInt(a.X + b.X, a.Y + b.Y); }
		public static PVecInt operator -(PVecInt a, PVecInt b) { return new PVecInt(a.X - b.X, a.Y - b.Y); }
		public static PVecInt operator *(int a, PVecInt b) { return new PVecInt(a * b.X, a * b.Y); }
		public static PVecInt operator *(PVecInt b, int a) { return new PVecInt(a * b.X, a * b.Y); }
		public static PVecInt operator /(PVecInt a, int b) { return new PVecInt(a.X / b, a.Y / b); }

		public static PVecInt operator -(PVecInt a) { return new PVecInt(-a.X, -a.Y); }

		public static bool operator ==(PVecInt me, PVecInt other) { return (me.X == other.X && me.Y == other.Y); }
		public static bool operator !=(PVecInt me, PVecInt other) { return !(me == other); }

		public static PVecInt Max(PVecInt a, PVecInt b) { return new PVecInt(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static PVecInt Min(PVecInt a, PVecInt b) { return new PVecInt(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public static int Dot(PVecInt a, PVecInt b) { return a.X * b.X + a.Y * b.Y; }

		public PVecInt Sign() { return new PVecInt(Math.Sign(X), Math.Sign(Y)); }
		public PVecInt Abs() { return new PVecInt(Math.Abs(X), Math.Abs(Y)); }
		public int LengthSquared { get { return X * X + Y * Y; } }
		public int Length { get { return (int)Math.Sqrt(LengthSquared); } }

		public float2 ToFloat2() { return new float2(X, Y); }
		public int2 ToInt2() { return new int2(X, Y); }
		public CVec ToCVec() { return new CVec(X / Game.CellSize, Y / Game.CellSize); }

		public PVecInt Clamp(Rectangle r)
		{
			return new PVecInt(
				Math.Min(r.Right, Math.Max(X, r.Left)),
				Math.Min(r.Bottom, Math.Max(Y, r.Top))
			);
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			PVecInt o = (PVecInt)obj;
			return o == this;
		}

		public override string ToString() { return "{0},{1}".F(X, Y); }
	}
}
