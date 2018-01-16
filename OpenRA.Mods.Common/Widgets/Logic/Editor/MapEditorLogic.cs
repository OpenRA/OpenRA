#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("ChangeZoomKey")]
	public class MapEditorLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public MapEditorLogic(Widget widget, ModData modData, World world, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			MiniYaml yaml;
			var changeZoomKey = new HotkeyReference();
			if (logicArgs.TryGetValue("ChangeZoomKey", out yaml))
				changeZoomKey = modData.Hotkeys[yaml.Value];

			var editorViewport = widget.Get<EditorViewportControllerWidget>("MAP_EDITOR");

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
				var selectedZoom = (Game.Settings.Graphics.PixelDouble ? 2f : 1f).ToString();

				zoomDropdown.SelectedItem = selectedZoom;
				Func<float, ScrollItemWidget, ScrollItemWidget> setupItem = (zoom, itemTemplate) =>
				{
					var item = ScrollItemWidget.Setup(
						itemTemplate,
						() =>
						{
							return float.Parse(zoomDropdown.SelectedItem) == zoom;
						},
						() =>
						{
							zoomDropdown.SelectedItem = selectedZoom = zoom.ToString();
							worldRenderer.Viewport.Zoom = float.Parse(selectedZoom);
						});

					var label = zoom.ToString();
					item.Get<LabelWidget>("LABEL").GetText = () => label;

					return item;
				};

				var options = worldRenderer.Viewport.AvailableZoomSteps;
				zoomDropdown.OnMouseDown = _ => zoomDropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 150, options, setupItem);
				zoomDropdown.GetText = () => zoomDropdown.SelectedItem;
				zoomDropdown.OnKeyPress = e =>
				{
					if (!changeZoomKey.IsActivatedBy(e))
						return;

					var selected = (options.IndexOf(float.Parse(selectedZoom)) + 1) % options.Length;
					var zoom = options[selected];
					worldRenderer.Viewport.Zoom = zoom;
					selectedZoom = zoom.ToString();
					zoomDropdown.SelectedItem = zoom.ToString();
				};
			}

			var copypasteButton = widget.GetOrNull<ButtonWidget>("COPYPASTE_BUTTON");
			if (copypasteButton != null)
			{
				copypasteButton.OnClick = () => editorViewport.SetBrush(new EditorCopyPasteBrush(editorViewport, worldRenderer));
				copypasteButton.IsHighlighted = () => editorViewport.CurrentBrush is EditorCopyPasteBrush;
			}

			var coordinateLabel = widget.GetOrNull<LabelWidget>("COORDINATE_LABEL");
			if (coordinateLabel != null)
			{
				coordinateLabel.GetText = () =>
				{
					var cell = worldRenderer.Viewport.ViewToWorld(Viewport.LastMousePos);
					var map = worldRenderer.World.Map;
					return map.Height.Contains(cell) ?
						"{0},{1} ({2})".F(cell, map.Height[cell], map.Tiles[cell].Type) : "";
				};
			}

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
