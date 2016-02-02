#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorViewportControllerWidget : Widget
	{
		public IEditorBrush CurrentBrush { get; private set; }

		public readonly string TooltipContainer;
		public readonly string TooltipTemplate;
		public bool IsEyedropping { get; private set; }

		readonly Lazy<TooltipContainerWidget> tooltipContainer;
		readonly EditorDefaultBrush defaultBrush;
		readonly World world;
		readonly WorldRenderer worldRenderer;

		bool enableTooltips;

		[ObjectCreator.UseCtor]
		public EditorViewportControllerWidget(World world, WorldRenderer worldRenderer)
		{
			this.world = world;
			this.worldRenderer = worldRenderer;
			tooltipContainer = Exts.Lazy(() => Ui.Root.Get<TooltipContainerWidget>(TooltipContainer));
			CurrentBrush = defaultBrush = new EditorDefaultBrush(this, worldRenderer);
		}

		public void ClearBrush() { SetBrush(null); }

		public void ToggleEyedropping()
		{
			this.IsEyedropping = !this.IsEyedropping;
		}

		public void SetBrush(IEditorBrush brush)
		{
			if (CurrentBrush != null)
				CurrentBrush.Dispose();

			CurrentBrush = brush ?? defaultBrush;
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
			if (mi.Button == MouseButton.Left && mi.Event == MouseInputEvent.Up && IsEyedropping)
			{
				// Handle eydropper tool - interacts with the tile selector logic
				var map = world.Map;
				var mapTiles = map.MapTiles.Value;
				var mapHeight = map.MapHeight.Value;

				var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);

				var tileset = map.Rules.TileSets[worldRenderer.World.Map.Tileset];

				var template = mapTiles[cell].Type;
				var category = tileset.Templates[template].Category;
				var tileSelectorWidget = this.Parent.Get<ContainerWidget>("TILE_WIDGETS");

				var logic = (Logic.TileSelectorLogic)(tileSelectorWidget.LogicObjects[0]);
				CurrentBrush = new EditorTileBrush(this, template, worldRenderer);
				logic.SwitchCategory(worldRenderer, tileset, category);

				ToggleEyedropping();

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
