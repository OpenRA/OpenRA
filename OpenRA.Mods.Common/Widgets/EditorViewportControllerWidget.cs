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
		}

		void EditorActionManagerOnChange()
		{
			DefaultBrush.SelectedActor = null;
		}

		public void ClearBrush() { SetBrush(null); }
		public void SetBrush(IEditorBrush brush)
		{
			if (CurrentBrush != null)
				CurrentBrush.Dispose();

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

		void Zoom(int amount)
		{
			var zoomSteps = worldRenderer.Viewport.AvailableZoomSteps;
			var currentZoom = worldRenderer.Viewport.Zoom;

			var nextIndex = zoomSteps.IndexOf(currentZoom) - amount;
			if (nextIndex < 0 || nextIndex >= zoomSteps.Length)
				return;

			var zoom = zoomSteps[nextIndex];
			Parent.Get<DropDownButtonWidget>("ZOOM_BUTTON").SelectedItem = zoom.ToString();
			worldRenderer.Viewport.Zoom = zoom;
		}

		public override bool HandleMouseInput(MouseInput mi)
		{
			if (mi.Event == MouseInputEvent.Scroll &&
				Game.Settings.Game.AllowZoom && mi.Modifiers.HasModifier(Game.Settings.Game.ZoomModifier))
			{
				Zoom(mi.Delta.Y);
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
