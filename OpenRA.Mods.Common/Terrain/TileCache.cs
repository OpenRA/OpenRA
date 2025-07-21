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

using OpenRA.Graphics;

namespace OpenRA
{
	public interface ITileCache
	{
		bool HasTileSprite(TerrainTile r, int? variant = null);
		Sprite TileSprite(TerrainTile r, int? variant = null);
		SheetBuilder GetSheetBuilder(SheetType sheetType);
	}
}
