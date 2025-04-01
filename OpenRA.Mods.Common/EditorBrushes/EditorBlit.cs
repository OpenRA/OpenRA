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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.EditorBrushes
{
	public readonly record struct BlitTile(TerrainTile TerrainTile, ResourceTile ResourceTile, ResourceLayerContents? ResourceLayerContents, byte Height);

	public readonly record struct EditorBlitSource(CellRegion CellRegion, Dictionary<string, EditorActorPreview> Actors, Dictionary<CPos, BlitTile> Tiles);

	[Flags]
	public enum MapBlitFilters
	{
		None = 0,
		Terrain = 1,
		Resources = 2,
		Actors = 4,
		All = Terrain | Resources | Actors
	}

	/// <summary>
	/// Core implementation for EditorActions which overwrite a region of the map (such as
	/// copy-paste).
	/// </summary>
	public sealed class EditorBlit
	{
		readonly MapBlitFilters blitFilters;
		readonly IResourceLayer resourceLayer;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorBlitSource blitSource;
		readonly EditorBlitSource revertBlitSource;
		readonly CPos blitPosition;
		readonly Map map;
		readonly bool respectBounds;

		public EditorBlit(
			MapBlitFilters blitFilters,
			IResourceLayer resourceLayer,
			CPos blitPosition,
			Map map,
			EditorBlitSource blitSource,
			EditorActorLayer editorActorLayer,
			bool respectBounds)
		{
			this.blitFilters = blitFilters;
			this.resourceLayer = resourceLayer;
			this.blitSource = blitSource;
			this.blitPosition = blitPosition;
			this.editorActorLayer = editorActorLayer;
			this.map = map;
			this.respectBounds = respectBounds;

			var blitSize = blitSource.CellRegion.BottomRight - blitSource.CellRegion.TopLeft;
			revertBlitSource = CopyRegionContents(
				map,
				editorActorLayer,
				resourceLayer,
				new CellRegion(map.Grid.Type, blitPosition, blitPosition + blitSize),
				blitFilters);
		}

		/// <summary>
		/// Returns an EditorBlitSource containing the map contents for a given region.
		/// </summary>
		public static EditorBlitSource CopyRegionContents(
			Map map,
			EditorActorLayer editorActorLayer,
			IResourceLayer resourceLayer,
			CellRegion region,
			MapBlitFilters blitFilters)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var mapResources = map.Resources;

			var previews = new Dictionary<string, EditorActorPreview>();
			var tiles = new Dictionary<CPos, BlitTile>();

			foreach (var cell in region.CellCoords)
			{
				if (!mapTiles.Contains(cell))
					continue;

				tiles.Add(
					cell,
					new BlitTile(mapTiles[cell],
					mapResources[cell],
					resourceLayer?.GetResource(cell),
					mapHeight[cell]));
			}

			if (blitFilters.HasFlag(MapBlitFilters.Actors))
				foreach (var preview in editorActorLayer.PreviewsInCellRegion(region.CellCoords))
					previews.TryAdd(preview.ID, preview);

			return new EditorBlitSource(region, previews, tiles);
		}

		void Blit(EditorBlitSource source, bool isRevert)
		{
			var blitPos = isRevert ? source.CellRegion.TopLeft : blitPosition;
			var blitVec = blitPos - source.CellRegion.TopLeft;
			var blitSize = source.CellRegion.BottomRight - source.CellRegion.TopLeft;
			var blitRegion = new CellRegion(map.Grid.Type, blitPos, blitPos + blitSize);

			if (blitFilters.HasFlag(MapBlitFilters.Actors))
			{
				// Clear any existing actors in the paste cells.
				foreach (var regionActor in editorActorLayer.PreviewsInCellRegion(blitRegion.CellCoords).ToList())
					editorActorLayer.Remove(regionActor);
			}

			foreach (var tileKeyValuePair in source.Tiles)
			{
				var position = tileKeyValuePair.Key + blitVec;
				if (!map.Tiles.Contains(position) || (respectBounds && !map.Contains(position)))
					continue;

				// Clear any existing resources.
				if (resourceLayer != null && blitFilters.HasFlag(MapBlitFilters.Resources))
					resourceLayer.ClearResources(position);

				var tile = tileKeyValuePair.Value;
				var resourceLayerContents = tile.ResourceLayerContents;

				if (blitFilters.HasFlag(MapBlitFilters.Terrain))
				{
					map.Tiles[position] = tile.TerrainTile;
					map.Height[position] = tile.Height;
				}

				if (blitFilters.HasFlag(MapBlitFilters.Resources) &&
					resourceLayerContents.HasValue &&
					!string.IsNullOrWhiteSpace(resourceLayerContents.Value.Type))
					resourceLayer.AddResource(resourceLayerContents.Value.Type, position, resourceLayerContents.Value.Density);
			}

			if (blitFilters.HasFlag(MapBlitFilters.Actors))
			{
				if (isRevert)
				{
					// For reverts, just place the original actors back exactly how they were.
					foreach (var actor in source.Actors.Values)
						editorActorLayer.Add(actor);
				}
				else
				{
					// Create copies of the original actors, update their locations, and place.
					foreach (var actorKeyValuePair in source.Actors)
					{
						var copy = actorKeyValuePair.Value.Export();
						var locationInit = copy.GetOrDefault<LocationInit>();
						if (locationInit != null)
						{
							var actorPosition = locationInit.Value + blitVec;
							if (respectBounds && !map.Contains(actorPosition))
								continue;

							copy.RemoveAll<LocationInit>();
							copy.Add(new LocationInit(actorPosition));
						}

						editorActorLayer.Add(copy);
					}
				}
			}
		}

		public void Commit() => Blit(blitSource, false);
		public void Revert() => Blit(revertBlitSource, true);

		public int TileCount()
		{
			return blitSource.Tiles.Count;
		}
	}
}
