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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorResourceBrush : IEditorBrush
	{
		public readonly string ResourceType;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActionManager editorActionManager;
		readonly IResourceLayer resourceLayer;

		AddResourcesEditorAction action;
		bool resourceAdded;

		CPos cell;
		readonly List<IRenderable> preview = [];
		readonly IResourceRenderer[] resourceRenderers;

		public EditorResourceBrush(EditorViewportControllerWidget editorWidget, string resourceType, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			ResourceType = resourceType;
			worldRenderer = wr;
			world = wr.World;
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			resourceLayer = world.WorldActor.Trait<IResourceLayer>();

			resourceRenderers = world.WorldActor.TraitsImplementing<IResourceRenderer>().ToArray();
			cell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(Viewport.LastMousePos));
			UpdatePreview();
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

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

			if (mi.Button == MouseButton.Left && mi.Event != MouseInputEvent.Up)
			{
				var existing = resourceLayer.GetResource(cell);
				if (!string.IsNullOrEmpty(existing.Type) && existing.Type != ResourceType)
					return true;

				if (resourceLayer.CanAddResource(ResourceType, cell))
				{
					action ??= new AddResourcesEditorAction(ResourceType, resourceLayer);
					action.Add(new CellResource(cell, existing));
					resourceAdded = true;
				}
			}
			else if (resourceAdded && mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				editorActionManager.Add(action);
				action = null;
				resourceAdded = false;
			}

			return true;
		}

		void UpdatePreview()
		{
			var pos = world.Map.CenterOfCell(cell);

			preview.Clear();
			preview.AddRange(resourceRenderers.SelectMany(r => r.RenderPreview(worldRenderer, ResourceType, pos)));
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self)
		{
			var currentCell = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (cell != currentCell)
			{
				cell = currentCell;
				UpdatePreview();
			}
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr) { return action == null ? preview : null; }
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr) { yield break; }

		public void Tick() { }

		public void Dispose() { }
	}

	readonly record struct CellResource(CPos Cell, ResourceLayerContents OldResourceTile);
	readonly record struct FilledCellResource(CPos Cell, ResourceLayerContents OldResourceTile, string PlacedType);

	sealed class AddResourcesEditorAction : IEditorAction
	{
		[FluentReference("count", "type")]
		const string AddedResource = "notification-added-resource";

		public string Text { get; private set; }

		readonly IResourceLayer resourceLayer;
		readonly string resourceType;
		readonly List<CellResource> cellResources = [];

		public AddResourcesEditorAction(string resourceType, IResourceLayer resourceLayer)
		{
			this.resourceType = resourceType;
			this.resourceLayer = resourceLayer;
		}

		public void Execute()
		{
			cellResources.TrimExcess();
		}

		public void Do()
		{
			foreach (var resourceCell in cellResources)
				resourceLayer.AddResource(resourceType, resourceCell.Cell, resourceLayer.GetMaxDensity(resourceType));
		}

		public void Undo()
		{
			foreach (var resourceCell in cellResources)
			{
				// If resources match, simulate a replace command.
				if (resourceCell.OldResourceTile.Type == resourceType || resourceCell.OldResourceTile.Type == null)
					resourceLayer.ClearResources(resourceCell.Cell);

				if (resourceCell.OldResourceTile.Type == resourceType || resourceCell.OldResourceTile.Type != null)
					resourceLayer.AddResource(resourceCell.OldResourceTile.Type, resourceCell.Cell, resourceCell.OldResourceTile.Density);
			}
		}

		public void Add(CellResource resourceCell)
		{
			resourceLayer.AddResource(resourceType, resourceCell.Cell, resourceLayer.GetMaxDensity(resourceType));
			cellResources.Add(resourceCell);
			Text = FluentProvider.GetMessage(AddedResource, "count", cellResources.Count, "type", resourceType);
		}
	}

	sealed class FillSelectionWithResourceEditorAction : IEditorAction
	{
		[FluentReference("count", "type")]
		const string FilledResource = "notification-filled-resource";

		public string Text { get; private set; }

		readonly IResourceLayer resourceLayer;
		readonly string[] resourceTypes;
		readonly EditorAssetMixMode mixMode;
		readonly int fillDensityPercent;
		readonly EditorFillMode fillMode;
		readonly CellCoordsRegion area;
		readonly IReadOnlySet<CPos> mask;
		readonly List<FilledCellResource> filledCells = [];
		int nextResourceType;

		public FillSelectionWithResourceEditorAction(
			IResourceLayer resourceLayer,
			string resourceType,
			EditorAssetMixMode mixMode,
			int fillDensityPercent,
			EditorFillMode fillMode,
			CellCoordsRegion area,
			IReadOnlySet<CPos> mask = null)
			: this(resourceLayer, [resourceType], mixMode, fillDensityPercent, fillMode, area, mask) { }

		public FillSelectionWithResourceEditorAction(
			IResourceLayer resourceLayer,
			IEnumerable<string> resourceTypes,
			EditorAssetMixMode mixMode,
			int fillDensityPercent,
			EditorFillMode fillMode,
			CellCoordsRegion area,
			IReadOnlySet<CPos> mask = null)
		{
			this.resourceLayer = resourceLayer;
			this.resourceTypes = resourceTypes.Distinct().ToArray();
			this.mixMode = mixMode;
			this.fillDensityPercent = fillDensityPercent.Clamp(10, 100);
			this.fillMode = fillMode;
			this.area = area;
			this.mask = mask;
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var cells = mask != null ? mask.ToList() : area.ToList();

			foreach (var cell in EditorFillSelection.SelectCells(cells, fillDensityPercent))
				PlaceResourceAt(cell);

			Text = FluentProvider.GetMessage(FilledResource, "count", filledCells.Count, "type", resourceTypes[0]);
		}

		void PlaceResourceAt(CPos cell)
		{
			var resourceType = PickResourceType();
			var existing = resourceLayer.GetResource(cell);

			if (fillMode == EditorFillMode.Overlap
				&& !string.IsNullOrEmpty(existing.Type)
				&& existing.Type != resourceType)
				return;

			if (!resourceLayer.CanAddResource(resourceType, cell))
				return;

			filledCells.Add(new FilledCellResource(cell, existing, resourceType));
			resourceLayer.AddResource(resourceType, cell, resourceLayer.GetMaxDensity(resourceType));
		}

		public void Undo()
		{
			foreach (var resourceCell in filledCells)
			{
				if (resourceCell.OldResourceTile.Type == resourceCell.PlacedType || resourceCell.OldResourceTile.Type == null)
					resourceLayer.ClearResources(resourceCell.Cell);

				if (resourceCell.OldResourceTile.Type == resourceCell.PlacedType || resourceCell.OldResourceTile.Type != null)
					resourceLayer.AddResource(resourceCell.OldResourceTile.Type, resourceCell.Cell, resourceCell.OldResourceTile.Density);
			}

			filledCells.Clear();
		}

		string PickResourceType()
		{
			if (resourceTypes.Length == 1)
				return resourceTypes[0];

			if (mixMode == EditorAssetMixMode.Sequential)
				return resourceTypes[nextResourceType++ % resourceTypes.Length];

			return resourceTypes[Game.CosmeticRandom.Next(resourceTypes.Length)];
		}
	}
}
