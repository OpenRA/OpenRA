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

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorViewportControllerWidget : Widget
	{
		public IEditorBrush CurrentBrush { get; private set; }

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate;
		public readonly EditorDefaultBrush DefaultBrush;

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly WorldRenderer worldRenderer;
		readonly EditorActionManager editorActionManager;

		bool enableTooltips;

		[ObjectCreator.UseCtor]
		public EditorViewportControllerWidget(World world, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			CurrentBrush = DefaultBrush = new EditorDefaultBrush(this, worldRenderer);
			editorActionManager = world.WorldActor.Trait<EditorActionManager>();

			editorActionManager.OnChange += EditorActionManagerOnChange;

			// Allow zooming out to full map size
			worldRenderer.Viewport.UnlockMinimumZoom(0.25f);
		}

		void EditorActionManagerOnChange()
		{
			DefaultBrush.SelectedActor = null;
		}

		public void ClearBrush() { SetBrush(null); }
		public void SetBrush(IEditorBrush brush)
		{
			CurrentBrush?.Dispose();

			CurrentBrush = brush ?? DefaultBrush;
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

		public override void Removed()
		{
			base.Removed();
			editorActionManager.OnChange -= EditorActionManagerOnChange;
		}
	}
}
