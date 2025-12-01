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
using System.Runtime.InteropServices;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;

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
		readonly EditorBlitSource commitBlitSource;
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
			this.blitPosition = blitPosition;
			this.editorActorLayer = editorActorLayer;
			this.map = map;
			this.respectBounds = respectBounds;

			var blitSize = blitSource.CellRegion.BottomRight - blitSource.CellRegion.TopLeft;

			// Only include into the revert blit stuff which would be modified by the main blit.
			var mask = GetBlitSourceMask(
				blitSource, blitPosition - blitSource.CellRegion.TopLeft);

			commitBlitSource = blitSource;
			revertBlitSource = CopyRegionContents(
				map,
				editorActorLayer,
				resourceLayer,
				new CellRegion(map.Grid.Type, blitPosition, blitPosition + blitSize),
				blitFilters,
				mask);
		}

		/// <summary>
		/// Returns an EditorBlitSource containing the map contents for a given region.
		/// If a mask is supplied, only tiles and actors (fully or partially) overlapping the mask
		/// are included in the EditorBlitSource.
		/// </summary>
		public static EditorBlitSource CopyRegionContents(
			Map map,
			EditorActorLayer editorActorLayer,
			IResourceLayer resourceLayer,
			CellRegion region,
			MapBlitFilters blitFilters,
			IReadOnlySet<CPos> mask = null)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var mapResources = map.Resources;

			var previews = new Dictionary<string, EditorActorPreview>();
			var tiles = new Dictionary<CPos, BlitTile>();

			foreach (var cell in region.CellCoords)
			{
				if (!mapTiles.Contains(cell) || (mask != null && !mask.Contains(cell)))
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
					if (mask == null || preview.Footprint.Keys.Any(mask.Contains))
						previews.TryAdd(preview.ID, preview);

			return new EditorBlitSource(region, previews, tiles);
		}

		void Blit(bool isRevert)
		{
			var source = isRevert ? revertBlitSource : commitBlitSource;
			var blitPos = isRevert ? source.CellRegion.TopLeft : blitPosition;
			var blitVec = blitPos - source.CellRegion.TopLeft;
			var blitSize = source.CellRegion.BottomRight - source.CellRegion.TopLeft;
			var blitRegion = new CellRegion(map.Grid.Type, blitPos, blitPos + blitSize);

			if (blitFilters.HasFlag(MapBlitFilters.Actors))
			{
				// Clear any existing actors in the paste cells.
				//
				// revertBlitSource's mask may be a superset of the commitBlitSource's mask if
				// - Its a sparse blit; and
				// - The revert actors removed by the commit are partially outside of the commit mask.
				// Otherwise, it's a (practically) equal set. (Subject to map bounds.)
				//
				// This implies that:
				// - commitBlitSource's mask will overlap all commit actors.
				// - revertBlitSource's mask will overlap all revert actors.
				// - commitBlitSource's mask will overlap all and no more than the revert actors.
				// - revertBlitSource's mask will overlap all revert actors BUT MAY OVERLAP MORE!
				//
				// This means we use the commit mask, not the revert one.
				var commitBlitVec = blitPosition - commitBlitSource.CellRegion.TopLeft;
				var mask = GetBlitSourceMask(commitBlitSource, commitBlitVec);
				using (new PerfTimer("RemoveActors", 1))
					editorActorLayer.RemoveRegion(blitRegion.CellCoords, mask);
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
					!string.IsNullOrWhiteSpace(resourceLayerContents.Value.Type) &&
					resourceLayer.CanAddResource(resourceLayerContents.Value.Type, position))
				{
					resourceLayer.AddResource(resourceLayerContents.Value.Type, position, resourceLayerContents.Value.Density);
				}
			}

			if (blitFilters.HasFlag(MapBlitFilters.Actors))
			{
				if (isRevert)
				{
					// For reverts, just place the original actors back exactly how they were.
					using (new PerfTimer("AddActors", 1))
						editorActorLayer.AddRange(source.Actors.Values.ToArray().AsSpan());
				}
				else
				{
					// Create copies of the original actors, update their locations, and place.
					var copies = new List<ActorReference>(source.Actors.Count);
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

						copies.Add(copy);
					}

					using (new PerfTimer("AddActors", 1))
						editorActorLayer.AddRange(CollectionsMarshal.AsSpan(copies));
				}
			}
		}

		public static IEnumerable<IRenderable> PreviewBlitSource(
			EditorBlitSource blitSource,
			MapBlitFilters filters,
			CVec offset,
			WorldRenderer wr)
		{
			var world = wr.World;
			var map = world.Map;

			var wOffset = map.CenterOfCell(CPos.Zero + offset) - map.CenterOfCell(CPos.Zero);

			if (filters.HasFlag(MapBlitFilters.Terrain))
			{
				var terrainRenderer = world.WorldActor.Trait<ITiledTerrainRenderer>();
				foreach (var (cpos, tile) in blitSource.Tiles)
				{
					var preview =
						terrainRenderer.RenderPreview(
							wr,
							tile.TerrainTile,
							map.CenterOfCell(cpos + offset));
					foreach (var renderable in preview)
						yield return renderable;
				}
			}

			if (filters.HasFlag(MapBlitFilters.Resources))
			{
				var resourceRenderers = world.WorldActor.TraitsImplementing<IResourceRenderer>().ToArray();
				var resourceLayer = world.WorldActor.Trait<IResourceLayer>();
				foreach (var (pos, tile) in blitSource.Tiles)
				{
					if (tile.ResourceLayerContents == null || tile.ResourceLayerContents.Value.Type == null)
						continue;

					var cPos = pos + offset;
					if (!filters.HasFlag(MapBlitFilters.Terrain) && !resourceLayer.CanAddResource(tile.ResourceLayerContents.Value.Type, cPos))
						continue;

					var preview = resourceRenderers
						.SelectMany(r => r.RenderPreview(
							wr,
							tile.ResourceLayerContents.Value.Type,
							map.CenterOfCell(cPos)));
					foreach (var renderable in preview)
						yield return renderable;
				}
			}

			if (filters.HasFlag(MapBlitFilters.Actors))
			{
				foreach (var (_, editorActorPreview) in blitSource.Actors)
				{
					var preview = editorActorPreview.RenderWithOffset(wOffset)
						.OrderBy(WorldRenderer.RenderableZPositionComparisonKey);
					foreach (var renderable in preview)
						yield return renderable;
				}
			}
		}

		/// <summary>
		/// Find the set of cells within an EditorBlitSource that are actually occupied by a
		/// BlitTile or actor. Note that all tiles must be inside the CellRegion, and actors must
		/// be at least partially inside the CellRegion. If an actor partially lies outside of the
		/// CellRegion, only cells within the CellRegion are included in the output set.
		/// </summary>
		static HashSet<CPos> GetBlitSourceMask(
			EditorBlitSource blitSource,
			CVec offset)
		{
			var mask = new HashSet<CPos>();

			var sourceCellCoords = blitSource.CellRegion.CellCoords;

			foreach (var (cpos, _) in blitSource.Tiles)
			{
				if (!sourceCellCoords.Contains(cpos))
					throw new ArgumentException("EditorBlitSource contains a BlitTile outside of its CellRegion");
				mask.Add(cpos + offset);
			}

			foreach (var (_, editorActorPreview) in blitSource.Actors)
			{
				var anyContained = false;
				foreach (var cpos in editorActorPreview.Footprint.Keys)
				{
					if (sourceCellCoords.Contains(cpos))
					{
						mask.Add(cpos + offset);
						anyContained = true;
					}
				}

				if (!anyContained)
					throw new ArgumentException("EditorBlitSource contains an actor entirely outside of its CellRegion");
			}

			return mask;
		}

		public void Commit() => Blit(false);
		public void Revert() => Blit(true);

		public int TileCount()
		{
			return commitBlitSource.Tiles.Count;
		}

		public int ActorCount()
		{
			return commitBlitSource.Actors.Count;
		}
	}
}
