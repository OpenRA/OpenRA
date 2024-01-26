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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorMarkerLayerBrush : IEditorBrush
	{
		public int? Template;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorActionManager editorActionManager;
		readonly MarkerLayerOverlay markerLayerOverlay;
		readonly EditorViewportControllerWidget editorWidget;

		PaintMarkerTileEditorAction action;
		bool painting;

		public EditorMarkerLayerBrush(EditorViewportControllerWidget editorWidget, int? id, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;

			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			markerLayerOverlay = world.WorldActor.Trait<MarkerLayerOverlay>();

			Template = id;
			worldRenderer = wr;
			world = wr.World;
			action = new PaintMarkerTileEditorAction(Template, markerLayerOverlay);
		}

		public bool HandleMouseInput(MouseInput mi)
		{
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

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

			if (mi.Button == MouseButton.Left && mi.Event != MouseInputEvent.Up)
			{
				action.Add(cell);
				painting = true;
			}
			else if (painting && mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				if (action.DidPaintTiles)
					editorActionManager.Add(action);

				action = new PaintMarkerTileEditorAction(Template, markerLayerOverlay);
				painting = false;
			}

			return true;
		}

		public void Tick() { }

		public void Dispose() { }
	}

	readonly struct PaintMarkerTile
	{
		public readonly CPos Cell;
		public readonly int? Previous;

		public PaintMarkerTile(CPos cell, int? previous)
		{
			Cell = cell;
			Previous = previous;
		}
	}

	class PaintMarkerTileEditorAction : IEditorAction
	{
		[TranslationReference("amount", "type")]
		const string AddedMarkerTiles = "notification-added-marker-tiles";

		[TranslationReference("amount")]
		const string RemovedMarkerTiles = "notification-removed-marker-tiles";

		public string Text { get; private set; }

		readonly int? type;
		readonly MarkerLayerOverlay markerLayerOverlay;

		readonly List<PaintMarkerTile> paintTiles = new();

		public bool DidPaintTiles => paintTiles.Count > 0;

		public PaintMarkerTileEditorAction(
			int? type,
			MarkerLayerOverlay markerLayerOverlay)
		{
			this.markerLayerOverlay = markerLayerOverlay;
			this.type = type;
		}

		public void Execute()
		{
		}

		public void Do()
		{
			foreach (var paintTile in paintTiles)
				markerLayerOverlay.SetTile(paintTile.Cell, type);
		}

		public void Undo()
		{
			foreach (var paintTile in paintTiles)
				markerLayerOverlay.SetTile(paintTile.Cell, paintTile.Previous);
		}

		public void Add(CPos target)
		{
			foreach (var cell in markerLayerOverlay.CalculateMirrorPositions(target))
			{
				var existing = markerLayerOverlay.CellLayer[cell];
				if (existing == type)
					continue;

				paintTiles.Add(new PaintMarkerTile(cell, existing));
				markerLayerOverlay.SetTile(cell, type);
			}

			if (type != null)
				Text = TranslationProvider.GetString(AddedMarkerTiles, Translation.Arguments("amount", paintTiles.Count, "type", type));
			else
				Text = TranslationProvider.GetString(RemovedMarkerTiles, Translation.Arguments("amount", paintTiles.Count));
		}
	}

	class ClearSelectedMarkerTilesEditorAction : IEditorAction
	{
		[TranslationReference("amount", "type")]
		const string ClearedSelectedMarkerTiles = "notification-cleared-selected-marker-tiles";

		public string Text { get; }

		readonly MarkerLayerOverlay markerLayerOverlay;
		readonly HashSet<CPos> tiles;
		readonly int tile;

		public ClearSelectedMarkerTilesEditorAction(
			int tile,
			MarkerLayerOverlay markerLayerOverlay)
		{
			this.tile = tile;
			this.markerLayerOverlay = markerLayerOverlay;

			tiles = new HashSet<CPos>(markerLayerOverlay.Tiles[tile]);

			Text = TranslationProvider.GetString(ClearedSelectedMarkerTiles, Translation.Arguments("amount", tiles.Count, "type", tile));
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			markerLayerOverlay.ClearSelected(tile);
		}

		public void Undo()
		{
			markerLayerOverlay.SetSelected(tile, tiles);
		}
	}

	class ClearAllMarkerTilesEditorAction : IEditorAction
	{
		[TranslationReference("amount")]
		const string ClearedAllMarkerTiles = "notification-cleared-all-marker-tiles";

		public string Text { get; }

		readonly MarkerLayerOverlay markerLayerOverlay;
		readonly Dictionary<int, HashSet<CPos>> tiles;

		public ClearAllMarkerTilesEditorAction(
			MarkerLayerOverlay markerLayerOverlay)
		{
			this.markerLayerOverlay = markerLayerOverlay;
			tiles = new Dictionary<int, HashSet<CPos>>(markerLayerOverlay.Tiles);

			var allTilesCount = tiles.Values.Select(x => x.Count).Sum();

			Text = TranslationProvider.GetString(ClearedAllMarkerTiles, Translation.Arguments("amount", allTilesCount));
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			markerLayerOverlay.ClearAll();
		}

		public void Undo()
		{
			markerLayerOverlay.SetAll(tiles);
		}
	}
}
