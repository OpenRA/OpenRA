#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public interface IEditorBrush : IDisposable
	{
		bool HandleMouseInput(MouseInput mi);
		void Tick();
	}

	public sealed class EditorDefaultBrush : IEditorBrush
	{
		public readonly ActorInfo Actor;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActorLayer editorLayer;
		readonly Dictionary<int, ResourceType> resources;
		readonly EditorActionManager editorActionManager;

		public EditorActorPreview SelectedActor;
		int2 worldPixel;

		public EditorDefaultBrush(EditorViewportControllerWidget editorWidget, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;

			editorLayer = world.WorldActor.Trait<EditorActorLayer>();
			resources = world.WorldActor.TraitsImplementing<ResourceType>()
				.ToDictionary(r => r.Info.ResourceType, r => r);

			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
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

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses mouse wheel and both mouse buttons, but nothing else
			// Mouse move events are important for tooltips, so we always allow these through
			if ((mi.Button != MouseButton.Left && mi.Button != MouseButton.Right
				&& mi.Event != MouseInputEvent.Move && mi.Event != MouseInputEvent.Scroll) ||
				mi.Event == MouseInputEvent.Down)
				return false;

			worldPixel = worldRenderer.Viewport.ViewToWorldPx(mi.Location);
			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

			var underCursor = editorLayer.PreviewsAt(worldPixel).MinByOrDefault(CalculateActorSelectionPriority);

			var mapResources = world.Map.Resources;
			ResourceType type = null;
			if (underCursor != null)
				editorWidget.SetTooltip(underCursor.Tooltip);
			else if (mapResources.Contains(cell) && resources.TryGetValue(mapResources[cell].Type, out type))
				editorWidget.SetTooltip(type.Info.Type);
			else
				editorWidget.SetTooltip(null);

			// Finished with mouse move events, so let them bubble up the widget tree
			if (mi.Event == MouseInputEvent.Move)
				return false;

			if (mi.Button == MouseButton.Left)
			{
				editorWidget.SetTooltip(null);
				SelectedActor = underCursor;
			}

			if (mi.Button == MouseButton.Right)
			{
				editorWidget.SetTooltip(null);

				if (underCursor != null && underCursor != SelectedActor)
					editorActionManager.Add(new RemoveActorAction(editorLayer, underCursor));

				if (mapResources.Contains(cell) && mapResources[cell].Type != 0)
					editorActionManager.Add(new RemoveResourceAction(mapResources, cell, type));
			}

			return true;
		}

		public void Tick() { }
		public void Dispose() { }
	}

	class RemoveActorAction : IEditorAction
	{
		public string Text { get; private set; }

		readonly EditorActorLayer editorActorLayer;
		readonly EditorActorPreview actor;

		public RemoveActorAction(EditorActorLayer editorActorLayer, EditorActorPreview actor)
		{
			this.editorActorLayer = editorActorLayer;
			this.actor = actor;

			Text = "Removed {0} ({1})".F(actor.Info.Name, actor.ID);
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

	class RemoveResourceAction : IEditorAction
	{
		public string Text { get; private set; }

		readonly CellLayer<ResourceTile> mapResources;
		readonly CPos cell;

		ResourceTile resourceTile;

		public RemoveResourceAction(CellLayer<ResourceTile> mapResources, CPos cell, ResourceType type)
		{
			this.mapResources = mapResources;
			this.cell = cell;

			Text = "Removed {0}".F(type.Info.TerrainType);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			resourceTile = mapResources[cell];
			mapResources[cell] = default(ResourceTile);
		}

		public void Undo()
		{
			mapResources[cell] = resourceTile;
		}
	}
}
