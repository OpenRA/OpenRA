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
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorCopyPasteBrush : IEditorBrush
	{
		readonly WorldRenderer worldRenderer;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActorLayer editorActorLayer;
		readonly EditorActionManager editorActionManager;
		readonly EditorBlitSource clipboard;
		readonly IResourceLayer resourceLayer;
		readonly Func<MapBlitFilters> getCopyFilters;

		public CPos? PastePreviewPosition { get; private set; }

		public CellRegion Region => clipboard.CellRegion;

		public EditorCopyPasteBrush(
			EditorViewportControllerWidget editorWidget,
			WorldRenderer wr,
			EditorBlitSource clipboard,
			IResourceLayer resourceLayer,
			Func<MapBlitFilters> getCopyFilters)
		{
			this.getCopyFilters = getCopyFilters;
			this.editorWidget = editorWidget;
			this.clipboard = clipboard;
			this.resourceLayer = resourceLayer;
			worldRenderer = wr;

			editorActionManager = wr.World.WorldActor.Trait<EditorActionManager>();
			editorActorLayer = wr.World.WorldActor.Trait<EditorActorLayer>();
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

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				var pastePosition = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
				var editorBlit = new EditorBlit(
					getCopyFilters(),
					resourceLayer,
					pastePosition,
					worldRenderer.World.Map,
					clipboard,
					editorActorLayer,
					true);
				var action = new CopyPasteEditorAction(editorBlit);

				editorActionManager.Add(action);
				return true;
			}

			return false;
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self) { }
		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr) { yield break; }
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (PastePreviewPosition != null)
			{
				yield return new EditorSelectionAnnotationRenderable(Region, editorWidget.SelectionAltColor, editorWidget.SelectionAltOffset, PastePreviewPosition);
				yield return new EditorSelectionAnnotationRenderable(Region, editorWidget.PasteColor, int2.Zero, PastePreviewPosition);
			}
		}

		public void Tick()
		{
			PastePreviewPosition = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
		}

		public void Dispose() { }
	}

	sealed class CopyPasteEditorAction : IEditorAction
	{
		[FluentReference("amount")]
		const string CopiedTiles = "notification-copied-tiles";

		public string Text { get; }

		readonly EditorBlit editorBlit;

		public CopyPasteEditorAction(EditorBlit editorBlit)
		{
			this.editorBlit = editorBlit;

			Text = FluentProvider.GetMessage(CopiedTiles, "amount", editorBlit.TileCount());
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			editorBlit.Commit();
		}

		public void Undo()
		{
			editorBlit.Revert();
		}
	}
}
