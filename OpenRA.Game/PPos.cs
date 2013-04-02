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
	/// Pixel coordinate position in the world (fine).
	/// </summary>
	public struct PPos
	{
		public readonly int X, Y;

		public PPos(int x, int y) { X = x; Y = y; }

		public static readonly PPos Zero = new PPos(0, 0);
		public static PPos FromWPos(WPos pos)
		{
			return new PPos(Game.CellSize*pos.X/1024, Game.CellSize*pos.Y/1024);
		}

		// Temporary hack for things that throw away altitude and
		// cache screen positions directly. This can go once all
		// the callers understand world coordinates
		public static PPos FromWPosHackZ(WPos pos)
		{
			return new PPos(Game.CellSize*pos.X/1024, Game.CellSize*(pos.Y - pos.Z)/1024);
		}

		public static explicit operator PPos(int2 a) { return new PPos(a.X, a.Y); }

		public static explicit operator PVecInt(PPos a) { return new PVecInt(a.X, a.Y); }

		public static PPos operator +(PPos a, PVecInt b) { return new PPos(a.X + b.X, a.Y + b.Y); }
		public static PVecInt operator -(PPos a, PPos b) { return new PVecInt(a.X - b.X, a.Y - b.Y); }
		public static PPos operator -(PPos a, PVecInt b) { return new PPos(a.X - b.X, a.Y - b.Y); }

		public static bool operator ==(PPos me, PPos other) { return (me.X == other.X && me.Y == other.Y); }
		public static bool operator !=(PPos me, PPos other) { return !(me == other); }

		public static PPos Max(PPos a, PPos b) { return new PPos(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static PPos Min(PPos a, PPos b) { return new PPos(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public static PPos Lerp(PPos a, PPos b, int mul, int div)
		{
			return a + ((PVecInt)(b - a) * mul / div);
		}

		public static PPos Average(params PPos[] list)
		{
			if (list == null || list.Length == 0)
				throw new ArgumentException("PPos: Cannot calculate average of empty list.");

			var x = 0;
			var y = 0;
			foreach(var pos in list)
			{
				x += pos.X;
				y += pos.Y;
			}

			x /= list.Length;
			y /= list.Length;

			return new PPos(x,y);
		}

		public float2 ToFloat2() { return new float2(X, Y); }
		public int2 ToInt2() { return new int2(X, Y); }
		public CPos ToCPos() { return new CPos((int)(1f / Game.CellSize * X), (int)(1f / Game.CellSize * Y)); }
		public PSubPos ToPSubPos() { return new PSubPos(X * PSubPos.PerPx, Y * PSubPos.PerPx); }

		public PPos Clamp(Rectangle r)
		{
			return new PPos(Math.Min(r.Right, Math.Max(X, r.Left)),
							Math.Min(r.Bottom, Math.Max(Y, r.Top)));
		}

		public WPos ToWPos(int z)
		{
			return new WPos(1024*X/Game.CellSize,
			                1024*Y/Game.CellSize,
			                1024*z/Game.CellSize);
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			PPos o = (PPos)obj;
			return o == this;
		}

		public override string ToString() { return "{0},{1}".F(X, Y); }
	}
}
