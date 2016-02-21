#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class GameInfoBriefingLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public GameInfoBriefingLogic(Widget widget, World world)
		{
			var previewWidget = widget.Get<MapPreviewWidget>("MAP_PREVIEW");
			previewWidget.Preview = () => Game.ModData.MapCache[world.Map.Uid];

			var mapDescriptionPanel = widget.Get<ScrollPanelWidget>("MAP_DESCRIPTION_PANEL");
			var mapDescription = widget.Get<LabelWidget>("MAP_DESCRIPTION");
			var mapFont = Game.Renderer.Fonts[mapDescription.Font];
			var text = world.Map.Description != null ? world.Map.Description.Replace("\\n", "\n") : "";
			text = WidgetUtils.WrapText(text, mapDescription.Bounds.Width, mapFont);
			mapDescription.Text = text;
			mapDescription.Bounds.Height = mapFont.Measure(text).Y;
			mapDescriptionPanel.ScrollToTop();
			mapDescriptionPanel.Layout.AdjustChildren();
		}
	}
}
