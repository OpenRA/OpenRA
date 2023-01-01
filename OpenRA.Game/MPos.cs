#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Primitives;

namespace OpenRA
{
	public readonly struct MPos : IEquatable<MPos>
	{
		public readonly int U, V;

		public MPos(int u, int v) { U = u; V = v; }
		public static readonly MPos Zero = new MPos(0, 0);

		public static bool operator ==(MPos me, MPos other) { return me.U == other.U && me.V == other.V; }
		public static bool operator !=(MPos me, MPos other) { return !(me == other); }

		public override int GetHashCode() { return U.GetHashCode() ^ V.GetHashCode(); }

		public bool Equals(MPos other) { return other == this; }
		public override bool Equals(object obj) { return obj is MPos && Equals((MPos)obj); }

		public MPos Clamp(Rectangle r)
		{
			return new MPos(Math.Min(r.Right, Math.Max(U, r.Left)),
							Math.Min(r.Bottom, Math.Max(V, r.Top)));
		}

		public override string ToString() { return U + "," + V; }

		public CPos ToCPos(Map map)
		{
			return ToCPos(map.Grid.Type);
		}

		public CPos ToCPos(MapGridType gridType)
		{
			if (gridType == MapGridType.Rectangular)
				return new CPos(U, V);

			// Convert from rectangular map position to RectangularIsometric cell position
			//  - The staggered rows make this fiddly (hint: draw a diagram!)
			// (a) Consider the relationships:
			//  - +1u (even -> odd) adds (1, -1) to (x, y)
			//  - +1v (even -> odd) adds (1, 0) to (x, y)
			//  - +1v (odd -> even) adds (0, 1) to (x, y)
			// (b) Therefore:
			//  - au + 2bv adds (a + b) to (x, y)
			//  - a correction factor is added if v is odd
			var offset = (V & 1) == 1 ? 1 : 0;
			var y = (V - offset) / 2 - U;
			var x = V - y;
			return new CPos(x, y);
		}
	}

	/// <summary>
	/// Projected map position
	/// </summary>
	public readonly struct PPos : IEquatable<PPos>
	{
		public readonly int U, V;

		public PPos(int u, int v) { U = u; V = v; }
		public static readonly PPos Zero = new PPos(0, 0);

		public static bool operator ==(PPos me, PPos other) { return me.U == other.U && me.V == other.V; }
		public static bool operator !=(PPos me, PPos other) { return !(me == other); }

		public static explicit operator MPos(PPos puv) { return new MPos(puv.U, puv.V); }
		public static explicit operator PPos(MPos uv) { return new PPos(uv.U, uv.V); }

		public PPos Clamp(Rectangle r)
		{
			return new PPos(Math.Min(r.Right, Math.Max(U, r.Left)),
				Math.Min(r.Bottom, Math.Max(V, r.Top)));
		}

		public override int GetHashCode() { return U.GetHashCode() ^ V.GetHashCode(); }

		public bool Equals(PPos other) { return other == this; }
		public override bool Equals(object obj) { return obj is PPos && Equals((PPos)obj); }

		public override string ToString() { return U + "," + V; }
	}
}
