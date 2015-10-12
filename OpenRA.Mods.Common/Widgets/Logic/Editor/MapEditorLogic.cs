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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MapEditorLogic
	{
		[ObjectCreator.UseCtor]
		public MapEditorLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			var gridButton = widget.GetOrNull<ButtonWidget>("GRID_BUTTON");
			var terrainGeometryTrait = world.WorldActor.Trait<TerrainGeometryOverlay>();

			if (gridButton != null && terrainGeometryTrait != null)
			{
				gridButton.OnClick = () => terrainGeometryTrait.Enabled ^= true;
				gridButton.IsHighlighted = () => terrainGeometryTrait.Enabled;
			}

			var zoomDropdown = widget.GetOrNull<DropDownButtonWidget>("ZOOM_BUTTON");
			if (zoomDropdown != null)
			{
				var selectedZoom = Game.Settings.Graphics.PixelDouble ? 2f : 1f;
				var selectedLabel = selectedZoom.ToString();
				Func<float, ScrollItemWidget, ScrollItemWidget> setupItem = (zoom, itemTemplate) =>
				{
					var item = ScrollItemWidget.Setup(itemTemplate,
						() => selectedZoom == zoom,
						() => { worldRenderer.Viewport.Zoom = selectedZoom = zoom; selectedLabel = zoom.ToString(); });

					var label = zoom.ToString();
					item.Get<LabelWidget>("LABEL").GetText = () => label;

					return item;
				};

				var options = new[] { 2f, 1f, 0.5f, 0.25f };
				zoomDropdown.OnMouseDown = _ => zoomDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options, setupItem);
				zoomDropdown.GetText = () => selectedLabel;
				zoomDropdown.GetKey = _ => Game.Settings.Keys.TogglePixelDoubleKey;
				zoomDropdown.OnKeyPress = e =>
				{
					var key = Hotkey.FromKeyInput(e);
					if (key != Game.Settings.Keys.TogglePixelDoubleKey)
						return;

					var selected = (options.IndexOf(selectedZoom) + 1) % options.Length;
					worldRenderer.Viewport.Zoom = selectedZoom = options[selected];
					selectedLabel = selectedZoom.ToString();
				};
			}

			var coordinateLabel = widget.GetOrNull<LabelWidget>("COORDINATE_LABEL");
			if (coordinateLabel != null)
				coordinateLabel.GetText = () => worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos).ToString();

			var cashLabel = widget.GetOrNull<LabelWidget>("CASH_LABEL");
			if (cashLabel != null)
			{
				var reslayer = worldRenderer.World.WorldActor.TraitsImplementing<EditorResourceLayer>().FirstOrDefault();
				if (reslayer != null)
					cashLabel.GetText = () => "$ {0}".F(reslayer.NetWorth);
			}
		}
	}
}
