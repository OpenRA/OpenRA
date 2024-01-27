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

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.EditorBrushes
{
	public readonly struct ClipboardTile
	{
		public readonly TerrainTile TerrainTile;
		public readonly ResourceTile ResourceTile;
		public readonly ResourceLayerContents? ResourceLayerContents;
		public readonly byte Height;

		public ClipboardTile(TerrainTile terrainTile, ResourceTile resourceTile, ResourceLayerContents? resourceLayerContents, byte height)
		{
			TerrainTile = terrainTile;
			ResourceTile = resourceTile;
			ResourceLayerContents = resourceLayerContents;
			Height = height;
		}
	}

	public readonly struct EditorClipboard
	{
		public readonly CellRegion CellRegion;
		public readonly Dictionary<string, EditorActorPreview> Actors;
		public readonly Dictionary<CPos, ClipboardTile> Tiles;

		public EditorClipboard(CellRegion cellRegion, Dictionary<string, EditorActorPreview> actors, Dictionary<CPos, ClipboardTile> tiles)
		{
			CellRegion = cellRegion;
			Actors = actors;
			Tiles = tiles;
		}
	}
}
