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
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	[Flags]
	public enum MapCopyFilters
	{
		None = 0,
		Terrain = 1,
		Resources = 2,
		Actors = 4,
		All = Terrain | Resources | Actors
	}

	public sealed class EditorCopyPasteBrush : IEditorBrush
	{
		readonly WorldRenderer worldRenderer;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorActionManager editorActionManager;
		readonly EditorClipboard clipboard;
		readonly IResourceLayer resourceLayer;
		readonly Func<MapCopyFilters> getCopyFilters;

		public CPos? PastePreviewPosition { get; private set; }

		public CellRegion Region => clipboard.CellRegion;

		public EditorCopyPasteBrush(
			EditorViewportControllerWidget editorWidget,
			WorldRenderer wr,
			EditorClipboard clipboard,
			IResourceLayer resourceLayer,
			Func<MapCopyFilters> getCopyFilters)
		{
			this.getCopyFilters = getCopyFilters;
			this.editorWidget = editorWidget;
			this.clipboard = clipboard;
			this.resourceLayer = resourceLayer;
			worldRenderer = wr;

			editorActionManager = wr.World.WorldActor.Trait<EditorActionManager>();
			editorActorLayer = wr.World.WorldActor.Trait<EditorActorLayer>();
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses left and right mouse buttons, but nothing else
			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right)
				return false;

			if (mi.Button == MouseButton.Right)
			{
				if (mi.Event == MouseInputEvent.Up)
				{
					editorWidget.ClearBrush();
					return true;
				}

				return false;
			}

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				var pastePosition = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
				var action = new CopyPasteEditorAction(
					getCopyFilters(),
					resourceLayer,
					pastePosition,
					worldRenderer.World.Map,
					clipboard,
					editorActorLayer);

				editorActionManager.Add(action);
				return true;
			}

			return false;
		}

		public void Tick()
		{
			PastePreviewPosition = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
		}

		public void Dispose()
		{
		}
	}

	sealed class CopyPasteEditorAction : IEditorAction
	{
		[TranslationReference("amount")]
		const string CopiedTiles = "notification-copied-tiles";

		public string Text { get; }

		readonly MapCopyFilters copyFilters;
		readonly IResourceLayer resourceLayer;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorClipboard clipboard;
		readonly EditorClipboard undoClipboard;
		readonly CPos pastePosition;
		readonly Map map;

		public CopyPasteEditorAction(
			MapCopyFilters copyFilters,
			IResourceLayer resourceLayer,
			CPos pastePosition,
			Map map,
			EditorClipboard clipboard,
			EditorActorLayer editorActorLayer)
		{
			this.copyFilters = copyFilters;
			this.resourceLayer = resourceLayer;
			this.clipboard = clipboard;
			this.pastePosition = pastePosition;
			this.editorActorLayer = editorActorLayer;
			this.map = map;

			undoClipboard = CopySelectionContents();

			Text = TranslationProvider.GetString(CopiedTiles, Translation.Arguments("amount", clipboard.Tiles.Count));
		}

		/// <summary>
		/// TODO: This is pretty much repeated in MapEditorSelectionLogic.
		/// </summary>
		/// <returns>Clipboard containing map contents for this region.</returns>
		EditorClipboard CopySelectionContents()
		{
			var selectionSize = clipboard.CellRegion.BottomRight - clipboard.CellRegion.TopLeft;
			var source = new CellCoordsRegion(pastePosition, pastePosition + selectionSize);
			var selection = new CellRegion(map.Grid.Type, pastePosition, pastePosition + selectionSize);

			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var mapResources = map.Resources;

			var previews = new Dictionary<string, EditorActorPreview>();
			var tiles = new Dictionary<CPos, ClipboardTile>();

			foreach (var cell in source)
			{
				if (!mapTiles.Contains(cell))
					continue;

				var resourceLayerContents = resourceLayer?.GetResource(cell);
				tiles.Add(cell, new ClipboardTile(mapTiles[cell], mapResources[cell], resourceLayerContents, mapHeight[cell]));

				if (copyFilters.HasFlag(MapCopyFilters.Actors))
					foreach (var preview in selection.SelectMany(editorActorLayer.PreviewsAt).Distinct())
						previews.TryAdd(preview.ID, preview);
			}

			return new EditorClipboard(selection, previews, tiles);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var sourcePos = clipboard.CellRegion.TopLeft;
			var pasteVec = new CVec(pastePosition.X - sourcePos.X, pastePosition.Y - sourcePos.Y);

			foreach (var tileKeyValuePair in clipboard.Tiles)
			{
				var position = tileKeyValuePair.Key + pasteVec;
				if (!map.Contains(position))
					continue;

				// Clear any existing resources.
				if (resourceLayer != null && copyFilters.HasFlag(MapCopyFilters.Resources))
					resourceLayer.ClearResources(position);

				var tile = tileKeyValuePair.Value;
				var resourceLayerContents = tile.ResourceLayerContents;

				if (copyFilters.HasFlag(MapCopyFilters.Terrain))
				{
					map.Tiles[position] = tile.TerrainTile;
					map.Height[position] = tile.Height;
				}

				if (copyFilters.HasFlag(MapCopyFilters.Resources) &&
					resourceLayerContents.HasValue &&
					!string.IsNullOrWhiteSpace(resourceLayerContents.Value.Type))
					resourceLayer.AddResource(resourceLayerContents.Value.Type, position, resourceLayerContents.Value.Density);
			}

			if (copyFilters.HasFlag(MapCopyFilters.Actors))
			{
				// Clear any existing actors in the paste cells.
				var selectionSize = clipboard.CellRegion.BottomRight - clipboard.CellRegion.TopLeft;
				var pasteRegion = new CellRegion(map.Grid.Type, pastePosition, pastePosition + selectionSize);
				foreach (var regionActor in pasteRegion.SelectMany(editorActorLayer.PreviewsAt).ToHashSet())
					editorActorLayer.Remove(regionActor);

				// Now place actors.
				foreach (var actorKeyValuePair in clipboard.Actors)
				{
					var selection = clipboard.CellRegion;
					var copy = actorKeyValuePair.Value.Export();
					var locationInit = copy.GetOrDefault<LocationInit>();
					if (locationInit != null)
					{
						var actorPosition = locationInit.Value + new CVec(pastePosition.X - selection.TopLeft.X, pastePosition.Y - selection.TopLeft.Y);
						if (!map.Contains(actorPosition))
							continue;

						copy.RemoveAll<LocationInit>();
						copy.Add(new LocationInit(actorPosition));
					}

					editorActorLayer.Add(copy);
				}
			}
		}

		public void Undo()
		{
			foreach (var tileKeyValuePair in undoClipboard.Tiles)
			{
				var position = tileKeyValuePair.Key;
				var tile = tileKeyValuePair.Value;
				var resourceLayerContents = tile.ResourceLayerContents;

				// Clear any existing resources.
				if (resourceLayer != null && copyFilters.HasFlag(MapCopyFilters.Resources))
					resourceLayer.ClearResources(position);

				if (copyFilters.HasFlag(MapCopyFilters.Terrain))
				{
					map.Tiles[position] = tile.TerrainTile;
					map.Height[position] = tile.Height;
				}

				if (copyFilters.HasFlag(MapCopyFilters.Resources) &&
					resourceLayerContents.HasValue &&
					!string.IsNullOrWhiteSpace(resourceLayerContents.Value.Type))
					resourceLayer.AddResource(resourceLayerContents.Value.Type, position, resourceLayerContents.Value.Density);
			}

			if (copyFilters.HasFlag(MapCopyFilters.Actors))
			{
				// Clear existing actors.
				foreach (var regionActor in undoClipboard.CellRegion.SelectMany(editorActorLayer.PreviewsAt).Distinct().ToList())
					editorActorLayer.Remove(regionActor);

				// Place actors back again.
				foreach (var actor in undoClipboard.Actors.Values)
					editorActorLayer.Add(actor);
			}
		}
	}
}
