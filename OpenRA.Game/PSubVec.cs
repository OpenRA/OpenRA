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
	/// Sub-pixel coordinate vector (very fine)
	/// </summary>
	public struct PSubVec
	{
		public readonly int X, Y;

		public PSubVec(int x, int y) { X = x; Y = y; }
		public PSubVec(Size p) { X = p.Width; Y = p.Height; }

		public static readonly PSubVec Zero = new PSubVec(0, 0);
		public static PSubVec OneCell { get { return new PSubVec(Game.CellSize, Game.CellSize); } }

		public static explicit operator PSubVec(int2 a) { return new PSubVec(a.X, a.Y); }
		public static explicit operator PSubVec(float2 a) { return new PSubVec((int)a.X, (int)a.Y); }

		public static PSubVec operator +(PSubVec a, PSubVec b) { return new PSubVec(a.X + b.X, a.Y + b.Y); }
		public static PSubVec operator -(PSubVec a, PSubVec b) { return new PSubVec(a.X - b.X, a.Y - b.Y); }
		public static PSubVec operator *(int a, PSubVec b) { return new PSubVec(a * b.X, a * b.Y); }
		public static PSubVec operator *(PSubVec b, int a) { return new PSubVec(a * b.X, a * b.Y); }
		public static PSubVec operator /(PSubVec a, int b) { return new PSubVec(a.X / b, a.Y / b); }

		public static PSubVec operator -(PSubVec a) { return new PSubVec(-a.X, -a.Y); }

		public static bool operator ==(PSubVec me, PSubVec other) { return (me.X == other.X && me.Y == other.Y); }
		public static bool operator !=(PSubVec me, PSubVec other) { return !(me == other); }

		public static PSubVec Max(PSubVec a, PSubVec b) { return new PSubVec(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static PSubVec Min(PSubVec a, PSubVec b) { return new PSubVec(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public static int Dot(PSubVec a, PSubVec b) { return a.X * b.X + a.Y * b.Y; }

		public PSubVec Sign() { return new PSubVec(Math.Sign(X), Math.Sign(Y)); }
		public PSubVec Abs() { return new PSubVec(Math.Abs(X), Math.Abs(Y)); }
		public int LengthSquared { get { return X * X + Y * Y; } }
		public int Length { get { return (int)Math.Sqrt(LengthSquared); } }

		public float2 ToFloat2() { return new float2(X, Y); }
		public int2 ToInt2() { return new int2(X, Y); }
		public PVecInt ToPVecInt() { return new PVecInt(X / PSubPos.PerPx, Y / PSubPos.PerPx); }

		public PSubVec Clamp(Rectangle r)
		{
			return new PSubVec(
				Math.Min(r.Right, Math.Max(X, r.Left)),
				Math.Min(r.Bottom, Math.Max(Y, r.Top))
			);
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			PSubVec o = (PSubVec)obj;
			return o == this;
		}

		public override string ToString() { return "{0},{1}".F(X, Y); }
	}

	public static class PSubVecExtensions
	{
		/// <summary>
		/// Scales the float2 vector up to a subpixel vector.
		/// </summary>
		/// <param name="vec"></param>
		/// <returns></returns>
		public static PSubVec ToPSubVec(this float2 vec)
		{
			return new PSubVec((int)(vec.X * PSubPos.PerPx), (int)(vec.Y * PSubPos.PerPx));
		}

		public static PSubVec ToPSubVec(this PVecInt vec)
		{
			return new PSubVec((vec.X * PSubPos.PerPx), (vec.Y * PSubPos.PerPx));
		}
	}
}
