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
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("StatisticsBasicKey", "StatisticsEconomyKey", "StatisticsProductionKey", "StatisticsCombatKey", "StatisticsGraphKey")]
	public class MenuButtonsChromeLogic : ChromeLogic
	{
		readonly World world;
		readonly Widget worldRoot;
		readonly Widget menuRoot;
		readonly string clickSound = ChromeMetrics.Get<string>("ClickSound");

		bool disableSystemButtons;
		Widget currentWidget;

		[ObjectCreator.UseCtor]
		public MenuButtonsChromeLogic(Widget widget, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
		{
			this.world = world;

			worldRoot = Ui.Root.Get("WORLD_ROOT");
			menuRoot = Ui.Root.Get("MENU_ROOT");

			MiniYaml yaml;
			string[] keyNames = Enum.GetNames(typeof(ObserverStatsPanel));
			var statsHotkeys = new HotkeyReference[keyNames.Length];
			for (var i = 0; i < keyNames.Length; i++)
				statsHotkeys[i] = logicArgs.TryGetValue("Statistics" + keyNames[i] + "Key", out yaml) ? modData.Hotkeys[yaml.Value] : new HotkeyReference();

			// System buttons
			var options = widget.GetOrNull<MenuButtonWidget>("OPTIONS_BUTTON");
			if (options != null)
			{
				var blinking = false;
				var lp = world.LocalPlayer;
				options.IsDisabled = () => disableSystemButtons;
				options.OnClick = () =>
				{
					blinking = false;
					OpenMenuPanel(options, new WidgetArgs()
					{
						{ "activePanel", IngameInfoPanel.AutoSelect }
					});
				};
				options.IsHighlighted = () => blinking && Game.LocalTick % 50 < 25;

				if (lp != null)
				{
					Action<Player, bool> startBlinking = (player, inhibitAnnouncement) =>
					{
						if (!inhibitAnnouncement && player == world.LocalPlayer)
							blinking = true;
					};

					var mo = lp.PlayerActor.TraitOrDefault<MissionObjectives>();

					if (mo != null)
						mo.ObjectiveAdded += startBlinking;
				}
			}

			var debug = widget.GetOrNull<MenuButtonWidget>("DEBUG_BUTTON");
			if (debug != null)
			{
				// Can't use DeveloperMode.Enabled because there is a hardcoded hack to *always*
				// enable developer mode for singleplayer games, but we only want to show the button
				// if it has been explicitly enabled
				var def = world.Map.Rules.Actors["player"].TraitInfo<DeveloperModeInfo>().CheckboxEnabled;
				var enabled = world.LobbyInfo.GlobalSettings.OptionOrDefault("cheats", def);
				debug.IsVisible = () => enabled;
				debug.IsDisabled = () => disableSystemButtons;
				debug.OnClick = () => OpenMenuPanel(debug, new WidgetArgs()
				{
					{ "activePanel", IngameInfoPanel.Debug }
				});
			}

			var stats = widget.GetOrNull<MenuButtonWidget>("OBSERVER_STATS_BUTTON");
			if (stats != null)
			{
				stats.IsDisabled = () => disableSystemButtons || world.Map.Visibility.HasFlag(MapVisibility.MissionSelector);
				stats.OnClick = () => OpenMenuPanel(stats, new WidgetArgs() { { "activePanel", ObserverStatsPanel.Basic } });
			}

			var keyListener = widget.GetOrNull<LogicKeyListenerWidget>("OBSERVER_KEY_LISTENER");
			if (keyListener != null)
			{
				keyListener.AddHandler(e =>
				{
					if (e.Event == KeyInputEvent.Down && !e.IsRepeat)
					{
						for (var i = 0; i < statsHotkeys.Length; i++)
						{
							if (statsHotkeys[i].IsActivatedBy(e))
							{
								Game.Sound.PlayNotification(modData.DefaultRules, null, "Sounds", clickSound, null);
								OpenMenuPanel(stats, new WidgetArgs() { { "activePanel", i } });
								return true;
							}
						}
					}

					return false;
				});
			}

			if (logicArgs.TryGetValue("ClickSound", out yaml))
				clickSound = yaml.Value;
		}

		void OpenMenuPanel(MenuButtonWidget button, WidgetArgs widgetArgs = null)
		{
			disableSystemButtons = true;
			var cachedPause = world.PredictedPaused;

			if (button.HideIngameUI)
			{
				// Cancel custom input modes (guard, building placement, etc)
				world.CancelInputMode();

				worldRoot.IsVisible = () => false;
			}

			if (button.Pause && world.LobbyInfo.NonBotClients.Count() == 1)
				world.SetPauseState(true);

			var cachedDisableWorldSounds = Game.Sound.DisableWorldSounds;
			if (button.DisableWorldSounds)
				Game.Sound.DisableWorldSounds = true;

			widgetArgs = widgetArgs ?? new WidgetArgs();
			widgetArgs.Add("onExit", () =>
			{
				if (button.HideIngameUI)
					worldRoot.IsVisible = () => true;

				if (button.DisableWorldSounds)
					Game.Sound.DisableWorldSounds = cachedDisableWorldSounds;

				if (button.Pause && world.LobbyInfo.NonBotClients.Count() == 1)
					world.SetPauseState(cachedPause);

				menuRoot.RemoveChild(currentWidget);
				disableSystemButtons = false;
			});

			currentWidget = Game.LoadWidget(world, button.MenuContainer, menuRoot, widgetArgs);
			Game.RunAfterTick(Ui.ResetTooltips);
		}
	}
}
