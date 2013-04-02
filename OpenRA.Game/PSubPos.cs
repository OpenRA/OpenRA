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
	/// Sub-pixel coordinate position in the world (very fine).
	/// </summary>
	public struct PSubPos
	{
		public readonly int X, Y;

		public PSubPos(int x, int y) { X = x; Y = y; }

		public const int PerPx = 1024;

		public static readonly PSubPos Zero = new PSubPos(0, 0);

		public static explicit operator PSubPos(int2 a) { return new PSubPos(a.X, a.Y); }

		public static explicit operator PSubVec(PSubPos a) { return new PSubVec(a.X, a.Y); }

		public static PSubPos operator +(PSubPos a, PSubVec b) { return new PSubPos(a.X + b.X, a.Y + b.Y); }
		public static PSubVec operator -(PSubPos a, PSubPos b) { return new PSubVec(a.X - b.X, a.Y - b.Y); }
		public static PSubPos operator -(PSubPos a, PSubVec b) { return new PSubPos(a.X - b.X, a.Y - b.Y); }

		public static bool operator ==(PSubPos me, PSubPos other) { return (me.X == other.X && me.Y == other.Y); }
		public static bool operator !=(PSubPos me, PSubPos other) { return !(me == other); }

		public static PSubPos Max(PSubPos a, PSubPos b) { return new PSubPos(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static PSubPos Min(PSubPos a, PSubPos b) { return new PSubPos(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public static PSubPos Lerp(PSubPos a, PSubPos b, int mul, int div)
		{
			return a + ((PSubVec)(b - a) * mul / div);
		}

		public float2 ToFloat2() { return new float2(X, Y); }
		public int2 ToInt2() { return new int2(X, Y); }
		public PPos ToPPos() { return new PPos(X / PerPx, Y / PerPx); }
		public CPos ToCPos() { return ToPPos().ToCPos(); }

		public PSubPos Clamp(Rectangle r)
		{
			return new PSubPos(Math.Min(r.Right, Math.Max(X, r.Left)),
							Math.Min(r.Bottom, Math.Max(Y, r.Top)));
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			PSubPos o = (PSubPos)obj;
			return o == this;
		}

		public override string ToString() { return "{0},{1}".F(X, Y); }
	}
}
