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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Scripting;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public enum IngameInfoPanel { AutoSelect, Map, Objectives, Debug, Chat, LobbbyOptions }

	class GameInfoLogic : ChromeLogic
	{
		[TranslationReference]
		const string Objectives = "menu-game-info.objectives";

		[TranslationReference]
		const string Briefing = "menu-game-info.briefing";

		[TranslationReference]
		const string Options = "menu-game-info.options";

		[TranslationReference]
		const string Debug = "menu-game-info.debug";

		[TranslationReference]
		const string Chat = "menu-game-info.chat";

		readonly World world;
		readonly ModData modData;
		readonly Action<bool> hideMenu;
		readonly IObjectivesPanel iop;
		IngameInfoPanel activePanel;
		readonly bool hasError;

		[ObjectCreator.UseCtor]
		public GameInfoLogic(Widget widget, ModData modData, World world, IngameInfoPanel initialPanel, Action<bool> hideMenu)
		{
			var panels = new Dictionary<IngameInfoPanel, (string Panel, string Label, Action<ButtonWidget, Widget> Setup)>()
			{
				{ IngameInfoPanel.Objectives, ("OBJECTIVES_PANEL", Objectives, SetupObjectivesPanel) },
				{ IngameInfoPanel.Map, ("MAP_PANEL", Briefing, SetupMapPanel) },
				{ IngameInfoPanel.LobbbyOptions, ("LOBBY_OPTIONS_PANEL", Options, SetupLobbyOptionsPanel) },
				{ IngameInfoPanel.Debug, ("DEBUG_PANEL", Debug, SetupDebugPanel) },
				{ IngameInfoPanel.Chat, ("CHAT_PANEL", Chat, SetupChatPanel) }
			};

			this.world = world;
			this.modData = modData;
			this.hideMenu = hideMenu;
			activePanel = initialPanel;

			var visiblePanels = new List<IngameInfoPanel>();

			// Objectives/Stats tab
			var scriptContext = world.WorldActor.TraitOrDefault<LuaScript>();
			hasError = scriptContext != null && scriptContext.FatalErrorOccurred;
			iop = world.WorldActor.TraitsImplementing<IObjectivesPanel>().FirstOrDefault();

			if (hasError || (iop != null && iop.PanelName != null))
				visiblePanels.Add(IngameInfoPanel.Objectives);

			// Briefing tab
			var missionData = world.WorldActor.Info.TraitInfoOrDefault<MissionDataInfo>();
			if (missionData != null && !string.IsNullOrEmpty(missionData.Briefing))
				visiblePanels.Add(IngameInfoPanel.Map);

			// Lobby Options tab
			visiblePanels.Add(IngameInfoPanel.LobbbyOptions);

			// Debug/Cheats tab
			// Can't use DeveloperMode.Enabled because there is a hardcoded hack to *always*
			// enable developer mode for singleplayer games, but we only want to show the button
			// if it has been explicitly enabled
			var def = world.Map.Rules.Actors[SystemActors.Player].TraitInfo<DeveloperModeInfo>().CheckboxEnabled;
			var developerEnabled = world.LobbyInfo.GlobalSettings.OptionOrDefault("cheats", def);
			if (world.LocalPlayer != null && developerEnabled)
				visiblePanels.Add(IngameInfoPanel.Debug);

			if (world.LobbyInfo.NonBotClients.Count() > 1)
				visiblePanels.Add(IngameInfoPanel.Chat);

			var numTabs = visiblePanels.Count;
			var tabContainer = !hasError ? widget.GetOrNull($"TAB_CONTAINER_{numTabs}") : null;
			if (tabContainer != null)
				tabContainer.IsVisible = () => true;

			var chatPanel = widget.Get(panels[IngameInfoPanel.Chat].Panel);

			for (var i = 0; i < numTabs; i++)
			{
				var type = visiblePanels[i];
				var info = panels[type];
				var tabButton = tabContainer?.Get<ButtonWidget>($"BUTTON{i + 1}");

				if (tabButton != null)
				{
					tabButton.Text = modData.Translation.GetString(info.Label);
					tabButton.OnClick = () =>
					{
						if (activePanel == IngameInfoPanel.Chat)
							LeaveChatPanel(chatPanel);

						activePanel = type;
					};
					tabButton.IsHighlighted = () => activePanel == type;
				}

				var panelContainer = widget.Get<ContainerWidget>(info.Panel);
				panelContainer.IsVisible = () => activePanel == type;
				info.Setup(tabButton, panelContainer);

				if (activePanel == IngameInfoPanel.AutoSelect)
					activePanel = type;
			}

			var titleText = widget.Get<LabelWidget>("TITLE");

			var mapTitle = world.Map.Title;
			var firstCategory = world.Map.Categories.FirstOrDefault();
			if (firstCategory != null)
				mapTitle = firstCategory + ": " + mapTitle;

			titleText.GetText = () => mapTitle;
		}

		void SetupObjectivesPanel(ButtonWidget objectivesTabButton, Widget objectivesPanelContainer)
		{
			var panel = hasError ? "SCRIPT_ERROR_PANEL" : iop.PanelName;
			Game.LoadWidget(world, panel, objectivesPanelContainer, new WidgetArgs()
			{
				{ "hideMenu", hideMenu }
			});
		}

		void SetupMapPanel(ButtonWidget mapTabButton, Widget mapPanelContainer)
		{
			Game.LoadWidget(world, "MAP_PANEL", mapPanelContainer, new WidgetArgs());
		}

		void SetupLobbyOptionsPanel(ButtonWidget mapTabButton, Widget optionsPanelContainer)
		{
			Game.LoadWidget(world, "LOBBY_OPTIONS_PANEL", optionsPanelContainer, new WidgetArgs()
			{
				{ "getMap", (Func<MapPreview>)(() => modData.MapCache[world.Map.Uid]) },
				{ "configurationDisabled", (Func<bool>)(() => true) }
			});
		}

		void SetupDebugPanel(ButtonWidget debugTabButton, Widget debugPanelContainer)
		{
			if (debugTabButton != null)
				debugTabButton.IsDisabled = () => world.IsGameOver;

			Game.LoadWidget(world, "DEBUG_PANEL", debugPanelContainer, new WidgetArgs());

			if (activePanel == IngameInfoPanel.AutoSelect)
				activePanel = IngameInfoPanel.Debug;
		}

		void SetupChatPanel(ButtonWidget chatTabButton, Widget chatPanelContainer)
		{
			if (chatTabButton != null)
			{
				var lastOnClick = chatTabButton.OnClick;
				chatTabButton.OnClick = () =>
				{
					lastOnClick();
					chatPanelContainer.Get<TextFieldWidget>("CHAT_TEXTFIELD").TakeKeyboardFocus();
				};
			}

			Game.LoadWidget(world, "CHAT_CONTAINER", chatPanelContainer, new WidgetArgs() { { "isMenuChat", true } });
		}

		static void LeaveChatPanel(Widget chatPanelContainer)
		{
			chatPanelContainer.Get<TextFieldWidget>("CHAT_TEXTFIELD").YieldKeyboardFocus();
		}
	}
}
