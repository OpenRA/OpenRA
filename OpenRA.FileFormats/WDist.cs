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
using System.Linq;

namespace OpenRA
{
	/// <summary>
	/// 1d world distance - 1024 units = 1 cell.
	/// </summary>
	public struct WDist : IComparable
	{
		public readonly int Range;

		public WDist(int r) { Range = r; }
		public static readonly WDist Zero = new WDist(0);
		public static WDist FromCells(int cells) { return new WDist(1024*cells); }

		public static WDist operator +(WDist a, WDist b) { return new WDist(a.Range + b.Range); }
		public static WDist operator -(WDist a, WDist b) { return new WDist(a.Range - b.Range); }
		public static WDist operator -(WDist a) { return new WDist(-a.Range); }
		public static WDist operator /(WDist a, int b) { return new WDist(a.Range / b); }
		public static WDist operator *(WDist a, int b) { return new WDist(a.Range * b); }
		public static WDist operator *(int a, WDist b) { return new WDist(a * b.Range); }

		public static bool operator ==(WDist me, WDist other) { return (me.Range == other.Range); }
		public static bool operator !=(WDist me, WDist other) { return !(me == other); }

		// Sampled a N-sample probability density function in the range [-1024..1024]
		// 1 sample produces a rectangular probability
		// 2 samples produces a triangular probability
		// ...
		// N samples approximates a true gaussian
		public static WDist FromPDF(Thirdparty.Random r, int samples)
		{
			return new WDist(Exts.MakeArray(samples, _ => r.Next(-1024, 1024))
				.Sum() / samples);
		}

		public static bool TryParse(string s, out WDist result)
		{
			s = s.ToLowerInvariant();
			var components = s.Split('c');
			int cell = 0;
			int subcell = 0;
			result = WDist.Zero;

			switch (components.Length)
			{
			case 2:
				if (!int.TryParse(components[0], out cell) ||
				    !int.TryParse(components[1], out subcell))
					return false;
				break;
			case 1:
				if (!int.TryParse(components[0], out subcell))
					return false;
				break;
			default: return false;
			}

			// Propagate sign to fractional part
			if (cell < 0)
				subcell = -subcell;

			result = new WDist(1024*cell + subcell);
			return true;
		}

		public override int GetHashCode() { return Range.GetHashCode(); }

		public override bool Equals(object obj)
		{
			var o = obj as WDist?;
			return o != null && o == this;
		}

		public int CompareTo(object obj)
		{
			var o = obj as WDist?;
			if (o == null)
				return 1;

			return Range.CompareTo(o.Value.Range);
		}

		public override string ToString() { return "{0}".F(Range); }
	}
}
