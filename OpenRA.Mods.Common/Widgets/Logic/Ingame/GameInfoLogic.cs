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

using System.Linq;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public enum IngameInfoPanel { AutoSelect, Map, Objectives, Debug }

	class GameInfoLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public GameInfoLogic(Widget widget, World world, IngameInfoPanel activePanel)
		{
			var lp = world.LocalPlayer;
			var numTabs = 0;

			widget.IsVisible = () => activePanel != IngameInfoPanel.AutoSelect;

			// Objectives/Stats tab
			var scriptContext = world.WorldActor.TraitOrDefault<LuaScript>();
			var hasError = scriptContext != null && scriptContext.FatalErrorOccurred;
			var iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();
			var hasObjectivesPanel = hasError || (iop != null && iop.PanelName != null);

			if (hasObjectivesPanel)
			{
				numTabs++;
				var objectivesTabButton = widget.Get<ButtonWidget>(string.Concat("BUTTON", numTabs.ToString()));
				objectivesTabButton.GetText = () => "Objectives";
				objectivesTabButton.IsVisible = () => numTabs > 1 && !hasError;
				objectivesTabButton.OnClick = () => activePanel = IngameInfoPanel.Objectives;
				objectivesTabButton.IsHighlighted = () => activePanel == IngameInfoPanel.Objectives;

				var panel = hasError ? "SCRIPT_ERROR_PANEL" : iop.PanelName;
				var objectivesPanel = widget.Get<ContainerWidget>("OBJECTIVES_PANEL");
				objectivesPanel.IsVisible = () => activePanel == IngameInfoPanel.Objectives;

				Game.LoadWidget(world, panel, objectivesPanel, new WidgetArgs());

				if (activePanel == IngameInfoPanel.AutoSelect)
					activePanel = IngameInfoPanel.Objectives;
			}

			// Briefing tab
			var missionData = world.WorldActor.Info.TraitInfoOrDefault<MissionDataInfo>();
			if (missionData != null && !string.IsNullOrEmpty(missionData.Briefing))
			{
				numTabs++;
				var mapTabButton = widget.Get<ButtonWidget>(string.Concat("BUTTON", numTabs.ToString()));
				mapTabButton.Text = "Briefing";
				mapTabButton.IsVisible = () => numTabs > 1 && !hasError;
				mapTabButton.OnClick = () => activePanel = IngameInfoPanel.Map;
				mapTabButton.IsHighlighted = () => activePanel == IngameInfoPanel.Map;

				var mapPanel = widget.Get<ContainerWidget>("MAP_PANEL");
				mapPanel.IsVisible = () => activePanel == IngameInfoPanel.Map;

				Game.LoadWidget(world, "MAP_PANEL", mapPanel, new WidgetArgs());

				if (activePanel == IngameInfoPanel.AutoSelect)
					activePanel = IngameInfoPanel.Map;
			}

			// Debug/Cheats tab
			// Can't use DeveloperMode.Enabled because there is a hardcoded hack to *always*
			// enable developer mode for singleplayer games, but we only want to show the button
			// if it has been explicitly enabled
			var def = world.Map.Rules.Actors["player"].TraitInfo<DeveloperModeInfo>().Enabled;
			var developerEnabled = world.LobbyInfo.GlobalSettings.OptionOrDefault("cheats", def);
			if (lp != null && developerEnabled)
			{
				numTabs++;
				var debugTabButton = widget.Get<ButtonWidget>(string.Concat("BUTTON", numTabs.ToString()));
				debugTabButton.Text = "Debug";
				debugTabButton.IsVisible = () => lp != null && numTabs > 1 && !hasError;
				debugTabButton.IsDisabled = () => world.IsGameOver;
				debugTabButton.OnClick = () => activePanel = IngameInfoPanel.Debug;
				debugTabButton.IsHighlighted = () => activePanel == IngameInfoPanel.Debug;

				var debugPanelContainer = widget.Get<ContainerWidget>("DEBUG_PANEL");
				debugPanelContainer.IsVisible = () => activePanel == IngameInfoPanel.Debug;

				Game.LoadWidget(world, "DEBUG_PANEL", debugPanelContainer, new WidgetArgs());

				if (activePanel == IngameInfoPanel.AutoSelect)
					activePanel = IngameInfoPanel.Debug;
			}

			// Handle empty space when tabs aren't displayed
			var titleText = widget.Get<LabelWidget>("TITLE");
			var titleTextNoTabs = widget.GetOrNull<LabelWidget>("TITLE_NO_TABS");

			var mapTitle = world.Map.Title;
			var firstCategory = world.Map.Categories.FirstOrDefault();
			if (firstCategory != null)
				mapTitle = firstCategory + ": " + mapTitle;

			titleText.IsVisible = () => numTabs > 1 || (numTabs == 1 && titleTextNoTabs == null);
			titleText.GetText = () => mapTitle;
			if (titleTextNoTabs != null)
			{
				titleTextNoTabs.IsVisible = () => numTabs == 1;
				titleTextNoTabs.GetText = () => mapTitle;
			}

			var bg = widget.Get<BackgroundWidget>("BACKGROUND");
			var bgNoTabs = widget.GetOrNull<BackgroundWidget>("BACKGROUND_NO_TABS");

			bg.IsVisible = () => numTabs > 1 || (numTabs == 1 && bgNoTabs == null);
			if (bgNoTabs != null)
				bgNoTabs.IsVisible = () => numTabs == 1;
		}
	}
}
