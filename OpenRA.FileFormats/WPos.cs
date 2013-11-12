#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

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

		public static WPos Lerp(WPos a, WPos b, int mul, int div) { return a + (b - a) * mul / div; }

		public static WPos LerpQuadratic(WPos a, WPos b, WAngle pitch, int mul, int div)
		{
			// Start with a linear lerp between the points
			var ret = Lerp(a, b, mul, div);

			if (pitch.Angle == 0)
				return ret;

			// Add an additional quadratic variation to height
			// Attempts to avoid integer overflow by keeping the intermediate variables reasonably sized
			var offset = (int)(((((((long)(b - a).Length * mul) / div) * (div - mul)) / div) * pitch.Tan()) / 1024);
			return new WPos(ret.X, ret.Y, ret.Z + offset);
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode(); }

		public override bool Equals(object obj)
		{
			var o = obj as WPos?;
			return o != null && o == this;
		}

		public override string ToString() { return "{0},{1},{2}".F(X, Y, Z); }
	}

	public static class IEnumerableExtensions
	{
		public static WPos Average(this IEnumerable<WPos> source)
		{
			var length = source.Count();
			if (length == 0)
				return WPos.Zero;

			var x = 0L;
			var y = 0L;
			var z = 0L;
			foreach (var pos in source)
			{
				x += pos.X;
				y += pos.Y;
				z += pos.Z;
			}

			x /= length;
			y /= length;
			z /= length;

			return new WPos((int)x, (int)y, (int)z);
		}
	}
}
