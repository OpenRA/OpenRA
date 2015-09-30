#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.IO;

namespace OpenRA
{
	public enum MapGridType { Rectangular, Diamond }

	public class MapGrid : IGlobalModData
	{
		public readonly MapGridType Type = MapGridType.Rectangular;
		public readonly Size TileSize = new Size(24, 24);
		public readonly byte MaximumTerrainHeight = 0;
		public readonly byte SubCellDefaultIndex = byte.MaxValue;
		public readonly WVec[] SubCellOffsets =
		{
			new WVec(0, 0, 0),       // full cell - index 0
			new WVec(-299, -256, 0), // top left - index 1
			new WVec(256, -256, 0),  // top right - index 2
			new WVec(0, 0, 0),       // center - index 3
			new WVec(-299, 256, 0),  // bottom left - index 4
			new WVec(256, 256, 0),   // bottom right - index 5
		};

		public MapGrid(MiniYaml yaml)
		{
			FieldLoader.Load(this, yaml);

			// The default subcell index defaults to the middle entry
			if (SubCellDefaultIndex == byte.MaxValue)
				SubCellDefaultIndex = (byte)(SubCellOffsets.Length / 2);
			else if (SubCellDefaultIndex < (SubCellOffsets.Length > 1 ? 1 : 0) || SubCellDefaultIndex >= SubCellOffsets.Length)
				throw new InvalidDataException("Subcell default index must be a valid index into the offset triples and must be greater than 0 for mods with subcells");
		}
	}
}
