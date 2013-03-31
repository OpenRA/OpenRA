#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
	/// 3d World position - 1024 units = 1 cell.
	/// </summary>
	public struct WPos
	{
		public readonly int X, Y, Z;

		public WPos(int x, int y, int z) { X = x; Y = y; Z = z; }
		public WPos(WRange x, WRange y, WRange z) { X = x.Range; Y = y.Range; Z = z.Range; }

		public static readonly WPos Zero = new WPos(0, 0, 0);

		public static explicit operator WVec(WPos a) { return new WVec(a.X, a.Y, a.Z); }

		public static WPos operator +(WPos a, WVec b) { return new WPos(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
		public static WPos operator -(WPos a, WVec b) { return new WPos(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
		public static WVec operator -(WPos a, WPos b) { return new WVec(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }

		public static bool operator ==(WPos me, WPos other) { return (me.X == other.X && me.Y == other.Y && me.Z == other.Z); }
		public static bool operator !=(WPos me, WPos other) { return !(me == other); }

		public static WPos Average(params WPos[] list)
		{
			if (list == null || list.Length == 0)
				return WPos.Zero;

			var x = 0;
			var y = 0;
			var z = 0;
			foreach(var pos in list)
			{
				x += pos.X;
				y += pos.Y;
				z += pos.Z;
			}

			x /= list.Length;
			y /= list.Length;
			z /= list.Length;

			return new WPos(x,y,z);
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode(); }

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;

			WPos o = (WPos)obj;
			return o == this;
		}

		public override string ToString() { return "{0},{1},{2}".F(X, Y, Z); }
	}
}
