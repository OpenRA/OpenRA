#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Widgets;
using OpenRA.Mods.RA.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncSettingsLogic
	{
		enum PanelType { General, Input }

		SoundDevice soundDevice;
		PanelType settingsPanel = PanelType.General;
		ColorPreviewManagerWidget colorPreview;
		World world;

		[ObjectCreator.UseCtor]
		public CncSettingsLogic(Widget widget, World world, Action onExit)
		{
			this.world = world;
			var panel = widget.Get("SETTINGS_PANEL");

			// General pane
			var generalButton = panel.Get<ButtonWidget>("GENERAL_BUTTON");
			generalButton.OnClick = () => settingsPanel = PanelType.General;
			generalButton.IsHighlighted = () => settingsPanel == PanelType.General;

			var generalPane = panel.Get("GENERAL_CONTROLS");
			generalPane.IsVisible = () => settingsPanel == PanelType.General;

			var gameSettings = Game.Settings.Game;
			var playerSettings = Game.Settings.Player;
			var debugSettings = Game.Settings.Debug;
			var graphicsSettings = Game.Settings.Graphics;
			var soundSettings = Game.Settings.Sound;

			// Player profile
			var nameTextfield = generalPane.Get<TextFieldWidget>("NAME_TEXTFIELD");
			nameTextfield.Text = playerSettings.Name;

			colorPreview = panel.Get<ColorPreviewManagerWidget>("COLOR_MANAGER");
			colorPreview.Color = playerSettings.Color;

			var colorDropdown = generalPane.Get<DropDownButtonWidget>("COLOR");
			colorDropdown.OnMouseDown = _ => ShowColorPicker(colorDropdown, playerSettings);
			colorDropdown.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => playerSettings.Color.RGB;

			// Debug
			var perftextCheckbox = generalPane.Get<CheckboxWidget>("PERFTEXT_CHECKBOX");
			perftextCheckbox.IsChecked = () => debugSettings.PerfText;
			perftextCheckbox.OnClick = () => debugSettings.PerfText ^= true;

			var perfgraphCheckbox = generalPane.Get<CheckboxWidget>("PERFGRAPH_CHECKBOX");
			perfgraphCheckbox.IsChecked = () => debugSettings.PerfGraph;
			perfgraphCheckbox.OnClick = () => debugSettings.PerfGraph ^= true;

			var checkunsyncedCheckbox = generalPane.Get<CheckboxWidget>("CHECKUNSYNCED_CHECKBOX");
			checkunsyncedCheckbox.IsChecked = () => debugSettings.SanityCheckUnsyncedCode;
			checkunsyncedCheckbox.OnClick = () => debugSettings.SanityCheckUnsyncedCode ^= true;

			var showFatalErrorDialog = generalPane.Get<CheckboxWidget>("SHOW_FATAL_ERROR_DIALOG_CHECKBOX");
			showFatalErrorDialog.IsChecked = () => Game.Settings.Debug.ShowFatalErrorDialog;
			showFatalErrorDialog.OnClick = () => Game.Settings.Debug.ShowFatalErrorDialog ^= true;

			// Video
			var windowModeDropdown = generalPane.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => SettingsMenuLogic.ShowWindowModeDropdown(windowModeDropdown, graphicsSettings);
			windowModeDropdown.GetText = () => graphicsSettings.Mode == WindowMode.Windowed ?
				"Windowed" : graphicsSettings.Mode == WindowMode.Fullscreen ? "Fullscreen" : "Pseudo-Fullscreen";

			var pixelDoubleCheckbox = generalPane.Get<CheckboxWidget>("PIXELDOUBLE_CHECKBOX");
			pixelDoubleCheckbox.IsChecked = () => graphicsSettings.PixelDouble;
			pixelDoubleCheckbox.OnClick = () =>
			{
				graphicsSettings.PixelDouble ^= true;
				Game.Zoom = graphicsSettings.PixelDouble ? 2 : 1;
			};

			var showShellmapCheckbox = generalPane.Get<CheckboxWidget>("SHOW_SHELLMAP");
			showShellmapCheckbox.IsChecked = () => gameSettings.ShowShellmap;
			showShellmapCheckbox.OnClick = () => gameSettings.ShowShellmap ^= true;

			generalPane.Get("WINDOW_RESOLUTION").IsVisible = () => graphicsSettings.Mode == WindowMode.Windowed;
			var windowWidth = generalPane.Get<TextFieldWidget>("WINDOW_WIDTH");
			windowWidth.Text = graphicsSettings.WindowedSize.X.ToString();

			var windowHeight = generalPane.Get<TextFieldWidget>("WINDOW_HEIGHT");
			windowHeight.Text = graphicsSettings.WindowedSize.Y.ToString();

			// Audio
			var soundSlider = generalPane.Get<SliderWidget>("SOUND_SLIDER");
			soundSlider.OnChange += x => { soundSettings.SoundVolume = x; Sound.SoundVolume = x; };
			soundSlider.Value = soundSettings.SoundVolume;

			var musicSlider = generalPane.Get<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => { soundSettings.MusicVolume = x; Sound.MusicVolume = x; };
			musicSlider.Value = soundSettings.MusicVolume;

			var shellmapMusicCheckbox = generalPane.Get<CheckboxWidget>("SHELLMAP_MUSIC");
			shellmapMusicCheckbox.IsChecked = () => soundSettings.MapMusic;
			shellmapMusicCheckbox.OnClick = () => soundSettings.MapMusic ^= true;

			var devices = Sound.AvailableDevices();
			soundDevice = devices.FirstOrDefault(d => d.Engine == soundSettings.Engine && d.Device == soundSettings.Device) ?? devices.First();

			var audioDeviceDropdown = generalPane.Get<DropDownButtonWidget>("AUDIO_DEVICE");
			audioDeviceDropdown.OnMouseDown = _ => ShowAudioDeviceDropdown(audioDeviceDropdown, soundSettings, devices);
			audioDeviceDropdown.GetText = () => soundDevice.Label;

			// Input pane
			var inputPane = panel.Get("INPUT_CONTROLS");
			inputPane.IsVisible = () => settingsPanel == PanelType.Input;

			var inputButton = panel.Get<ButtonWidget>("INPUT_BUTTON");
			inputButton.OnClick = () => settingsPanel = PanelType.Input;
			inputButton.IsHighlighted = () => settingsPanel == PanelType.Input;

			var classicMouseCheckbox = inputPane.Get<CheckboxWidget>("CLASSICORDERS_CHECKBOX");
			classicMouseCheckbox.IsChecked = () => gameSettings.UseClassicMouseStyle;
			classicMouseCheckbox.OnClick = () => gameSettings.UseClassicMouseStyle ^= true;

			var scrollSlider = inputPane.Get<SliderWidget>("SCROLLSPEED_SLIDER");
			scrollSlider.Value = gameSettings.ViewportEdgeScrollStep;
			scrollSlider.OnChange += x => gameSettings.ViewportEdgeScrollStep = x;

			var edgescrollCheckbox = inputPane.Get<CheckboxWidget>("EDGESCROLL_CHECKBOX");
			edgescrollCheckbox.IsChecked = () => gameSettings.ViewportEdgeScroll;
			edgescrollCheckbox.OnClick = () => gameSettings.ViewportEdgeScroll ^= true;

			var mouseScrollDropdown = inputPane.Get<DropDownButtonWidget>("MOUSE_SCROLL");
			mouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(mouseScrollDropdown, gameSettings);
			mouseScrollDropdown.GetText = () => gameSettings.MouseScroll.ToString();

			var teamchatCheckbox = inputPane.Get<CheckboxWidget>("TEAMCHAT_CHECKBOX");
			teamchatCheckbox.IsChecked = () => gameSettings.TeamChatToggle;
			teamchatCheckbox.OnClick = () => gameSettings.TeamChatToggle ^= true;

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				playerSettings.Name = nameTextfield.Text;
				int x, y;
				int.TryParse(windowWidth.Text, out x);
				int.TryParse(windowHeight.Text, out y);
				graphicsSettings.WindowedSize = new int2(x, y);
				soundSettings.Device = soundDevice.Device;
				soundSettings.Engine = soundDevice.Engine;
				Game.Settings.Save();
				Ui.CloseWindow();
				onExit();
			};
		}

		static bool ShowMouseScrollDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, MouseScrollType>()
			{
				{ "Disabled", MouseScrollType.Disabled },
				{ "Standard", MouseScrollType.Standard },
				{ "Inverted", MouseScrollType.Inverted },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
				                                  () => s.MouseScroll == options[o],
				                                  () => s.MouseScroll = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
			return true;
		}

		bool ShowColorPicker(DropDownButtonWidget color, PlayerSettings s)
		{
			Action<HSLColor> onChange = c => colorPreview.Color = c;
			Action onExit = () =>
			{
				s.Color = colorPreview.Color;
				color.RemovePanel();
			};

			var colorChooser = Game.LoadWidget(world, "COLOR_CHOOSER", null, new WidgetArgs()
			{
				{ "onExit", onExit },
				{ "onChange", onChange },
				{ "initialColor", s.Color }
			});

			color.AttachPanel(colorChooser, onExit);
			return true;
		}

		bool ShowAudioDeviceDropdown(DropDownButtonWidget dropdown, SoundSettings s, SoundDevice[] devices)
		{
			var i = 0;
			var options = devices.ToDictionary(d => (i++).ToString(), d => d);

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => soundDevice == options[o],
					() => soundDevice = options[o]);

				item.Get<LabelWidget>("LABEL").GetText = () => options[o].Label;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
			return true;
		}
	}
}
