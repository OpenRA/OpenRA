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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class AdvancedSettingsLogic : ChromeLogic
	{
		readonly DebugSettings debugSettings;
		readonly GameSettings gameSettings;
		readonly ServerSettings serverSettings;
		static ServerSettings originalServerSettings;

		[ObjectCreator.UseCtor]
		public AdvancedSettingsLogic(ModData modData, SettingsLogic settingsLogic, string panelID, string label)
		{
			debugSettings = modData.GetSettings<DebugSettings>();
			gameSettings = modData.GetSettings<GameSettings>();
			serverSettings = modData.GetSettings<ServerSettings>();
			originalServerSettings ??= serverSettings.Clone();
			settingsLogic.RegisterSettingsPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");

			// Advanced
			SettingsUtils.BindCheckboxPref(panel, "NAT_DISCOVERY", serverSettings, "DiscoverNatDevices");
			SettingsUtils.BindCheckboxPref(panel, "PERFTEXT_CHECKBOX", debugSettings, "PerfText");
			SettingsUtils.BindCheckboxPref(panel, "PERFGRAPH_CHECKBOX", debugSettings, "PerfGraph");
			SettingsUtils.BindCheckboxPref(panel, "FETCH_NEWS_CHECKBOX", gameSettings, "FetchNews");
			SettingsUtils.BindCheckboxPref(panel, "SENDSYSINFO_CHECKBOX", debugSettings, "SendSystemInformation");
			SettingsUtils.BindCheckboxPref(panel, "CHECK_VERSION_CHECKBOX", debugSettings, "CheckVersion");

			var ssi = panel.Get<CheckboxWidget>("SENDSYSINFO_CHECKBOX");
			ssi.IsDisabled = () => !gameSettings.FetchNews;

			// Developer
			SettingsUtils.BindCheckboxPref(panel, "BOTDEBUG_CHECKBOX", debugSettings, "BotDebug");
			SettingsUtils.BindCheckboxPref(panel, "LUADEBUG_CHECKBOX", debugSettings, "LuaDebug");
			SettingsUtils.BindCheckboxPref(panel, "REPLAY_COMMANDS_CHECKBOX", debugSettings, "EnableDebugCommandsInReplays");
			SettingsUtils.BindCheckboxPref(panel, "CHECKUNSYNCED_CHECKBOX", debugSettings, "SyncCheckUnsyncedCode");
			SettingsUtils.BindCheckboxPref(panel, "CHECKBOTSYNC_CHECKBOX", debugSettings, "SyncCheckBotModuleCode");
			SettingsUtils.BindCheckboxPref(panel, "PERFLOGGING_CHECKBOX", debugSettings, "EnableSimulationPerfLogging");

			panel.Get("BOTDEBUG_CHECKBOX_CONTAINER").IsVisible = () => debugSettings.DisplayDeveloperSettings;
			panel.Get("CHECKUNSYNCED_CHECKBOX_CONTAINER").IsVisible = () => debugSettings.DisplayDeveloperSettings;
			panel.Get("CHECKBOTSYNC_CHECKBOX_CONTAINER").IsVisible = () => debugSettings.DisplayDeveloperSettings;
			panel.Get("LUADEBUG_CHECKBOX_CONTAINER").IsVisible = () => debugSettings.DisplayDeveloperSettings;
			panel.Get("REPLAY_COMMANDS_CHECKBOX_CONTAINER").IsVisible = () => debugSettings.DisplayDeveloperSettings;
			panel.Get("PERFLOGGING_CHECKBOX_CONTAINER").IsVisible = () => debugSettings.DisplayDeveloperSettings;
			panel.Get("DEBUG_HIDDEN_CONTAINER").IsVisible = () => !debugSettings.DisplayDeveloperSettings;

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			return () => serverSettings.DiscoverNatDevices != originalServerSettings.DiscoverNatDevices;
		}

		Action ResetPanel(Widget panel)
		{
			var defaultDebugSettings = new DebugSettings();
			var defaultServerSettings = new ServerSettings();

			return () =>
			{
				serverSettings.DiscoverNatDevices = defaultServerSettings.DiscoverNatDevices;
				debugSettings.PerfText = defaultDebugSettings.PerfText;
				debugSettings.PerfGraph = defaultDebugSettings.PerfGraph;
				debugSettings.SyncCheckUnsyncedCode = defaultDebugSettings.SyncCheckUnsyncedCode;
				debugSettings.SyncCheckBotModuleCode = defaultDebugSettings.SyncCheckBotModuleCode;
				debugSettings.BotDebug = defaultDebugSettings.BotDebug;
				debugSettings.LuaDebug = defaultDebugSettings.LuaDebug;
				debugSettings.SendSystemInformation = defaultDebugSettings.SendSystemInformation;
				debugSettings.CheckVersion = defaultDebugSettings.CheckVersion;
				debugSettings.EnableDebugCommandsInReplays = defaultDebugSettings.EnableDebugCommandsInReplays;
				debugSettings.EnableSimulationPerfLogging = defaultDebugSettings.EnableSimulationPerfLogging;
			};
		}
	}
}
