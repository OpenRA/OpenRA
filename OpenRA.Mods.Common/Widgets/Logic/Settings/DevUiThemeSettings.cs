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

using OpenRA.Graphics;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public static class DevUiThemeSettings
	{
		public static void Bind(Widget panel, GraphicSettings graphicSettings)
		{
			if (Game.ModData.Manifest.Id != "ra")
				return;

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
		}

		public static void Reset(GraphicSettings graphicSettings)
		{
			graphicSettings.DevUiThemeColor = new GraphicSettings().DevUiThemeColor;
			DevUiTheme.ApplyTheme(graphicSettings.DevUiThemeColor);
		}
	}
}
