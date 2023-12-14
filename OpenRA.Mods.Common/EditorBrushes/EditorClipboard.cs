using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.EditorBrushes
{
	public readonly struct ClipboardTile
	{
		public readonly TerrainTile TerrainTile;
		public readonly ResourceTile ResourceTile;
		public readonly ResourceLayerContents ResourceLayerContents;
		public readonly byte Height;

		public ClipboardTile(TerrainTile terrainTile, ResourceTile resourceTile, ResourceLayerContents resourceLayerContents, byte height)
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
