#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Scripting;

namespace OpenRA
{
	public readonly struct WPos : IScriptBindable, ILuaAdditionBinding, ILuaSubtractionBinding, ILuaEqualityBinding, ILuaTableBinding, IEquatable<WPos>
	{
		public readonly int X, Y, Z;

		public WPos(int x, int y, int z) { X = x; Y = y; Z = z; }
		public WPos(WDist x, WDist y, WDist z) { X = x.Length; Y = y.Length; Z = z.Length; }

		public static readonly WPos Zero = new WPos(0, 0, 0);

		public static explicit operator WVec(in WPos a) { return new WVec(a.X, a.Y, a.Z); }

		public static WPos operator +(in WPos a, in WVec b) { return new WPos(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
		public static WPos operator -(in WPos a, in WVec b) { return new WPos(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
		public static WVec operator -(in WPos a, in WPos b) { return new WVec(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }

		public static bool operator ==(in WPos me, in WPos other) { return me.X == other.X && me.Y == other.Y && me.Z == other.Z; }
		public static bool operator !=(in WPos me, in WPos other) { return !(me == other); }

		/// <summary>
		/// Returns the linear interpolation between points 'a' and 'b'
		/// </summary>
		public static WPos Lerp(in WPos a, in WPos b, int mul, int div) { return a + (b - a) * mul / div; }

		/// <summary>
		/// Returns the linear interpolation between points 'a' and 'b'
		/// </summary>
		public static WPos Lerp(in WPos a, in WPos b, long mul, long div)
		{
			// The intermediate variables may need more precision than
			// an int can provide, so we can't use WPos.
			var x = (int)(a.X + (b.X - a.X) * mul / div);
			var y = (int)(a.Y + (b.Y - a.Y) * mul / div);
			var z = (int)(a.Z + (b.Z - a.Z) * mul / div);

			return new WPos(x, y, z);
		}

		public static WPos LerpQuadratic(in WPos a, in WPos b, WAngle pitch, int mul, int div)
		{
			// Start with a linear lerp between the points
			var ret = Lerp(a, b, mul, div);

			if (pitch.Angle == 0)
				return ret;

			// Add an additional quadratic variation to height
			// Uses decimal to avoid integer overflow
			var offset = (decimal)(b - a).Length * pitch.Tan() * mul * (div - mul) / (1024 * div * div);
			var clampedOffset = (int)(offset + (decimal)ret.Z).Clamp<decimal>((decimal)int.MinValue, (decimal)int.MaxValue);

			return new WPos(ret.X, ret.Y, clampedOffset);
		}

		public override int GetHashCode() { return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode(); }

		public bool Equals(WPos other) { return other == this; }
		public override bool Equals(object obj) { return obj is WPos && Equals((WPos)obj); }

		public override string ToString() { return X + "," + Y + "," + Z; }

		#region Scripting interface

		public LuaValue Add(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out WPos a) || !right.TryGetClrValue(out WVec b))
				throw new LuaException($"Attempted to call WPos.Add(WPos, WVec) with invalid arguments ({left.WrappedClrType().Name}, {right.WrappedClrType().Name})");

			return new LuaCustomClrObject(a + b);
		}

		public LuaValue Subtract(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			var rightType = right.WrappedClrType();
			if (!left.TryGetClrValue(out WPos a))
				throw new LuaException($"Attempted to call WPos.Subtract(WPos, (WPos|WVec)) with invalid arguments ({left.WrappedClrType().Name}, {rightType.Name})");

			if (rightType == typeof(WPos))
			{
				right.TryGetClrValue(out WPos b);
				return new LuaCustomClrObject(a - b);
			}
			else if (rightType == typeof(WVec))
			{
				right.TryGetClrValue(out WVec b);
				return new LuaCustomClrObject(a - b);
			}

			throw new LuaException($"Attempted to call WPos.Subtract(WPos, (WPos|WVec)) with invalid arguments ({left.WrappedClrType().Name}, {rightType.Name})");
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			if (!left.TryGetClrValue(out WPos a) || !right.TryGetClrValue(out WPos b))
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
					case "Z": return Z;
					default: throw new LuaException($"WPos does not define a member '{key}'");
				}
			}

			set => throw new LuaException("WPos is read-only. Use WPos.New to create a new value");
		}

		#endregion
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
