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
		readonly IResourceRenderer[] resourceRenderers;

		readonly HashSet<CPos> cells = [];
		readonly List<IRenderable> preview = [];

		AddResourcesEditorAction action;

		bool linePainting;
		CPos lineStart;

		CPos cell;

		public EditorResourceBrush(EditorViewportControllerWidget editorWidget, string resourceType, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			ResourceType = resourceType;
			worldRenderer = wr;
			world = wr.World;
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			resourceLayer = world.WorldActor.Trait<IResourceLayer>();
			resourceRenderers = world.WorldActor.TraitsImplementing<IResourceRenderer>().ToArray();

			ResourceType = resourceType;

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

			if (mi.Button != MouseButton.Left)
				return true;

			if (mi.Event == MouseInputEvent.Down && mi.Modifiers.HasModifier(Modifiers.Shift))
			{
				linePainting = true;
				lineStart = worldRenderer.Viewport.ViewToWorld(mi.Location);
			}
			else if (mi.Event == MouseInputEvent.Up)
			{
				if (action != null)
				{
					editorActionManager.Add(action);
					action = null;
				}

				linePainting = false;
				cells.Clear();
				UpdatePreview();
			}
			else
			{
				UpdatePreview();
				action ??= new AddResourcesEditorAction(ResourceType, resourceLayer);

				action.Replace(cells);
			}

			return true;
		}

		public bool HandleKeyboardInput(KeyInput ki) => false;

		void UpdatePreview()
		{
			var currentCell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (cell == currentCell && cells.Count > 0)
				return;

			cell = currentCell;

			if (linePainting)
			{
				cells.Clear();
				foreach (var cell in Util.GetCurvedLine(lineStart, currentCell, new int2(1, 1)))
					if (world.Map.Contains(cell))
						cells.Add(cell);
			}
			else
			{
				if (action == null)
					cells.Clear();

				if (world.Map.Contains(currentCell))
					cells.Add(currentCell);
			}

			preview.Clear();
			preview.AddRange(resourceRenderers
				.SelectMany(r => r.RenderPreview(worldRenderer, ResourceType, world.Map.CenterOfCell(cell))));
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self)
		{
			UpdatePreview();
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr) { return action == null ? preview : null; }
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr) { yield break; }

		public void Tick() { }

		public void Dispose() { }
	}

	readonly record struct CellResource(CPos Cell, ResourceLayerContents OldResourceTile);

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

		public void Replace(HashSet<CPos> resources)
		{
			Undo();
			cellResources.Clear();
			cellResources.AddRange(resources
				.Where(c => resourceLayer.CanAddResource(resourceType, c))
				.Select(c => new CellResource(c, resourceLayer.GetResource(c))));

			Do();

			Text = FluentProvider.GetMessage(AddedResource, "count", cellResources.Count, "type", resourceType);
		}
	}
}
