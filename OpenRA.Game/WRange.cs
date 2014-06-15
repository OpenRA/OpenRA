#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Support;

namespace OpenRA
{
	/// <summary>
	/// 1d world distance - 1024 units = 1 cell.
	/// </summary>
	public struct WRange : IComparable, IComparable<WRange>, IEquatable<WRange>
	{
		public readonly int Range;

		public WRange(int r) { Range = r; }
		public static readonly WRange Zero = new WRange(0);
		public static WRange FromCells(int cells) { return new WRange(1024*cells); }

		public static WRange operator +(WRange a, WRange b) { return new WRange(a.Range + b.Range); }
		public static WRange operator -(WRange a, WRange b) { return new WRange(a.Range - b.Range); }
		public static WRange operator -(WRange a) { return new WRange(-a.Range); }
		public static WRange operator /(WRange a, int b) { return new WRange(a.Range / b); }
		public static WRange operator *(WRange a, int b) { return new WRange(a.Range * b); }
		public static WRange operator *(int a, WRange b) { return new WRange(a * b.Range); }
		public static bool operator <(WRange a, WRange b) { return a.Range < b.Range; }
		public static bool operator >(WRange a, WRange b) { return a.Range > b.Range; }
		public static bool operator <=(WRange a, WRange b) { return a.Range <= b.Range; }
		public static bool operator >=(WRange a, WRange b) { return a.Range >= b.Range; }

		public static bool operator ==(WRange me, WRange other) { return (me.Range == other.Range); }
		public static bool operator !=(WRange me, WRange other) { return !(me == other); }

		// Sampled a N-sample probability density function in the range [-1024..1024]
		// 1 sample produces a rectangular probability
		// 2 samples produces a triangular probability
		// ...
		// N samples approximates a true gaussian
		public static WRange FromPDF(MersenneTwister r, int samples)
		{
			return new WRange(Exts.MakeArray(samples, _ => r.Next(-1024, 1024))
				.Sum() / samples);
		}

		public static bool TryParse(string s, out WRange result)
		{
			s = s.ToLowerInvariant();
			var components = s.Split('c');
			var cell = 0;
			var subcell = 0;
			result = WRange.Zero;

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

			result = new WRange(1024*cell + subcell);
			return true;
		}

		public override int GetHashCode() { return Range.GetHashCode(); }

		public bool Equals(WRange other) { return other == this; }
		public override bool Equals(object obj) { return obj is WRange && Equals((WRange)obj); }

		public int CompareTo(object obj)
		{
			if (!(obj is WRange))
				return 1;
			return Range.CompareTo(((WRange)obj).Range);
		}
		public int CompareTo(WRange other) { return Range.CompareTo(other.Range); }

		public override string ToString() { return Range.ToString(); }
	}
}
