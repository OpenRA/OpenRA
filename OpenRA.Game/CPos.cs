#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Scripting;

namespace OpenRA
{
	public struct CPos : IScriptBindable, ILuaAdditionBinding, ILuaSubtractionBinding, ILuaEqualityBinding, ILuaTableBinding, IEquatable<CPos>
	{
		public readonly int X, Y;

		public CPos(int x, int y) { X = x; Y = y; }
		public static readonly CPos Zero = new CPos(0, 0);

		public static explicit operator CPos(int2 a) { return new CPos(a.X, a.Y); }

		public static CPos operator +(CVec a, CPos b) { return new CPos(a.X + b.X, a.Y + b.Y); }
		public static CPos operator +(CPos a, CVec b) { return new CPos(a.X + b.X, a.Y + b.Y); }
		public static CPos operator -(CPos a, CVec b) { return new CPos(a.X - b.X, a.Y - b.Y); }
		public static CVec operator -(CPos a, CPos b) { return new CVec(a.X - b.X, a.Y - b.Y); }

		public static bool operator ==(CPos me, CPos other) { return me.X == other.X && me.Y == other.Y; }
		public static bool operator !=(CPos me, CPos other) { return !(me == other); }

		public static CPos Max(CPos a, CPos b) { return new CPos(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static CPos Min(CPos a, CPos b) { return new CPos(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public bool Equals(CPos other) { return X == other.X && Y == other.Y; }
		public override bool Equals(object obj) { return obj is CPos && Equals((CPos)obj); }

		public override string ToString() { return X + "," + Y; }

		public MPos ToMPos(Map map)
		{
			return ToMPos(map.Grid.Type);
		}

		public MPos ToMPos(MapGridType gridType)
		{
			if (gridType == MapGridType.Rectangular)
				return new MPos(X, Y);

			// Convert from RectangularIsometric cell (x, y) position to rectangular map position (u, v)
			//  - The staggered rows make this fiddly (hint: draw a diagram!)
			// (a) Consider the relationships:
			//  - +1x (even -> odd) adds (0, 1) to (u, v)
			//  - +1x (odd -> even) adds (1, 1) to (u, v)
			//  - +1y (even -> odd) adds (-1, 1) to (u, v)
			//  - +1y (odd -> even) adds (0, 1) to (u, v)
			// (b) Therefore:
			//  - ax + by adds (a - b)/2 to u (only even increments count)
			//  - ax + by adds a + b to v
			var u = (X - Y) / 2;
			var v = X + Y;
			return new MPos(u, v);
		}

		#region Scripting interface

		public LuaValue Add(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			CPos a;
			CVec b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call CPos.Add(CPos, CVec) with invalid arguments ({0}, {1})".F(left.WrappedClrType().Name, right.WrappedClrType().Name));

			return new LuaCustomClrObject(a + b);
		}

		public LuaValue Subtract(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			CPos a;
			CVec b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call CPos.Subtract(CPos, CVec) with invalid arguments ({0}, {1})".F(left.WrappedClrType().Name, right.WrappedClrType().Name));

			return new LuaCustomClrObject(a - b);
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			CPos a, b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				return false;

			return a == b;
		}

		public LuaValue this[LuaRuntime runtime, LuaValue key]
		{
			get
			{
				switch (key.ToString())
				{
					case "X": return X;
					case "Y": return Y;
					default: throw new LuaException("CPos does not define a member '{0}'".F(key));
				}
			}

			set
			{
				throw new LuaException("CPos is read-only. Use CPos.New to create a new value");
			}
		}

		#endregion
	}
}