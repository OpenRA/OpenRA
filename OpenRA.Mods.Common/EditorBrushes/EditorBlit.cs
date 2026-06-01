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
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.EditorBrushes
{
	public enum RotationDirection
	{
		Clockwise,
		CounterClockwise
	}

	public readonly record struct BlitTile(TerrainTile TerrainTile, ResourceTile ResourceTile, ResourceLayerContents? ResourceLayerContents, byte Height);

	public readonly record struct EditorBlitSource(CellCoordsRegion CellCoords, Dictionary<string, EditorActorPreview> Actors, Dictionary<CPos, BlitTile> Tiles);

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

			var blitSize = blitSource.CellCoords.BottomRight - blitSource.CellCoords.TopLeft;

			// Only include into the revert blit stuff which would be modified by the main blit.
			var mask = GetBlitSourceMask(
				blitSource, blitPosition - blitSource.CellCoords.TopLeft);

			commitBlitSource = blitSource;
			revertBlitSource = CopyRegionContents(
				map,
				editorActorLayer,
				resourceLayer,
				new CellCoordsRegion(blitPosition, blitPosition + blitSize),
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
			CellCoordsRegion region,
			MapBlitFilters blitFilters,
			IReadOnlySet<CPos> mask = null)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var mapResources = map.Resources;

			var previews = new Dictionary<string, EditorActorPreview>();
			var tiles = new Dictionary<CPos, BlitTile>();

			if (blitFilters.HasFlag(MapBlitFilters.Terrain) || blitFilters.HasFlag(MapBlitFilters.Resources))
			{
				foreach (var cell in region)
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
			}

			if (blitFilters.HasFlag(MapBlitFilters.Actors))
				foreach (var preview in editorActorLayer.PreviewsInCellRegion(region))
					if (mask == null || preview.Footprint.Keys.Any(mask.Contains))
						previews.TryAdd(preview.ID, preview);

			return new EditorBlitSource(region, previews, tiles);
		}

		void Blit(bool isRevert)
		{
			var source = isRevert ? revertBlitSource : commitBlitSource;
			var blitPos = isRevert ? source.CellCoords.TopLeft : blitPosition;
			var blitVec = blitPos - source.CellCoords.TopLeft;
			var blitSize = source.CellCoords.BottomRight - source.CellCoords.TopLeft;
			var blitRegion = new CellCoordsRegion(blitPos, blitPos + blitSize);

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
				var commitBlitVec = blitPosition - commitBlitSource.CellCoords.TopLeft;
				var mask = GetBlitSourceMask(commitBlitSource, commitBlitVec);
				using (new PerfTimer("RemoveActors", 1))
					editorActorLayer.RemoveRegion(blitRegion, mask);
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
			WorldRenderer wr,
			bool stickToGround)
		{
			var world = wr.World;
			var map = world.Map;
			var mapHeight = map.Height;
			var mapGrid = map.Grid;

			if (filters.HasFlag(MapBlitFilters.Terrain))
			{
				var terrainRenderer = world.WorldActor.Trait<ITiledTerrainRenderer>();
				foreach (var (pos, tile) in blitSource.Tiles)
				{
					var cPos = pos + offset;
					if (!map.Contains(cPos))
						continue;

					var height = stickToGround ? (mapHeight.TryGetValue(cPos, out var isoHeight) ? isoHeight : byte.MinValue) : tile.Height;
					var wPos = CellLayerUtils.CPosToWPos(cPos, height, mapGrid.Type);
					var preview = terrainRenderer.RenderPreview(wr, tile.TerrainTile, wPos);

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
					if (!map.Contains(cPos))
						continue;

					if (!filters.HasFlag(MapBlitFilters.Terrain) && !resourceLayer.CanAddResource(tile.ResourceLayerContents.Value.Type, cPos))
						continue;

					byte height;
					if (filters.HasFlag(MapBlitFilters.Terrain) && !stickToGround)
					{
						// We won't change relative tile height, use the saved value.
						height = tile.Height;
					}
					else
					{
						if (!mapHeight.TryGetValue(cPos, out height))
							height = byte.MinValue;

						// If a tile has inherent height, we know it will raise terrain.
						if (filters.HasFlag(MapBlitFilters.Terrain) && stickToGround)
							height += map.Rules.TerrainInfo.GetTerrainInfo(tile.TerrainTile).Height;
					}

					var wPos = CellLayerUtils.CPosToWPos(cPos, height, mapGrid.Type);
					var preview = resourceRenderers
						.SelectMany(r => r.RenderPreview(wr, tile.ResourceLayerContents.Value.Type, wPos));

					foreach (var renderable in preview)
						yield return renderable;
				}
			}

			if (filters.HasFlag(MapBlitFilters.Actors))
			{
				foreach (var (_, editorActorPreview) in blitSource.Actors)
				{
					var actorCell = editorActorPreview.Location + offset;
					if (!map.Contains(actorCell))
						continue;

					var useGround = stickToGround;
					if (!filters.HasFlag(MapBlitFilters.Terrain) || !blitSource.Tiles.ContainsKey(editorActorPreview.Location))
						useGround = true;

					var wOffset = CellLayerUtils.CVecToWVec(offset, 0, mapGrid.Type);
					if (useGround)
					{
						var actorPos = editorActorPreview.CenterPosition + wOffset;
						wOffset -= new WVec(0, 0, map.DistanceAboveTerrain(actorPos).Length);
					}

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
		public static HashSet<CPos> GetBlitSourceMask(
			EditorBlitSource blitSource,
			CVec offset)
		{
			var mask = new HashSet<CPos>();

			var sourceCellCoords = blitSource.CellCoords;

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

		public static EditorBlitSource Rotate(EditorBlitSource source, RotationDirection direction, WorldRenderer worldRenderer)
		{
			var region = source.CellCoords;
			var origin = region.TopLeft;
			var sizeDiff = region.BottomRight - origin;
			var width = sizeDiff.X + 1;
			var height = sizeDiff.Y + 1;

			CVec RotateOffset(CVec rel)
			{
				return direction switch
				{
					RotationDirection.Clockwise => new CVec(rel.Y, width - 1 - rel.X),
					RotationDirection.CounterClockwise => new CVec(height - 1 - rel.Y, rel.X),
					_ => throw new ArgumentOutOfRangeException(nameof(direction))
				};
			}

			var newRegion = new CellCoordsRegion(
				origin,
				origin + new CVec(height - 1, width - 1));

			var terrainInfo = worldRenderer.World.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			var newTiles = terrainInfo != null
				? RotateTiles(source.Tiles, origin, terrainInfo, RotateOffset)
				: RotateTilesPerCell(source.Tiles, origin, RotateOffset);

			var facingDelta = direction == RotationDirection.Clockwise ? new WAngle(256) : new WAngle(-256);
			var newActors = new Dictionary<string, EditorActorPreview>();
			foreach (var (id, preview) in source.Actors)
			{
				var copy = preview.Export();

				var locationInit = copy.GetOrDefault<LocationInit>();
				if (locationInit != null)
				{
					var rel = locationInit.Value - origin;
					copy.RemoveAll<LocationInit>();
					copy.Add(new LocationInit(origin + RotateOffset(new CVec(rel.X, rel.Y))));
				}

				var facingInit = copy.GetOrDefault<FacingInit>();
				if (facingInit != null)
				{
					var newFacing = new WAngle((facingInit.Value.Angle + facingDelta.Angle + 1024) % 1024);
					copy.RemoveAll<FacingInit>();
					copy.Add(new FacingInit(newFacing));
				}

				newActors[id] = new EditorActorPreview(worldRenderer, id, copy, preview.Owner);
			}

			return new EditorBlitSource(newRegion, newActors, newTiles);
		}

		static Dictionary<CPos, BlitTile> RotateTilesPerCell(
			IReadOnlyDictionary<CPos, BlitTile> sourceTiles,
			CPos origin,
			Func<CVec, CVec> rotateOffset)
		{
			var newTiles = new Dictionary<CPos, BlitTile>();
			foreach (var (pos, tile) in sourceTiles)
			{
				var rel = pos - origin;
				newTiles[origin + rotateOffset(new CVec(rel.X, rel.Y))] = tile;
			}

			return newTiles;
		}

		static Dictionary<CPos, BlitTile> RotateTiles(
			IReadOnlyDictionary<CPos, BlitTile> sourceTiles,
			CPos origin,
			ITemplatedTerrainInfo terrainInfo,
			Func<CVec, CVec> rotateOffset)
		{
			var processed = new HashSet<CPos>();
			var newTiles = new Dictionary<CPos, BlitTile>();

			foreach (var (pos, blitTile) in sourceTiles)
			{
				if (processed.Contains(pos))
					continue;

				var terrainTile = blitTile.TerrainTile;
				if (!terrainInfo.Templates.TryGetValue(terrainTile.Type, out var template)
					|| template.PickAny
					|| (template.Size.X == 1 && template.Size.Y == 1))
				{
					var rel = pos - origin;
					newTiles[origin + rotateOffset(new CVec(rel.X, rel.Y))] = blitTile;
					processed.Add(pos);
					continue;
				}

				var localX = terrainTile.Index % template.Size.X;
				var localY = terrainTile.Index / template.Size.X;
				var anchor = pos - new CVec(localX, localY);

				if (!IsCompleteTemplatePlacement(template, anchor, terrainTile.Type, sourceTiles))
				{
					var rel = pos - origin;
					newTiles[origin + rotateOffset(new CVec(rel.X, rel.Y))] = blitTile;
					processed.Add(pos);
					continue;
				}

				var templateWidth = template.Size.X;
				var rotatedCells = new List<(CPos NewCell, BlitTile Tile)>();
				for (byte i = 0; i < templateWidth * template.Size.Y; i++)
				{
					if (!template.Contains(i) || template[i] == null)
						continue;

					var cell = anchor + new CVec(i % templateWidth, i / templateWidth);
					var tile = sourceTiles[cell];
					var rel = cell - origin;
					rotatedCells.Add((origin + rotateOffset(new CVec(rel.X, rel.Y)), tile));
					processed.Add(cell);
				}

				var newAnchor = new CPos(
					rotatedCells.Min(c => c.NewCell.X),
					rotatedCells.Min(c => c.NewCell.Y));

				foreach (var (newCell, tile) in rotatedCells)
				{
					var newLocal = newCell - newAnchor;
					var newIndex = (byte)(newLocal.Y * templateWidth + newLocal.X);
					if (!template.Contains(newIndex) || template[newIndex] == null)
						continue;

					var newTerrainTile = new TerrainTile(terrainTile.Type, newIndex);
					newTiles[newCell] = new BlitTile(newTerrainTile, tile.ResourceTile, tile.ResourceLayerContents, tile.Height);
				}
			}

			return newTiles;
		}

		static bool IsCompleteTemplatePlacement(
			TerrainTemplateInfo template,
			CPos anchor,
			ushort templateType,
			IReadOnlyDictionary<CPos, BlitTile> sourceTiles)
		{
			var templateWidth = template.Size.X;
			for (byte i = 0; i < templateWidth * template.Size.Y; i++)
			{
				if (!template.Contains(i) || template[i] == null)
					continue;

				var cell = anchor + new CVec(i % templateWidth, i / templateWidth);
				if (!sourceTiles.TryGetValue(cell, out var tile))
					return false;

				if (tile.TerrainTile.Type != templateType || tile.TerrainTile.Index != i)
					return false;
			}

			return true;
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
