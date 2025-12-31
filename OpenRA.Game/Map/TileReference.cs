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

namespace OpenRA
{
	public readonly struct TerrainTile(ushort type, byte index)
	{
		public readonly ushort Type = type;
		public readonly byte Index = index;

		public override int GetHashCode() { return Type.GetHashCode() ^ Index.GetHashCode(); }

		public override string ToString() { return Type + "," + Index; }

		public static bool TryParse(ReadOnlySpan<char> s, out TerrainTile tt)
		{
			Span<Range> ranges = stackalloc Range[3];
			var parts = s.Split(ranges, ',');
			if (parts == 2 &&
				Exts.TryParseUInt16Invariant(s[ranges[0]], out var type) &&
				Exts.TryParseByteInvariant(s[ranges[1]], out var index))
			{
				tt = new TerrainTile(type, index);
				return true;
			}

			tt = default;
			return false;
		}
	}

	public readonly struct ResourceTile(byte type, byte index)
	{
		public readonly byte Type = type;
		public readonly byte Index = index;

		public override int GetHashCode() { return Type.GetHashCode() ^ Index.GetHashCode(); }
	}
}
