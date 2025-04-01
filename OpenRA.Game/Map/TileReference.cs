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

namespace OpenRA
{
	public readonly struct TerrainTile(ushort type, byte index)
	{
		public readonly ushort Type = type;
		public readonly byte Index = index;

		public override int GetHashCode() { return Type.GetHashCode() ^ Index.GetHashCode(); }

		public override string ToString() { return Type + "," + Index; }

		public static bool TryParse(string s, out TerrainTile tt)
		{
			var split = s.Split(',');
			if (split.Length == 2 &&
				Exts.TryParseUshortInvariant(split[0], out var type) &&
				Exts.TryParseByteInvariant(split[1], out var index))
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
