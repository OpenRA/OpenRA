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

using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	class GameInfoBriefingLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public GameInfoBriefingLogic(Widget widget, ModData modData, World world)
		{
			var previewWidget = widget.Get<MapPreviewWidget>("MAP_PREVIEW");
			previewWidget.Preview = () => modData.MapCache[world.Map.Uid];

			var mapDescriptionPanel = widget.Get<ScrollPanelWidget>("MAP_DESCRIPTION_PANEL");
			var mapDescription = widget.Get<LabelWidget>("MAP_DESCRIPTION");
			var mapFont = Game.Renderer.Fonts[mapDescription.Font];

			var missionData = world.Map.Rules.Actors[SystemActors.World].TraitInfoOrDefault<MissionDataInfo>();
			if (missionData != null)
			{
				var text = WidgetUtils.WrapText(missionData.Briefing?.Replace("\\n", "\n"), mapDescription.Bounds.Width, mapFont);
				mapDescription.Text = text;
				mapDescription.Bounds.Height = mapFont.Measure(text).Y;
				mapDescriptionPanel.ScrollToTop();
				mapDescriptionPanel.Layout.AdjustChildren();
			}
		}
	}
}
