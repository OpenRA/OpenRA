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

			action = new AddResourcesEditorAction(resourceType, resourceLayer);
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

			if (mi.Button == MouseButton.Left && mi.Event != MouseInputEvent.Up && resourceLayer.CanAddResource(ResourceType, cell))
			{
				action.Add(new CellResource(cell, resourceLayer.GetResource(cell), ResourceType));
				resourceAdded = true;
			}
			else if (resourceAdded && mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				editorActionManager.Add(action);
				action = new AddResourcesEditorAction(ResourceType, resourceLayer);
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

		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr) { return preview; }
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr) { yield break; }

		public void Tick() { }

		public void Dispose() { }
	}

	readonly record struct CellResource(CPos Cell, ResourceLayerContents OldResourceTile, string NewResourceType);

	sealed class AddResourcesEditorAction : IEditorAction
	{
		[FluentReference("amount", "type")]
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
		}

		public void Do()
		{
			foreach (var resourceCell in cellResources)
			{
				resourceLayer.ClearResources(resourceCell.Cell);
				resourceLayer.AddResource(resourceCell.NewResourceType, resourceCell.Cell, resourceLayer.GetMaxDensity(resourceCell.NewResourceType));
			}
		}

		public void Undo()
		{
			foreach (var resourceCell in cellResources)
			{
				resourceLayer.ClearResources(resourceCell.Cell);
				if (resourceCell.OldResourceTile.Type != null)
					resourceLayer.AddResource(resourceCell.OldResourceTile.Type, resourceCell.Cell, resourceCell.OldResourceTile.Density);
			}
		}

		public void Add(CellResource resourceCell)
		{
			resourceLayer.ClearResources(resourceCell.Cell);
			resourceLayer.AddResource(resourceCell.NewResourceType, resourceCell.Cell, resourceLayer.GetMaxDensity(resourceCell.NewResourceType));
			cellResources.Add(resourceCell);
			Text = FluentProvider.GetMessage(AddedResource, "amount", cellResources.Count, "type", resourceType);
		}
	}
}
