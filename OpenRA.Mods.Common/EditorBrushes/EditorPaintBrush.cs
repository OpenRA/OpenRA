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
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorPaintBrush : IEditorBrush
	{
		readonly MultiBrushTool tool;
		readonly WorldRenderer worldRenderer;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActionManager editorActionManager;

		CPos mousePosition;
		bool painting;

		public EditorPaintBrush(MultiBrushTool tool, EditorViewportControllerWidget editorWidget, WorldRenderer worldRenderer)
		{
			this.tool = tool;
			this.worldRenderer = worldRenderer;
			this.editorWidget = editorWidget;
			editorActionManager = worldRenderer.World.WorldActor.Trait<EditorActionManager>();
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			mousePosition = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
			tool.SetMousePosition(mousePosition, painting);

			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right)
				return false;

			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Down)
			{
				tool.AddMousePosition(mousePosition);
				painting = true;
			}
			else if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up)
			{
				painting = false;

				if (!tool.CanPaint())
				{
					// Nothing to paint
					tool.ClearMousePositions();
					return true;
				}

				var editorBlit = tool.CreateBlit();
				if (editorBlit != null)
				{
					var action = new PaintBrushEditorAction(editorBlit);
					editorActionManager.Add(action);
				}

				tool.ClearMousePositions();
				return true;
			}
			else if (mi.Button == MouseButton.Right && mi.Event == MouseInputEvent.Down)
			{
				tool.RefreshBlit(true);
				return true;
			}

			return false;
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self) { }
		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr)
		{
			return tool.RenderAboveShroud(wr, mousePosition);
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			return tool.RenderAnnotations(wr, mousePosition, editorWidget);
		}

		public void Tick()
		{
			mousePosition = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
		}

		public void Dispose() { }
	}

	sealed class PaintBrushEditorAction : IEditorAction
	{
		[FluentReference("tiles", "actors")]
		const string Painted = "notification-multi-brush-painted";

		public string Text { get; }

		readonly EditorBlit editorBlit;

		public PaintBrushEditorAction(EditorBlit editorBlit)
		{
			Text = FluentProvider.GetMessage(Painted, "tiles", editorBlit.TileCount(), "actors", editorBlit.ActorCount());
			this.editorBlit = editorBlit;
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
