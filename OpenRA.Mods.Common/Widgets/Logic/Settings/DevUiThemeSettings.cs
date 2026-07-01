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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public static class DevUiThemeSettings
	{
		// Returns an action that re-syncs the in-game player colour from the current theme colour
		// (when "Use in game" is enabled). Call it when the settings panel is saved/left so that
		// colour changes made while enabled are actually applied.
		public static Action Bind(Widget panel, GraphicSettings graphicSettings)
		{
			if (Game.ModData.Manifest.Id != "ra")
				return () => { };

			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");
			if (scrollPanel.GetOrNull<ColorMixerWidget>("DEV_UI_THEME_MIXER") == null)
			{
				Ui.LoadWidget("DEV_UI_THEME_SECTION_HEADER", scrollPanel, []);
				Ui.LoadWidget("DEV_UI_THEME_ROW", scrollPanel, []);
				SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);
			}

			var mixer = scrollPanel.Get<ColorMixerWidget>("DEV_UI_THEME_MIXER");
			var hueSlider = scrollPanel.Get<HueSliderWidget>("DEV_UI_THEME_HUE_SLIDER");
			var preview = scrollPanel.Get<ColorBlockWidget>("DEV_UI_THEME_PREVIEW");
			var defaultButton = scrollPanel.Get<ButtonWidget>("DEV_UI_THEME_DEFAULT_BUTTON");
			var useInGameButton = scrollPanel.Get<ButtonWidget>("DEV_UI_THEME_USE_IN_GAME_BUTTON");
			var useInGameCheck = scrollPanel.Get<CheckboxWidget>("DEV_UI_THEME_USE_IN_GAME_CHECK");

			mixer.SetColorLimits(0, 1, 0, 1);
			mixer.Set(graphicSettings.DevUiThemeColor);
			hueSlider.UpdateValue(graphicSettings.DevUiThemeColor.ToAhsv().H);
			preview.GetColor = () => graphicSettings.DevUiThemeColor;

			void ApplyColor(Color color, bool persist = false)
			{
				graphicSettings.DevUiThemeColor = color;
				DevUiTheme.ApplyTheme(color);
				if (persist)
					graphicSettings.Save();
			}

			mixer.OnChange += () => ApplyColor(mixer.Color);
			hueSlider.OnChange += h =>
			{
				mixer.SetColorLimits(0, 1, 0, 1, h);
				var (_, _, s, v) = mixer.Color.ToAhsv();
				mixer.Set(Color.FromAhsv(h, s, v));
				ApplyColor(mixer.Color);
			};

			defaultButton.OnClick = () =>
			{
				var color = DevUiTheme.DefaultRed;
				mixer.Set(color);
				hueSlider.UpdateValue(color.ToAhsv().H);
				ApplyColor(color, persist: true);
			};

			static Color ThemePlayerColor(GraphicSettings gs)
			{
				// Player colours are opaque RGB; drop any alpha from the theme colour.
				var c = gs.DevUiThemeColor;
				return Color.FromArgb(255, c.R, c.G, c.B);
			}

			// System 1 (enabled): the theme colour is the source of truth. Push it into the player
			//   colour (the Gameplay "Preferred Color"), which skirmish and multiplayer both read.
			// System 2 (disabled): the player colour (preferred colour) is the source of truth, so we
			//   restore the colour the player had before enabling and leave it under their control.
			void SetUseInGame(bool enabled)
			{
				if (enabled)
				{
					// Remember the current preferred colour so it can be restored when turned off.
					graphicSettings.DevUiThemePreviousPlayerColor = Game.Settings.Player.Color;
					Game.Settings.Player.Color = ThemePlayerColor(graphicSettings);
				}
				else
					Game.Settings.Player.Color = graphicSettings.DevUiThemePreviousPlayerColor;

				graphicSettings.DevUiThemeUseInGame = enabled;

				// Commits and persists all settings modules (graphics flag + player colour).
				Game.Settings.Save();
			}

			useInGameButton.OnClick = () => SetUseInGame(!graphicSettings.DevUiThemeUseInGame);

			useInGameCheck.IsChecked = () => graphicSettings.DevUiThemeUseInGame;
			useInGameCheck.OnClick = () => SetUseInGame(!graphicSettings.DevUiThemeUseInGame);

			// While enabled, keep the player colour in sync with the (possibly changed) theme colour
			// when the panel is saved/left. Persistence is handled by the caller's Game.Settings.Save().
			return () =>
			{
				if (graphicSettings.DevUiThemeUseInGame)
					Game.Settings.Player.Color = ThemePlayerColor(graphicSettings);
			};
		}

		public static void Reset(GraphicSettings graphicSettings)
		{
			graphicSettings.DevUiThemeColor = new GraphicSettings().DevUiThemeColor;
			DevUiTheme.ApplyTheme(graphicSettings.DevUiThemeColor);
		}
	}
}
