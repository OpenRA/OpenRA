#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Network;
using OpenRA.Widgets;
using System.IO;
using System;
using OpenRA.Graphics;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncMapChooserLogic : IWidgetDelegate
	{
		Map map;
		Widget scrollpanel;
		Widget itemTemplate;
		
		[ObjectCreator.UseCtor]
		internal CncMapChooserLogic([ObjectCreator.Param] Widget widget,
		                            [ObjectCreator.Param] string initialMap,
		                            [ObjectCreator.Param] Action onExit,
		                            [ObjectCreator.Param] Action<Map> onSelect)
		{
			if (string.IsNullOrEmpty(initialMap) || ! Game.modData.AvailableMaps.TryGetValue(initialMap, out map))
				map = Game.modData.AvailableMaps.FirstOrDefault(m => m.Value.Selectable).Value;
			
			var panel = widget.GetWidget("MAPCHOOSER_PANEL");
			
			panel.GetWidget<MapPreviewWidget>("MAP_PREVIEW").Map = () => map;
			panel.GetWidget<LabelWidget>("CURMAP_TITLE").GetText = () => map.Title;
			panel.GetWidget<LabelWidget>("CURMAP_AUTHOR").GetText = () => map.Author;
			panel.GetWidget<LabelWidget>("CURMAP_DESC").GetText = () => map.Description;
			panel.GetWidget<LabelWidget>("CURMAP_DESC_LABEL").IsVisible = () => map.Description != null;
			panel.GetWidget<LabelWidget>("CURMAP_SIZE").GetText = () => "{0}x{1}".F(map.Bounds.Width, map.Bounds.Height);
			panel.GetWidget<LabelWidget>("CURMAP_THEATER").GetText = () => Rules.TileSets[map.Tileset].Name;
			panel.GetWidget<LabelWidget>("CURMAP_PLAYERS").GetText = () => map.PlayerCount.ToString();

			panel.GetWidget<CncMenuButtonWidget>("BUTTON_OK").OnClick = () => { Widget.CloseWindow(); onSelect(map); };
			panel.GetWidget<CncMenuButtonWidget>("BUTTON_CANCEL").OnClick = () => { Widget.CloseWindow(); onExit(); };
			
			panel.GetWidget<CncMenuButtonWidget>("BUTTON_INSTALL").IsDisabled = () => true;
			panel.GetWidget<CncMenuButtonWidget>("BUTTON_INSTALL").OnClick = () => InstallMap();
			
			scrollpanel = panel.GetWidget<ScrollPanelWidget>("MAP_LIST");
			itemTemplate = scrollpanel.GetWidget<ContainerWidget>("MAP_TEMPLATE");
			EnumerateMaps();
		}
		
		void EnumerateMaps()
		{
			scrollpanel.RemoveChildren();
			foreach (var kv in Game.modData.AvailableMaps.OrderBy(kv => kv.Value.Title).OrderBy(kv => kv.Value.PlayerCount))
			{
				var m = kv.Value;
				if (!m.Selectable)
					continue;

				var template = itemTemplate.Clone() as ContainerWidget;
				template.GetBackground = () => (template.RenderBounds.Contains(Viewport.LastMousePos) ? "button-hover" : (m == map) ? "button-pressed" : null);
				template.OnMouseDown = mi => { if (mi.Button != MouseButton.Left) return false; map = m; return true; };
				template.IsVisible = () => true;
				template.GetWidget<LabelWidget>("TITLE").GetText = () => m.Title;
				template.GetWidget<LabelWidget>("PLAYERS").GetText = () => "{0}".F(m.PlayerCount);
				template.GetWidget<LabelWidget>("TYPE").GetText = () => m.Type;
				scrollpanel.AddChild(template);
			}
		}
		
		bool InstallMap()
		{
			Game.Utilities.PromptFilepathAsync("Select an OpenRA map file", path =>
			{
				if (!string.IsNullOrEmpty(path))
					Game.RunAfterTick(() => InstallMapInner(path));
			});
			return true;
		}
		
		void InstallMapInner(string path)
		{
			var toPath = new [] { Platform.SupportDir, "maps", Game.modData.Manifest.Mods[0], Path.GetFileName(path) }.Aggregate(Path.Combine);
			
			// Create directory if required
			var dir = Path.GetDirectoryName(toPath);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			
			// TODO: Attempt to mount the map and verify that
			// it is a valid Game.modData.Manifest.Mods[0] map.
			File.Copy(path, toPath, true);
			Game.modData.ReloadMaps();
			EnumerateMaps();
		}
	}
}
