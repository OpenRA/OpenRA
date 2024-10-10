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
using OpenRA.Widgets;
using Color = OpenRA.Primitives.Color;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorViewportControllerWidget : Widget
	{
		[Desc("Main color of the selection grid.")]
		public readonly Color SelectionMainColor = Color.White;

		[Desc("Alternate color of the selection grid.")]
		public readonly Color SelectionAltColor = Color.Black;

		[Desc("Main color of the copy / paste grid.")]
		public readonly Color PasteColor = Color.FromArgb(0xFF4CFF00);

		public IEditorBrush CurrentBrush { get; private set; }

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate;
		public readonly EditorDefaultBrush DefaultBrush;

		public event Action BrushChanged;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly WorldRenderer worldRenderer;
		readonly EditorCursorLayer editorCursor;
		public int2 SelectionAltOffset { get; }

		bool enableTooltips;

		[ObjectCreator.UseCtor]
		public EditorViewportControllerWidget(WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			CurrentBrush = DefaultBrush = new EditorDefaultBrush(this, worldRenderer);

			editorCursor = worldRenderer.World.WorldActor.Trait<EditorCursorLayer>();
			editorCursor.SetBrush(CurrentBrush);

			// Allow zooming out to full map size
			worldRenderer.Viewport.UnlockMinimumZoom(0.25f);

			SelectionAltOffset = worldRenderer.World.Map.Grid.Type == MapGridType.Rectangular
				? new int2(1, 1)
				: new int2(0, 1);
		}

		public void ClearBrush() { SetBrush(null); }
		public void SetBrush(IEditorBrush brush)
		{
			if (CurrentBrush != DefaultBrush)
				CurrentBrush?.Dispose();

			CurrentBrush = brush ?? DefaultBrush;

			BrushChanged?.Invoke();
			editorCursor.SetBrush(CurrentBrush);
		}

		public override void MouseEntered()
		{
			enableTooltips = true;
		}

		public override void MouseExited()
		{
			tooltipContainer.Value.RemoveTooltip();
			enableTooltips = false;
		}

		public void SetTooltip(string tooltip)
		{
			if (!enableTooltips)
				return;

			if (tooltip != null)
			{
				Func<string> getTooltip = () => tooltip;
				tooltipContainer.Value.SetTooltip(TooltipTemplate, new WidgetArgs() { { "getText", getTooltip } });
			}
			else
				tooltipContainer.Value.RemoveTooltip();
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Scroll && mi.Modifiers.HasModifier(Game.Settings.Game.ZoomModifier))
			{
				worldRenderer.Viewport.AdjustZoom(mi.Delta.Y * Game.Settings.Game.ZoomSpeed, mi.Location);
				return true;
			}

			if (CurrentBrush.HandleMouseInput(mi))
				return true;

			return base.HandleMouseInput(mi);
		}

		WPos cachedViewportPosition;
		public override void Tick()
		{
			// Clear any tooltips when the viewport is scrolled using the keyboard
			if (worldRenderer.Viewport.CenterPosition != cachedViewportPosition)
				SetTooltip(null);

			cachedViewportPosition = worldRenderer.Viewport.CenterPosition;
			CurrentBrush.Tick();
		}
	}
}
