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
	/// Cell coordinate position in the world (coarse).
	/// </summary>
	public struct CPos
	{
		public readonly int X, Y;

		public CPos(int x, int y) { X = x; Y = y; }
		public static readonly CPos Zero = new CPos(0, 0);

		public static explicit operator CPos(int2 a) { return new CPos(a.X, a.Y); }

		public static CPos operator +(CVec a, CPos b) { return new CPos(a.X + b.X, a.Y + b.Y); }
		public static CPos operator +(CPos a, CVec b) { return new CPos(a.X + b.X, a.Y + b.Y); }
		public static CPos operator -(CPos a, CVec b) { return new CPos(a.X - b.X, a.Y - b.Y); }

		public static CVec operator -(CPos a, CPos b) { return new CVec(a.X - b.X, a.Y - b.Y); }

		public static bool operator ==(CPos me, CPos other) { return (me.X == other.X && me.Y == other.Y); }
		public static bool operator !=(CPos me, CPos other) { return !(me == other); }

		public static CPos Max(CPos a, CPos b) { return new CPos(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static CPos Min(CPos a, CPos b) { return new CPos(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public float2 ToFloat2() { return new float2(X, Y); }
		public int2 ToInt2() { return new int2(X, Y); }

		public WPos CenterPosition { get { return new WPos(1024 * X + 512, 1024 * Y + 512, 0); } }
		public WPos TopLeft { get { return new WPos(1024 * X, 1024 * Y, 0); } }
		public WPos BottomRight { get { return new WPos(1024 * X + 1023, 1024 * Y + 1023, 0); } }

		public CPos Clamp(Rectangle r)
		{
			return new CPos(Math.Min(r.Right, Math.Max(X, r.Left)),
							Math.Min(r.Bottom, Math.Max(Y, r.Top)));
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public override bool Equals(object obj)
		{
			var o = obj as CPos?;
			return o != null && o == this;
		}

		public override string ToString() { return "{0},{1}".F(X, Y); }

	}

	public static class RectangleExtensions
	{
		public static CPos TopLeftAsCPos(this Rectangle r) { return new CPos(r.Left, r.Top); }
		public static CPos BottomRightAsCPos(this Rectangle r) { return new CPos(r.Right, r.Bottom); }
	}

	public static class WorldCoordinateExtensions
	{
		public static CPos ToCPos(this WPos a) { return new CPos(a.X / 1024, a.Y / 1024); }
		public static CVec ToCVec(this WVec a) { return new CVec(a.X / 1024, a.Y / 1024); }
	}
}