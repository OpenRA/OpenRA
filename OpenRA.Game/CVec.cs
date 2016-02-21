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
using System.Drawing;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Scripting;

namespace OpenRA
{
	public struct CVec : IScriptBindable, ILuaAdditionBinding, ILuaSubtractionBinding, ILuaUnaryMinusBinding, ILuaEqualityBinding, ILuaTableBinding, IEquatable<CVec>
	{
		public readonly int X, Y;

		public CVec(int x, int y) { X = x; Y = y; }
		public static readonly CVec Zero = new CVec(0, 0);

		public static CVec operator +(CVec a, CVec b) { return new CVec(a.X + b.X, a.Y + b.Y); }
		public static CVec operator -(CVec a, CVec b) { return new CVec(a.X - b.X, a.Y - b.Y); }
		public static CVec operator *(int a, CVec b) { return new CVec(a * b.X, a * b.Y); }
		public static CVec operator *(CVec b, int a) { return new CVec(a * b.X, a * b.Y); }
		public static CVec operator /(CVec a, int b) { return new CVec(a.X / b, a.Y / b); }

		public static CVec operator -(CVec a) { return new CVec(-a.X, -a.Y); }

		public static bool operator ==(CVec me, CVec other) { return me.X == other.X && me.Y == other.Y; }
		public static bool operator !=(CVec me, CVec other) { return !(me == other); }

		public static CVec Max(CVec a, CVec b) { return new CVec(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y)); }
		public static CVec Min(CVec a, CVec b) { return new CVec(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)); }

		public static int Dot(CVec a, CVec b) { return a.X * b.X + a.Y * b.Y; }

		public CVec Sign() { return new CVec(Math.Sign(X), Math.Sign(Y)); }
		public CVec Abs() { return new CVec(Math.Abs(X), Math.Abs(Y)); }
		public int LengthSquared { get { return X * X + Y * Y; } }
		public int Length { get { return Exts.ISqrt(LengthSquared); } }

		public CVec Clamp(Rectangle r)
		{
			return new CVec(
				Math.Min(r.Right, Math.Max(X, r.Left)),
				Math.Min(r.Bottom, Math.Max(Y, r.Top)));
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode(); }

		public bool Equals(CVec other) { return other == this; }
		public override bool Equals(object obj) { return obj is CVec && Equals((CVec)obj); }

		public override string ToString() { return X + "," + Y; }

		public static readonly CVec[] Directions =
		{
			new CVec(-1, -1),
			new CVec(-1,  0),
			new CVec(-1,  1),
			new CVec(0, -1),
			new CVec(0,  1),
			new CVec(1, -1),
			new CVec(1,  0),
			new CVec(1,  1),
		};

		#region Scripting interface

		public LuaValue Add(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			CVec a, b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call CVec.Add(CVec, CVec) with invalid arguments ({0}, {1})".F(left.WrappedClrType().Name, right.WrappedClrType().Name));

			return new LuaCustomClrObject(a + b);
		}

		public LuaValue Subtract(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			CVec a, b;
			if (!left.TryGetClrValue(out a) || !right.TryGetClrValue(out b))
				throw new LuaException("Attempted to call CVec.Subtract(CVec, CVec) with invalid arguments ({0}, {1})".F(left.WrappedClrType().Name, right.WrappedClrType().Name));

			return new LuaCustomClrObject(a - b);
		}

		public LuaValue Minus(LuaRuntime runtime)
		{
			return new LuaCustomClrObject(-this);
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			CVec a, b;
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
					default: throw new LuaException("CVec does not define a member '{0}'".F(key));
				}
			}

			set
			{
				throw new LuaException("CVec is read-only. Use CVec.New to create a new value");
			}
		}

		#endregion
	}
}
