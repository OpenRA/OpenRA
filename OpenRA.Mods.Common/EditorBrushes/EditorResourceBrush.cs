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
		readonly EditorCursorLayer editorCursor;
		readonly IResourceLayer resourceLayer;
		readonly int cursorToken;

		AddResourcesEditorAction action;
		bool resourceAdded;

		public EditorResourceBrush(EditorViewportControllerWidget editorWidget, string resourceType, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			ResourceType = resourceType;
			worldRenderer = wr;
			world = wr.World;
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			editorCursor = world.WorldActor.Trait<EditorCursorLayer>();
			resourceLayer = world.WorldActor.Trait<IResourceLayer>();
			action = new AddResourcesEditorAction(resourceType, resourceLayer);

			cursorToken = editorCursor.SetResource(wr, resourceType);
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

			if (editorCursor.CurrentToken != cursorToken)
				return false;

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

		public void Tick() { }

		public void Dispose()
		{
			editorCursor.Clear(cursorToken);
		}
	}

	readonly struct CellResource
	{
		public readonly CPos Cell;
		public readonly ResourceLayerContents OldResourceTile;
		public readonly string NewResourceType;

		public CellResource(CPos cell, ResourceLayerContents oldResourceTile, string newResourceType)
		{
			Cell = cell;
			OldResourceTile = oldResourceTile;
			NewResourceType = newResourceType;
		}
	}

	class AddResourcesEditorAction : IEditorAction
	{
		public string Text { get; private set; }

		readonly IResourceLayer resourceLayer;
		readonly string resourceType;
		readonly List<CellResource> cellResources = new List<CellResource>();

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

			var cellText = cellResources.Count != 1 ? "cells" : "cell";
			Text = $"Added {cellResources.Count} {cellText} of {resourceType}";
		}
	}
}
