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
using System.Collections.Immutable;
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

		readonly List<PaintMarkerTile> paintTiles = [];
		bool painting;
		CPos cell;

		public EditorMarkerLayerBrush(EditorViewportControllerWidget editorWidget, int? id, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;

			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			markerLayerOverlay = world.WorldActor.Trait<MarkerLayerOverlay>();

			Template = id;
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses left and right mouse buttons, but nothing else.
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

			if (mi.Button != MouseButton.Left)
				return true;

			if (mi.Event == MouseInputEvent.Up)
			{
				UpdatePreview();
				if (paintTiles.Count != 0)
				{
					editorActionManager.Add(new PaintMarkerTileEditorAction(Template, paintTiles.ToImmutableArray(), markerLayerOverlay));
					paintTiles.Clear();
					UpdatePreview(true);
				}

				painting = false;
			}
			else
			{
				painting = true;
				UpdatePreview();
			}

			return true;
		}

		void UpdatePreview(bool forceRefresh = false)
		{
			var currentCell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (!forceRefresh && cell == currentCell)
				return;

			cell = currentCell;

			if (!painting)
			{
				foreach (var paintTile in paintTiles)
					markerLayerOverlay.SetTile(paintTile.Cell, paintTile.Previous);

				paintTiles.Clear();
			}

			foreach (var cell in markerLayerOverlay.CalculateMirrorPositions(cell))
			{
				if (paintTiles.Any(t => t.Cell == cell))
					continue;

				var existing = markerLayerOverlay.CellLayer[cell];
				if (existing == Template)
					continue;

				paintTiles.Add(new PaintMarkerTile(cell, existing));
				markerLayerOverlay.SetTile(cell, Template);
			}
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self) { UpdatePreview(); }
		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr) { yield break; }
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr) { yield break; }

		public void Tick() { }

		public void Dispose()
		{
			foreach (var paintTile in paintTiles)
				markerLayerOverlay.SetTile(paintTile.Cell, paintTile.Previous);
		}
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

	sealed class PaintMarkerTileEditorAction : IEditorAction
	{
		[FluentReference("count", "type")]
		const string AddedMarkerTiles = "notification-added-marker-tiles";

		[FluentReference("count")]
		const string RemovedMarkerTiles = "notification-removed-marker-tiles";

		public string Text { get; }

		readonly int? type;
		readonly MarkerLayerOverlay markerLayerOverlay;

		readonly ImmutableArray<PaintMarkerTile> paintTiles = [];

		public PaintMarkerTileEditorAction(
			int? type,
			ImmutableArray<PaintMarkerTile> paintTiles,
			MarkerLayerOverlay markerLayerOverlay)
		{
			this.type = type;
			this.paintTiles = paintTiles;
			this.markerLayerOverlay = markerLayerOverlay;

			if (type != null)
			{
				var typeLabel = FluentProvider.GetMessage(markerLayerOverlay.Info.Colors.ElementAt(type.Value).Key);
				Text = FluentProvider.GetMessage(AddedMarkerTiles, "count", paintTiles.Length, "type", typeLabel);
			}
			else
				Text = FluentProvider.GetMessage(RemovedMarkerTiles, "count", paintTiles.Length);
		}

		public void Execute() { }

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
	}

	sealed class ClearSelectedMarkerTilesEditorAction : IEditorAction
	{
		[FluentReference("count", "type")]
		const string ClearedSelectedMarkerTiles = "notification-cleared-selected-marker-tiles";

		public string Text { get; }

		readonly MarkerLayerOverlay markerLayerOverlay;
		readonly ImmutableArray<CPos> tiles;
		readonly int tile;

		public ClearSelectedMarkerTilesEditorAction(
			int tile,
			MarkerLayerOverlay markerLayerOverlay)
		{
			this.tile = tile;
			this.markerLayerOverlay = markerLayerOverlay;

			tiles = markerLayerOverlay.Tiles[tile].ToImmutableArray();
			var typeLabel = FluentProvider.GetMessage(markerLayerOverlay.Info.Colors.ElementAt(tile).Key);
			Text = FluentProvider.GetMessage(ClearedSelectedMarkerTiles, "count", tiles.Length, "type", typeLabel);
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
			markerLayerOverlay.SetSelected(tile, tiles.AsSpan());
		}
	}

	sealed class ClearAllMarkerTilesEditorAction : IEditorAction
	{
		[FluentReference("count")]
		const string ClearedAllMarkerTiles = "notification-cleared-all-marker-tiles";

		public string Text { get; }

		readonly MarkerLayerOverlay markerLayerOverlay;
		readonly ImmutableDictionary<int, ImmutableArray<CPos>> tiles;

		public ClearAllMarkerTilesEditorAction(
			MarkerLayerOverlay markerLayerOverlay)
		{
			this.markerLayerOverlay = markerLayerOverlay;
			tiles = markerLayerOverlay.Tiles.ToImmutableDictionary(t => t.Key, t => t.Value.ToImmutableArray());
			var allTilesCount = tiles.Values.Sum(x => x.Length);

			Text = FluentProvider.GetMessage(ClearedAllMarkerTiles, "count", allTilesCount);
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
