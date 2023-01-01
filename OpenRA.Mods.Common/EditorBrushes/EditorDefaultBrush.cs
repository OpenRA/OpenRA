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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

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
		readonly EditorActionManager editorActionManager;
		readonly IResourceLayer resourceLayer;

		public EditorActorPreview SelectedActor;
		int2 worldPixel;

		public EditorDefaultBrush(EditorViewportControllerWidget editorWidget, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;

			editorLayer = world.WorldActor.Trait<EditorActorLayer>();
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
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
			var resourceUnderCursor = resourceLayer?.GetResource(cell).Type;

			if (underCursor != null)
				editorWidget.SetTooltip(underCursor.Tooltip);
			else if (resourceUnderCursor != null)
				editorWidget.SetTooltip(resourceUnderCursor);
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

				if (resourceUnderCursor != null)
					editorActionManager.Add(new RemoveResourceAction(resourceLayer, cell, resourceUnderCursor));
			}

			return true;
		}

		public void Tick() { }
		public void Dispose() { }
	}

	class RemoveActorAction : IEditorAction
	{
		public string Text { get; }

		readonly EditorActorLayer editorActorLayer;
		readonly EditorActorPreview actor;

		public RemoveActorAction(EditorActorLayer editorActorLayer, EditorActorPreview actor)
		{
			this.editorActorLayer = editorActorLayer;
			this.actor = actor;

			Text = $"Removed {actor.Info.Name} ({actor.ID})";
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
		public string Text { get; }

		readonly IResourceLayer resourceLayer;
		readonly CPos cell;

		ResourceLayerContents resourceContents;

		public RemoveResourceAction(IResourceLayer resourceLayer, CPos cell, string resourceType)
		{
			this.resourceLayer = resourceLayer;
			this.cell = cell;

			Text = $"Removed {resourceType}";
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
