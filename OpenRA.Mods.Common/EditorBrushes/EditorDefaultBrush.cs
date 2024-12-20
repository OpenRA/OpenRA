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
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public interface IEditorBrush : IDisposable
	{
		bool HandleMouseInput(MouseInput mi);
		void Tick();

		void TickRender(WorldRenderer wr, Actor self);
		IEnumerable<IRenderable> RenderAboveShroud(Actor self, WorldRenderer wr);
		IEnumerable<IRenderable> RenderAnnotations(Actor self, WorldRenderer wr);
	}

	public class EditorSelection
	{
		public CellRegion Area;
		public EditorActorPreview Actor;

		public bool HasSelection => Area != null || Actor != null;
	}

	public sealed class EditorDefaultBrush : IEditorBrush
	{
		const int MinMouseMoveBeforeDrag = 32;

		public event Action SelectionChanged;
		public event Action UpdateSelectedTab;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActorLayer editorLayer;
		readonly EditorActionManager editorActionManager;
		readonly IResourceLayer resourceLayer;
		readonly EditorActorLayer actorLayer;

		public CellRegion CurrentDragBounds => selectionBounds ?? Selection.Area;

		public EditorSelection Selection { get; private set; } = new();

		EditorSelection previousSelection;
		CellRegion selectionBounds;
		int2? selectionStartLocation;
		CPos? selectionStartCell;
		int2 worldPixel;
		bool draggingActor;
		MoveActorAction moveAction;

		public EditorDefaultBrush(EditorViewportControllerWidget editorWidget, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;

			editorLayer = world.WorldActor.Trait<EditorActorLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
			actorLayer = world.WorldActor.Trait<EditorActorLayer>();
		}

		long CalculateActorSelectionPriority(EditorActorPreview actor)
		{
			var centerPixel = new int2(actor.Bounds.X, actor.Bounds.Y);
			var pixelDistance = (centerPixel - worldPixel).Length;

			// If 2+ actors have the same pixel position, then the highest appears on top.
			var worldZPosition = actor.CenterPosition.Z;

			// Sort by pixel distance then in world z position.
			return ((long)pixelDistance << 32) + worldZPosition;
		}

		public void DeleteSelection(MapBlitFilters filters)
		{
			if (Selection.Area != null)
				editorActionManager.Add(new DeleteAreaAction(world.Map, filters, Selection.Area, resourceLayer, actorLayer));
		}

		public void ClearSelection(bool updateSelectedTab = false)
		{
			if (Selection.HasSelection)
			{
				previousSelection = Selection;
				SetSelection(new EditorSelection());
				editorActionManager.Add(new ChangeSelectionAction(this, Selection, previousSelection));

				if (updateSelectedTab)
					UpdateSelectedTab?.Invoke();
			}
		}

		public void SetSelection(EditorSelection selection)
		{
			if (Selection == selection)
				return;

			if (Selection.Actor != null)
				Selection.Actor.Selected = false;

			Selection = selection;
			if (Selection.Actor != null)
				Selection.Actor.Selected = true;

			SelectionChanged?.Invoke();
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses mouse wheel and both mouse buttons, but nothing else.
			// Mouse move events are important for tooltips, so we always allow these through.
			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right
				&& mi.Event != MouseInputEvent.Move && mi.Event != MouseInputEvent.Scroll)
				return false;

			worldPixel = worldRenderer.Viewport.ViewToWorldPx(mi.Location);
			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

			var underCursor = editorLayer.PreviewsAtWorldPixel(worldPixel).MinByOrDefault(CalculateActorSelectionPriority);
			var resourceUnderCursor = resourceLayer?.GetResource(cell).Type;

			if (underCursor != null)
				editorWidget.SetTooltip(underCursor.Tooltip);
			else if (resourceUnderCursor != null)
				editorWidget.SetTooltip(resourceUnderCursor);
			else
				editorWidget.SetTooltip(null);

			// Actor drag.
			if (mi.Button == MouseButton.Left)
			{
				if (mi.Event == MouseInputEvent.Down && underCursor != null && (mi.Modifiers.HasModifier(Modifiers.Shift) || underCursor == Selection.Actor))
				{
					editorWidget.SetTooltip(null);
					var cellViewPx = worldRenderer.Viewport.WorldToViewPx(worldRenderer.ScreenPosition(world.Map.CenterOfCell(cell)));
					var pixelOffset = cellViewPx - mi.Location;
					var cellOffset = underCursor.Location - cell;
					moveAction = new MoveActorAction(underCursor, actorLayer, worldRenderer, pixelOffset, cellOffset);
					draggingActor = true;
					return false;
				}
				else if (mi.Event == MouseInputEvent.Up && draggingActor)
				{
					editorWidget.SetTooltip(null);
					draggingActor = false;
					editorActionManager.Add(moveAction);
					moveAction = null;
					return false;
				}
				else if (mi.Event == MouseInputEvent.Move && draggingActor)
				{
					editorWidget.SetTooltip(null);
					moveAction.Move(mi.Location);
					return false;
				}
			}

			// Selection box drag.
			if (mi.Event == MouseInputEvent.Move &&
				selectionStartLocation != null &&
				(selectionBounds != null || (mi.Location - selectionStartLocation.Value).LengthSquared > MinMouseMoveBeforeDrag))
			{
				selectionStartCell ??= worldRenderer.Viewport.ViewToWorld(selectionStartLocation.Value);

				var topLeft = new CPos(Math.Min(selectionStartCell.Value.X, cell.X), Math.Min(selectionStartCell.Value.Y, cell.Y));
				var bottomRight = new CPos(Math.Max(selectionStartCell.Value.X, cell.X), Math.Max(selectionStartCell.Value.Y, cell.Y));
				var gridType = worldRenderer.World.Map.Grid.Type;

				// We've dragged enough to capture more than one cell, make a selection box.
				if (selectionBounds == null)
				{
					selectionBounds = new CellRegion(gridType, topLeft, bottomRight);

					// Lose focus on any search boxes so we can always copy/paste.
					Ui.KeyboardFocusWidget = null;
				}
				else
				{
					// We already have a drag box; resize it
					selectionBounds = new CellRegion(gridType, topLeft, bottomRight);
				}
			}

			// Finished with mouse move events, so let them bubble up the widget tree.
			if (mi.Event == MouseInputEvent.Move)
				return false;

			if (mi.Event == MouseInputEvent.Down && mi.Button == MouseButton.Left && selectionStartLocation == null)
			{
				// Start area drag.
				selectionStartLocation = mi.Location;
			}

			if (mi.Event == MouseInputEvent.Up)
			{
				if (mi.Button == MouseButton.Left)
				{
					editorWidget.SetTooltip(null);
					selectionStartLocation = null;
					selectionStartCell = null;

					// If we've released a bounds drag.
					if (selectionBounds != null)
					{
						// Set this as the editor selection.
						previousSelection = Selection;
						SetSelection(new EditorSelection
						{
							Area = selectionBounds
						});

						selectionBounds = null;
						editorActionManager.Add(new ChangeSelectionAction(this, Selection, previousSelection));
						UpdateSelectedTab?.Invoke();
					}
					else if (underCursor != null)
					{
						// We've clicked on an actor.
						if (Selection.Actor != underCursor)
						{
							previousSelection = Selection;
							SetSelection(new EditorSelection
							{
								Actor = underCursor,
							});

							editorActionManager.Add(new ChangeSelectionAction(this, Selection, previousSelection));
							UpdateSelectedTab?.Invoke();
						}
					}
					else if (Selection.HasSelection)
					{
						// Released left mouse without dragging or selecting an actor - deselect current.
						ClearSelection(updateSelectedTab: true);
					}
				}
				else if (mi.Button == MouseButton.Right)
				{
					editorWidget.SetTooltip(null);

					// Delete actor.
					if (underCursor != null && underCursor != Selection.Actor && !draggingActor)
						editorActionManager.Add(new RemoveActorAction(editorLayer, underCursor));

					// Or delete resource if found under cursor.
					if (resourceUnderCursor != null)
						editorActionManager.Add(new RemoveResourceAction(resourceLayer, cell, resourceUnderCursor));
				}
			}

			return true;
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self) { }
		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr) { yield break; }
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (CurrentDragBounds != null)
			{
				yield return new EditorSelectionAnnotationRenderable(CurrentDragBounds, editorWidget.SelectionAltColor, editorWidget.SelectionAltOffset, null);
				yield return new EditorSelectionAnnotationRenderable(CurrentDragBounds, editorWidget.SelectionMainColor, int2.Zero, null);
			}
		}

		public void Tick() { }

		public void Dispose() { }
	}

	sealed class ChangeSelectionAction : IEditorAction
	{
		[FluentReference("x", "y", "width", "height")]
		const string SelectedArea = "notification-selected-area";

		[FluentReference("id")]
		const string SelectedActor = "notification-selected-actor";

		[FluentReference]
		const string ClearedSelection = "notification-cleared-selection";

		public string Text { get; }

		readonly EditorSelection selection;
		readonly EditorSelection previousSelection;
		readonly EditorDefaultBrush defaultBrush;

		public ChangeSelectionAction(
			EditorDefaultBrush defaultBrush,
			EditorSelection selection,
			EditorSelection previousSelection)
		{
			this.defaultBrush = defaultBrush;
			this.selection = selection;
			this.previousSelection = new EditorSelection
			{
				Actor = previousSelection.Actor,
				Area = previousSelection.Area
			};

			if (selection.Area != null)
				Text = FluentProvider.GetMessage(SelectedArea,
					"x", selection.Area.TopLeft.X,
					"y", selection.Area.TopLeft.Y,
					"width", selection.Area.BottomRight.X - selection.Area.TopLeft.X,
					"height", selection.Area.BottomRight.Y - selection.Area.TopLeft.Y);
			else if (selection.Actor != null)
				Text = FluentProvider.GetMessage(SelectedActor, "id", selection.Actor.ID);
			else
				Text = FluentProvider.GetMessage(ClearedSelection);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			defaultBrush.SetSelection(selection);
		}

		public void Undo()
		{
			defaultBrush.SetSelection(previousSelection);
		}
	}

	sealed class DeleteAreaAction : IEditorAction
	{
		[FluentReference("x", "y", "width", "height")]
		const string RemovedArea = "notification-removed-area";

		public string Text { get; }

		readonly EditorBlitSource editorBlitSource;
		readonly MapBlitFilters blitFilters;
		readonly IResourceLayer resourceLayer;
		readonly EditorActorLayer editorActorLayer;
		readonly CellRegion area;
		readonly Map map;

		public DeleteAreaAction(Map map, MapBlitFilters blitFilters, CellRegion area, IResourceLayer resourceLayer, EditorActorLayer editorActorLayer)
		{
			this.map = map;
			this.blitFilters = blitFilters;
			this.resourceLayer = resourceLayer;
			this.editorActorLayer = editorActorLayer;
			this.area = area;

			editorBlitSource = EditorBlit.CopyRegionContents(map, editorActorLayer, resourceLayer, area, blitFilters);

			Text = FluentProvider.GetMessage(RemovedArea,
				"x", area.TopLeft.X,
				"y", area.TopLeft.Y,
				"width", area.BottomRight.X - area.TopLeft.X,
				"height", area.BottomRight.Y - area.TopLeft.Y);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			if (blitFilters.HasFlag(MapBlitFilters.Actors))
			{
				// Clear any existing actors in the paste cells.
				foreach (var regionActor in editorActorLayer.PreviewsInCellRegion(area.CellCoords).ToList())
					editorActorLayer.Remove(regionActor);
			}

			foreach (var tileKeyValuePair in editorBlitSource.Tiles)
			{
				var position = tileKeyValuePair.Key;
				if (!map.Tiles.Contains(position))
					continue;

				// Clear any existing resources.
				if (resourceLayer != null && blitFilters.HasFlag(MapBlitFilters.Resources))
					resourceLayer.ClearResources(position);

				if (blitFilters.HasFlag(MapBlitFilters.Terrain))
				{
					map.Tiles[position] = map.Rules.TerrainInfo.DefaultTerrainTile;
					map.Height[position] = 0;
				}
			}
		}

		public void Undo()
		{
			foreach (var tileKeyValuePair in editorBlitSource.Tiles)
			{
				var position = tileKeyValuePair.Key;
				if (!map.Tiles.Contains(position))
					continue;

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
				// Create copies of the original actors, update their locations, and place.
				foreach (var actorKeyValuePair in editorBlitSource.Actors)
				{
					var copy = actorKeyValuePair.Value.Export();
					var locationInit = copy.GetOrDefault<LocationInit>();
					if (locationInit != null)
					{
						if (!map.Tiles.Contains(locationInit.Value))
							continue;

						copy.RemoveAll<LocationInit>();
						copy.Add(new LocationInit(locationInit.Value));
					}

					editorActorLayer.Add(copy);
				}
			}
		}
	}

	sealed class RemoveSelectedActorAction : IEditorAction
	{
		[FluentReference("name", "id")]
		const string RemovedActor = "notification-removed-actor";

		public string Text { get; }

		readonly EditorSelection selection;
		readonly EditorDefaultBrush defaultBrush;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorActorPreview actor;

		public RemoveSelectedActorAction(
			EditorDefaultBrush defaultBrush,
			EditorActorLayer editorActorLayer,
			EditorActorPreview actor)
		{
			this.defaultBrush = defaultBrush;
			this.editorActorLayer = editorActorLayer;
			this.actor = actor;
			selection = new EditorSelection
			{
				Actor = defaultBrush.Selection.Actor
			};

			Text = FluentProvider.GetMessage(RemovedActor, "name", actor.Info.Name, "id", actor.ID);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			defaultBrush.SetSelection(new EditorSelection());
			editorActorLayer.Remove(actor);
		}

		public void Undo()
		{
			editorActorLayer.Add(actor);
			defaultBrush.SetSelection(selection);
		}
	}

	sealed class RemoveActorAction : IEditorAction
	{
		[FluentReference("name", "id")]
		const string RemovedActor = "notification-removed-actor";

		public string Text { get; }

		readonly EditorActorLayer editorActorLayer;
		readonly EditorActorPreview actor;

		public RemoveActorAction(EditorActorLayer editorActorLayer, EditorActorPreview actor)
		{
			this.editorActorLayer = editorActorLayer;
			this.actor = actor;

			Text = FluentProvider.GetMessage(RemovedActor, "name", actor.Info.Name, "id", actor.ID);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			editorActorLayer.Remove(actor);
		}

		public void Undo()
		{
			editorActorLayer.Add(actor);
		}
	}

	sealed class MoveActorAction : IEditorAction
	{
		[FluentReference("id", "x1", "y1", "x2", "y2")]
		const string MovedActor = "notification-moved-actor";

		public string Text { get; private set; }

		readonly EditorActorPreview actor;
		readonly EditorActorLayer layer;
		readonly WorldRenderer worldRenderer;
		readonly int2 pixelOffset;
		readonly CVec cellOffset;
		readonly CPos from;

		CPos to;

		public MoveActorAction(
			EditorActorPreview actor,
			EditorActorLayer layer,
			WorldRenderer worldRenderer,
			int2 pixelOffset,
			CVec cellOffset)
		{
			this.actor = actor;
			this.layer = layer;
			this.worldRenderer = worldRenderer;
			this.pixelOffset = pixelOffset;
			this.cellOffset = cellOffset;

			from = actor.Location;
		}

		public void Execute() { }

		public void Do()
		{
			layer.MoveActor(actor, to);
		}

		public void Undo()
		{
			layer.MoveActor(actor, from);
		}

		public void Move(int2 pixelTo)
		{
			to = worldRenderer.Viewport.ViewToWorld(pixelTo + pixelOffset) + cellOffset;
			layer.MoveActor(actor, to);

			Text = FluentProvider.GetMessage(MovedActor, "id", actor.ID, "x1", from.X, "y1", from.Y, "x2", to.X, "y2", to.Y);
		}
	}

	sealed class RemoveResourceAction : IEditorAction
	{
		[FluentReference("type")]
		const string RemovedResource = "notification-removed-resource";

		public string Text { get; }

		readonly IResourceLayer resourceLayer;
		readonly CPos cell;

		ResourceLayerContents resourceContents;

		public RemoveResourceAction(IResourceLayer resourceLayer, CPos cell, string resourceType)
		{
			this.resourceLayer = resourceLayer;
			this.cell = cell;

			Text = FluentProvider.GetMessage(RemovedResource, "type", resourceType);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			resourceContents = resourceLayer.GetResource(cell);
			resourceLayer.ClearResources(cell);
		}

		public void Undo()
		{
			resourceLayer.ClearResources(cell);
			resourceLayer.AddResource(resourceContents.Type, cell, resourceContents.Density);
		}
	}
}
