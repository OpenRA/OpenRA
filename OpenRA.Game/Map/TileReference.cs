#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA
{
	public struct TerrainTile
	{
		public readonly ushort Type;
		public readonly byte Index;

		public TerrainTile(ushort type, byte index)
		{
			Type = type;
			Index = index;
		}

		public override int GetHashCode() { return Type.GetHashCode() ^ Index.GetHashCode(); }
	}

	public struct ResourceTile
	{
		public readonly byte Type;
		public readonly byte Index;

		public ResourceTile(byte type, byte index)
		{
			Type = type;
			Index = index;
		}

		public override int GetHashCode() { return Type.GetHashCode() ^ Index.GetHashCode(); }
	}
}
