#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using Eluant;
using Eluant.ObjectBinding;
using OpenRA.Scripting;
using OpenRA.Support;

namespace OpenRA
{
	/// <summary>
	/// 1d world distance - 1024 units = 1 cell.
	/// </summary>
	public struct WDist : IComparable, IComparable<WDist>, IEquatable<WDist>, IScriptBindable, ILuaAdditionBinding, ILuaSubtractionBinding, ILuaEqualityBinding, ILuaTableBinding
	{
		public readonly int Range;
		public long RangeSquared { get { return (long)Range * (long)Range; } }

		public WDist(int r) { Range = r; }
		public static readonly WDist Zero = new WDist(0);
		public static WDist FromCells(int cells) { return new WDist(1024 * cells); }

		public static WDist operator +(WDist a, WDist b) { return new WDist(a.Range + b.Range); }
		public static WDist operator -(WDist a, WDist b) { return new WDist(a.Range - b.Range); }
		public static WDist operator -(WDist a) { return new WDist(-a.Range); }
		public static WDist operator /(WDist a, int b) { return new WDist(a.Range / b); }
		public static WDist operator *(WDist a, int b) { return new WDist(a.Range * b); }
		public static WDist operator *(int a, WDist b) { return new WDist(a * b.Range); }
		public static bool operator <(WDist a, WDist b) { return a.Range < b.Range; }
		public static bool operator >(WDist a, WDist b) { return a.Range > b.Range; }
		public static bool operator <=(WDist a, WDist b) { return a.Range <= b.Range; }
		public static bool operator >=(WDist a, WDist b) { return a.Range >= b.Range; }

		public static bool operator ==(WDist me, WDist other) { return me.Range == other.Range; }
		public static bool operator !=(WDist me, WDist other) { return !(me == other); }

		// Sampled a N-sample probability density function in the range [-1024..1024]
		// 1 sample produces a rectangular probability
		// 2 samples produces a triangular probability
		// ...
		// N samples approximates a true gaussian
		public static WDist FromPDF(MersenneTwister r, int samples)
		{
			return new WDist(Exts.MakeArray(samples, _ => r.Next(-1024, 1024))
				.Sum() / samples);
		}

		public static bool TryParse(string s, out WDist result)
		{
			result = WDist.Zero;

			if (string.IsNullOrEmpty(s))
				return false;

			s = s.ToLowerInvariant();
			var components = s.Split('c');
			var cell = 0;
			var subcell = 0;

			switch (components.Length)
			{
				case 2:
					if (!Exts.TryParseIntegerInvariant(components[0], out cell) ||
						!Exts.TryParseIntegerInvariant(components[1], out subcell))
						return false;
					break;
				case 1:
					if (!Exts.TryParseIntegerInvariant(components[0], out subcell))
						return false;
					break;
				default: return false;
			}

			// Propagate sign to fractional part
			if (cell < 0)
				subcell = -subcell;

			result = new WDist(1024 * cell + subcell);
			return true;
		}

		public override int GetHashCode() { return Range.GetHashCode(); }

		public bool Equals(WDist other) { return other == this; }
		public override bool Equals(object obj) { return obj is WDist && Equals((WDist)obj); }

		public int CompareTo(object obj)
		{
			if (!(obj is WDist))
				return 1;
			return Range.CompareTo(((WDist)obj).Range);
		}

		public int CompareTo(WDist other) { return Range.CompareTo(other.Range); }

		public override string ToString() { return Range.ToString(); }

		#region Scripting interface
		public LuaValue Add(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WDist a;
			WDist b;
			if (!left.TryGetClrValue<WDist>(out a) || !right.TryGetClrValue<WDist>(out b))
				throw new LuaException("Attempted to call WRange.Add(WRange, WRange) with invalid arguments.");

			return new LuaCustomClrObject(a + b);
		}

		public LuaValue Subtract(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WDist a;
			WDist b;
			if (!left.TryGetClrValue<WDist>(out a) || !right.TryGetClrValue<WDist>(out b))
				throw new LuaException("Attempted to call WRange.Subtract(WRange, WRange) with invalid arguments.");

			return new LuaCustomClrObject(a - b);
		}

		public LuaValue Equals(LuaRuntime runtime, LuaValue left, LuaValue right)
		{
			WDist a;
			WDist b;
			if (!left.TryGetClrValue<WDist>(out a) || !right.TryGetClrValue<WDist>(out b))
				throw new LuaException("Attempted to call WRange.Equals(WRange, WRange) with invalid arguments.");

			return a == b;
		}

		public LuaValue this[LuaRuntime runtime, LuaValue key]
		{
			get
			{
				switch (key.ToString())
				{
					case "Range": return Range;
					default: throw new LuaException("WPos does not define a member '{0}'".F(key));
				}
			}

			set
			{
				throw new LuaException("WRange is read-only. Use WRange.New to create a new value");
			}
		}
		#endregion
	}
}
