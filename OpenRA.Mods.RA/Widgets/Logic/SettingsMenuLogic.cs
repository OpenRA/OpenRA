#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class SettingsMenuLogic
	{
		Widget bg;

		public SettingsMenuLogic()
		{
			bg = Ui.Root.Get<BackgroundWidget>("SETTINGS_MENU");
			var tabs = bg.Get<ContainerWidget>("TAB_CONTAINER");

			//Tabs
			tabs.Get<ButtonWidget>("GENERAL").OnClick = () => FlipToTab("GENERAL_PANE");
			tabs.Get<ButtonWidget>("AUDIO").OnClick = () => FlipToTab("AUDIO_PANE");
			tabs.Get<ButtonWidget>("DISPLAY").OnClick = () => FlipToTab("DISPLAY_PANE");
			tabs.Get<ButtonWidget>("DEBUG").OnClick = () => FlipToTab("DEBUG_PANE");
			FlipToTab("GENERAL_PANE");

			//General
			var general = bg.Get("GENERAL_PANE");

			var name = general.Get<TextFieldWidget>("NAME");
			name.Text = Game.Settings.Player.Name;
			name.OnLoseFocus = () =>
			{
				name.Text = name.Text.Trim();

				if (name.Text.Length == 0)
					name.Text = Game.Settings.Player.Name;
				else
					Game.Settings.Player.Name = name.Text;
			};
			name.OnEnterKey = () => { name.LoseFocus(); return true; };

			var edgescrollCheckbox = general.Get<CheckboxWidget>("EDGE_SCROLL");
			edgescrollCheckbox.IsChecked = () => Game.Settings.Game.ViewportEdgeScroll;
			edgescrollCheckbox.OnClick = () => Game.Settings.Game.ViewportEdgeScroll ^= true;

			var edgeScrollSlider = general.Get<SliderWidget>("EDGE_SCROLL_AMOUNT");
			edgeScrollSlider.Value = Game.Settings.Game.ViewportEdgeScrollStep;
			edgeScrollSlider.OnChange += x => Game.Settings.Game.ViewportEdgeScrollStep = x;

			var inversescroll = general.Get<CheckboxWidget>("INVERSE_SCROLL");
			inversescroll.IsChecked = () => Game.Settings.Game.MouseScroll == MouseScrollType.Inverted;
			inversescroll.OnClick = () => Game.Settings.Game.MouseScroll = (Game.Settings.Game.MouseScroll == MouseScrollType.Inverted) ? MouseScrollType.Standard : MouseScrollType.Inverted;

			var teamchatCheckbox = general.Get<CheckboxWidget>("TEAMCHAT_TOGGLE");
			teamchatCheckbox.IsChecked = () => Game.Settings.Game.TeamChatToggle;
			teamchatCheckbox.OnClick = () => Game.Settings.Game.TeamChatToggle ^= true;

			var showShellmapCheckbox = general.Get<CheckboxWidget>("SHOW_SHELLMAP");
			showShellmapCheckbox.IsChecked = () => Game.Settings.Game.ShowShellmap;
			showShellmapCheckbox.OnClick = () => Game.Settings.Game.ShowShellmap ^= true;

			// Audio
			var audio = bg.Get("AUDIO_PANE");

			var soundslider = audio.Get<SliderWidget>("SOUND_VOLUME");
			soundslider.OnChange += x => Sound.SoundVolume = x;
			soundslider.Value = Sound.SoundVolume;

			var musicslider = audio.Get<SliderWidget>("MUSIC_VOLUME");
			musicslider.OnChange += x => Sound.MusicVolume = x;
			musicslider.Value = Sound.MusicVolume;

			// Display
			var display = bg.Get("DISPLAY_PANE");
			var gs = Game.Settings.Graphics;

			var windowModeDropdown = display.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => ShowWindowModeDropdown(windowModeDropdown, gs);
			windowModeDropdown.GetText = () => gs.Mode == WindowMode.Windowed ?
				"Windowed" : gs.Mode == WindowMode.Fullscreen ? "Fullscreen" : "Pseudo-Fullscreen";

			display.Get("WINDOW_RESOLUTION").IsVisible = () => gs.Mode == WindowMode.Windowed;
			var windowWidth = display.Get<TextFieldWidget>("WINDOW_WIDTH");
			windowWidth.Text = gs.WindowedSize.X.ToString();

			var windowHeight = display.Get<TextFieldWidget>("WINDOW_HEIGHT");
			windowHeight.Text = gs.WindowedSize.Y.ToString();

			var pixelDoubleCheckbox = display.Get<CheckboxWidget>("PIXELDOUBLE_CHECKBOX");
			pixelDoubleCheckbox.IsChecked = () => gs.PixelDouble;
			pixelDoubleCheckbox.OnClick = () =>
			{
				gs.PixelDouble ^= true;
				Game.viewport.Zoom = gs.PixelDouble ? 2 : 1;
			};

			// Debug
			var debug = bg.Get("DEBUG_PANE");

			var perfgraphCheckbox = debug.Get<CheckboxWidget>("PERFDEBUG_CHECKBOX");
			perfgraphCheckbox.IsChecked = () => Game.Settings.Debug.PerfGraph;
			perfgraphCheckbox.OnClick = () => Game.Settings.Debug.PerfGraph ^= true;

			var checkunsyncedCheckbox = debug.Get<CheckboxWidget>("CHECKUNSYNCED_CHECKBOX");
			checkunsyncedCheckbox.IsChecked = () => Game.Settings.Debug.SanityCheckUnsyncedCode;
			checkunsyncedCheckbox.OnClick = () => Game.Settings.Debug.SanityCheckUnsyncedCode ^= true;

			bg.Get<ButtonWidget>("BUTTON_CLOSE").OnClick = () =>
			{
				int x, y;
				int.TryParse(windowWidth.Text, out x);
				int.TryParse(windowHeight.Text, out y);
				gs.WindowedSize = new int2(x,y);
				Game.Settings.Save();
				Ui.CloseWindow();
			};
		}

		string open = null;

		bool FlipToTab(string id)
		{
			if (open != null)
				bg.Get(open).Visible = false;

			open = id;
			bg.Get(open).Visible = true;
			return true;
		}

		public static bool ShowWindowModeDropdown(DropDownButtonWidget dropdown, GraphicSettings s)
		{
			var options = new Dictionary<string, WindowMode>()
			{
				{ "Pseudo-Fullscreen", WindowMode.PseudoFullscreen },
				{ "Fullscreen", WindowMode.Fullscreen },
				{ "Windowed", WindowMode.Windowed },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.Mode == options[o],
					() => s.Mode = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
			return true;
		}
	}
}
