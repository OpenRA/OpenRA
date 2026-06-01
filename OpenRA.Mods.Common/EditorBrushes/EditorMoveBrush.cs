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
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorMoveBrush : IEditorBrush
	{
		public static readonly Color MoveHighlightColor = Color.Yellow;

		public event Action Changed;

		readonly WorldRenderer worldRenderer;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorDefaultBrush defaultBrush;
		readonly EditorActionManager editorActionManager;
		readonly EditorActorLayer editorActorLayer;
		readonly IResourceLayer resourceLayer;
		readonly Map map;

		EditorBlitSource moveSource;
		CellCoordsRegion sourceRegion;
		IReadOnlySet<CPos> sourceMask;
		CVec offset;

		public CVec Offset => offset;
		public bool HasMoveContent { get; private set; }

		public EditorMoveBrush(EditorViewportControllerWidget editorWidget, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			map = wr.World.Map;
			defaultBrush = editorWidget.DefaultBrush;
			editorActionManager = wr.World.WorldActor.Trait<EditorActionManager>();
			editorActorLayer = wr.World.WorldActor.Trait<EditorActorLayer>();
			resourceLayer = wr.World.WorldActor.TraitOrDefault<IResourceLayer>();

			defaultBrush.SelectionChanged += OnSelectionChanged;
			RefreshFromSelection();
		}

		void OnSelectionChanged()
		{
			RefreshFromSelection();
		}

		public void RefreshFromSelection()
		{
			offset = CVec.Zero;
			if (TryCaptureSelection(out moveSource, out sourceRegion, out sourceMask))
				HasMoveContent = true;
			else
			{
				moveSource = default;
				sourceRegion = default;
				sourceMask = null;
				HasMoveContent = false;
			}

			Changed?.Invoke();
		}

		public bool TryCaptureSelection(out EditorBlitSource source, out CellCoordsRegion region, out IReadOnlySet<CPos> mask)
		{
			source = default;
			region = default;
			mask = null;

			var selection = defaultBrush.Selection;
			if (selection.Area.HasValue)
			{
				region = selection.Area.Value;
				mask = selection.GetAreaMask();
				source = EditorBlit.CopyRegionContents(map, editorActorLayer, resourceLayer, region, MapBlitFilters.All, mask);
				return source.Tiles.Count > 0 || source.Actors.Count > 0;
			}

			if (selection.Actor != null)
			{
				var actor = selection.Actor;
				var cells = actor.Footprint.Keys.ToArray();
				if (cells.Length == 0)
					return false;

				region = CellCoordsRegion.BoundingRegion(cells);
				mask = new HashSet<CPos>(cells);
				source = EditorBlit.CopyRegionContents(map, editorActorLayer, resourceLayer, region, MapBlitFilters.All, mask);
				return source.Tiles.Count > 0 || source.Actors.Count > 0;
			}

			return false;
		}

		public void Nudge(CVec delta)
		{
			if (!HasMoveContent)
				return;

			offset += delta;
			Changed?.Invoke();
		}

		public void ResetOffset()
		{
			if (offset == CVec.Zero)
				return;

			offset = CVec.Zero;
			Changed?.Invoke();
		}

		public bool CanPlace()
		{
			return HasMoveContent && offset != CVec.Zero && IsDestinationValid();
		}

		bool IsDestinationValid()
		{
			var destination = sourceRegion.TopLeft + offset;
			var blitVec = destination - moveSource.CellCoords.TopLeft;
			foreach (var pos in moveSource.Tiles.Keys)
			{
				if (!map.Contains(pos + blitVec))
					return false;
			}

			foreach (var (_, preview) in moveSource.Actors)
			{
				if (!map.Contains(preview.Location + blitVec))
					return false;
			}

			return true;
		}

		public void Place()
		{
			if (!CanPlace())
				return;

			var appliedOffset = offset;
			var destination = sourceRegion.TopLeft + appliedOffset;
			editorActionManager.Add(new MoveRegionEditorAction(
				map,
				resourceLayer,
				editorActorLayer,
				moveSource,
				sourceRegion,
				sourceMask,
				destination));

			RefreshSelectionAfterMove(appliedOffset);
			RefreshFromSelection();
		}

		void RefreshSelectionAfterMove(CVec appliedOffset)
		{
			if (sourceMask != null)
			{
				var shiftedCells = sourceMask.Select(c => c + appliedOffset).ToHashSet();
				defaultBrush.SetSelection(EditorSelection.FromCells(shiftedCells));
				return;
			}

			defaultBrush.SetSelection(EditorSelection.FromRegion(new CellCoordsRegion(
				sourceRegion.TopLeft + appliedOffset,
				sourceRegion.BottomRight + appliedOffset)));
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			return defaultBrush.HandleMouseInput(mi);
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self) { }

		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			if (!HasMoveContent)
				yield break;

			var previewOffset = offset;
			var previewPosition = sourceRegion.TopLeft + previewOffset;
			var blitOffset = previewPosition - moveSource.CellCoords.TopLeft;

			foreach (var renderable in EditorBlit.PreviewBlitSource(
				moveSource,
				MapBlitFilters.All,
				blitOffset,
				wr,
				stickToGround: false))
				yield return renderable;
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!HasMoveContent)
				yield break;

			var sourceCells = EditorBlit.GetBlitSourceMask(moveSource, CVec.Zero);
			yield return new EditorSelectionAnnotationRenderable(
				sourceCells,
				MoveHighlightColor,
				int2.Zero,
				CVec.Zero);

			if (offset != CVec.Zero)
			{
				var previewCells = EditorBlit.GetBlitSourceMask(moveSource, offset);
				yield return new EditorSelectionAnnotationRenderable(
					previewCells,
					MoveHighlightColor,
					int2.Zero,
					CVec.Zero);
			}
		}

		public void Tick() { }

		public void Dispose()
		{
			defaultBrush.SelectionChanged -= OnSelectionChanged;
		}
	}

	sealed class MoveRegionEditorAction : IEditorAction
	{
		[FluentReference("x", "y")]
		const string MovedArea = "notification-moved-area";

		public string Text { get; }

		readonly EditorBlitSource content;
		readonly EditorBlit pasteBlit;
		readonly HashSet<CPos> cellsToClear;
		readonly Map map;
		readonly IResourceLayer resourceLayer;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorActorPreview[] actorsToRemove;

		public MoveRegionEditorAction(
			Map map,
			IResourceLayer resourceLayer,
			EditorActorLayer editorActorLayer,
			EditorBlitSource content,
			CellCoordsRegion sourceRegion,
			IReadOnlySet<CPos> sourceMask,
			CPos destinationTopLeft)
		{
			this.map = map;
			this.resourceLayer = resourceLayer;
			this.editorActorLayer = editorActorLayer;
			this.content = content;
			actorsToRemove = content.Actors.Values.ToArray();

			var offset = destinationTopLeft - content.CellCoords.TopLeft;
			cellsToClear = ComputeSourceCellsToClear(sourceRegion, sourceMask, offset);

			pasteBlit = new EditorBlit(
				MapBlitFilters.All,
				resourceLayer,
				destinationTopLeft,
				map,
				content,
				editorActorLayer,
				true);

			Text = FluentProvider.GetMessage(
				MovedArea,
				"x", destinationTopLeft.X,
				"y", destinationTopLeft.Y);
		}

		static HashSet<CPos> ComputeSourceCellsToClear(
			CellCoordsRegion sourceRegion,
			IReadOnlySet<CPos> sourceMask,
			CVec offset)
		{
			var sourceCells = new HashSet<CPos>();
			if (sourceMask != null)
				sourceCells.UnionWith(sourceMask);
			else
			{
				foreach (var cell in sourceRegion)
					sourceCells.Add(cell);
			}

			var destCells = sourceCells.Select(c => c + offset).ToHashSet();
			sourceCells.RemoveWhere(destCells.Contains);
			return sourceCells;
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			pasteBlit.Commit();
			RemoveSourceActors();
			ClearSourceCells();
		}

		public void Undo()
		{
			pasteBlit.Revert();
			RestoreSource();
		}

		void RemoveSourceActors()
		{
			if (actorsToRemove.Length == 0)
				return;

			editorActorLayer.RemoveRange(actorsToRemove);
		}

		void ClearSourceCells()
		{
			var defaultTile = map.Rules.TerrainInfo.DefaultTerrainTile;
			foreach (var cell in cellsToClear)
			{
				if (!map.Tiles.Contains(cell))
					continue;

				resourceLayer?.ClearResources(cell);
				map.Tiles[cell] = defaultTile;
				map.Height[cell] = 0;
			}
		}

		void RestoreSource()
		{
			foreach (var (position, tile) in content.Tiles)
			{
				if (!map.Tiles.Contains(position))
					continue;

				var resourceLayerContents = tile.ResourceLayerContents;
				resourceLayer?.ClearResources(position);
				map.Tiles[position] = tile.TerrainTile;
				map.Height[position] = tile.Height;

				if (resourceLayerContents.HasValue &&
					!string.IsNullOrWhiteSpace(resourceLayerContents.Value.Type))
				{
					resourceLayer.AddResource(
						resourceLayerContents.Value.Type,
						position,
						resourceLayerContents.Value.Density);
				}
			}

			if (actorsToRemove.Length == 0)
				return;

			var copies = new List<ActorReference>(actorsToRemove.Length);
			foreach (var preview in actorsToRemove)
			{
				var copy = preview.Export();
				var locationInit = copy.GetOrDefault<LocationInit>();
				if (locationInit != null && !map.Tiles.Contains(locationInit.Value))
					continue;

				copies.Add(copy);
			}

			editorActorLayer.AddRange(CollectionsMarshal.AsSpan(copies));
		}
	}
}
