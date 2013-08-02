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
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class SettingsMenuLogic
	{
		Widget bg;
		SoundDevice soundDevice;

		[ObjectCreator.UseCtor]
		public SettingsMenuLogic(Action onExit)
		{
			bg = Ui.Root.Get<BackgroundWidget>("SETTINGS_MENU");
			var tabs = bg.Get<ContainerWidget>("TAB_CONTAINER");

			//Tabs
			tabs.Get<ButtonWidget>("GENERAL").OnClick = () => FlipToTab("GENERAL_PANE");
			tabs.Get<ButtonWidget>("AUDIO").OnClick = () => FlipToTab("AUDIO_PANE");
			tabs.Get<ButtonWidget>("DISPLAY").OnClick = () => FlipToTab("DISPLAY_PANE");
			tabs.Get<ButtonWidget>("KEYS").OnClick = () => FlipToTab("KEYS_PANE");
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
			name.OnEnterKey = () => { name.YieldKeyboardFocus(); return true; };

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

			var useClassicMouseStyleCheckbox = general.Get<CheckboxWidget>("USE_CLASSIC_MOUSE_STYLE_CHECKBOX");
			useClassicMouseStyleCheckbox.IsChecked = () => Game.Settings.Game.UseClassicMouseStyle;
			useClassicMouseStyleCheckbox.OnClick = () => Game.Settings.Game.UseClassicMouseStyle ^= true;

			var allowNatDiscoveryCheckbox = general.Get<CheckboxWidget>("ALLOW_NAT_DISCOVERY_CHECKBOX");
			allowNatDiscoveryCheckbox.IsChecked = () => Game.Settings.Server.DiscoverNatDevices;
			allowNatDiscoveryCheckbox.OnClick = () => Game.Settings.Server.DiscoverNatDevices ^= true;

			// Audio
			var audio = bg.Get("AUDIO_PANE");
			var soundSettings = Game.Settings.Sound;

			var soundslider = audio.Get<SliderWidget>("SOUND_VOLUME");
			soundslider.OnChange += x => Sound.SoundVolume = x;
			soundslider.Value = Sound.SoundVolume;

			var musicslider = audio.Get<SliderWidget>("MUSIC_VOLUME");
			musicslider.OnChange += x => Sound.MusicVolume = x;
			musicslider.Value = Sound.MusicVolume;

			var videoslider = audio.Get<SliderWidget>("VIDEO_VOLUME");
			videoslider.OnChange += x => Sound.VideoVolume = x;
			videoslider.Value = Sound.VideoVolume;

			var cashticksdropdown = audio.Get<DropDownButtonWidget>("CASH_TICK_TYPE");
			cashticksdropdown.OnMouseDown = _ => ShowSoundTickDropdown(cashticksdropdown, soundSettings);
			cashticksdropdown.GetText = () => soundSettings.SoundCashTickType == SoundCashTicks.Extreme ?
				"Extreme" : soundSettings.SoundCashTickType == SoundCashTicks.Normal ? "Normal" : "Disabled";

			var mapMusicCheckbox = audio.Get<CheckboxWidget>("MAP_MUSIC_CHECKBOX");
			mapMusicCheckbox.IsChecked = () => Game.Settings.Sound.MapMusic;
			mapMusicCheckbox.OnClick = () => Game.Settings.Sound.MapMusic ^= true;

			var devices = Sound.AvailableDevices();
			soundDevice = devices.FirstOrDefault(d => d.Engine == soundSettings.Engine && d.Device == soundSettings.Device) ?? devices.First();

			var audioDeviceDropdown = audio.Get<DropDownButtonWidget>("AUDIO_DEVICE");
			audioDeviceDropdown.OnMouseDown = _ => ShowAudioDeviceDropdown(audioDeviceDropdown, soundSettings, devices);
			audioDeviceDropdown.GetText = () => soundDevice.Label;

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

			var capFrameRateCheckbox = display.Get<CheckboxWidget>("CAPFRAMERATE_CHECKBOX");
			capFrameRateCheckbox.IsChecked = () => gs.CapFramerate;
			capFrameRateCheckbox.OnClick = () => gs.CapFramerate ^= true;

			var maxFrameRate = display.Get<TextFieldWidget>("MAX_FRAMERATE");
			maxFrameRate.Text = gs.MaxFramerate.ToString();

			// Keys
			var keys = bg.Get("KEYS_PANE");
			var keyConfig = Game.Settings.Keys;

			var specialHotkeyList = keys.Get<ScrollPanelWidget>("SPECIALHOTKEY_LIST");
			var specialHotkeyTemplate = specialHotkeyList.Get<ScrollItemWidget>("SPECIALHOTKEY_TEMPLATE");

			var pauseKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(pauseKey, "Pause the game:", () => keyConfig.PauseKey, k => keyConfig.PauseKey = k);
			specialHotkeyList.AddChild(pauseKey);

			var viewportToBase = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(viewportToBase, "Move Viewport to Base:", () => keyConfig.CycleBaseKey, k => keyConfig.CycleBaseKey = k);
			specialHotkeyList.AddChild(viewportToBase);

			var lastEventKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(lastEventKey, "Move Viewport to Last Event:", () => keyConfig.ToLastEventKey, k => keyConfig.ToLastEventKey = k);
			specialHotkeyList.AddChild(lastEventKey);

			var viewportToSelectionKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(viewportToSelectionKey, "Move Viewport to Selection:", () => keyConfig.ToSelectionKey, k => keyConfig.ToSelectionKey = k);
			specialHotkeyList.AddChild(viewportToSelectionKey);
			
			var sellKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(sellKey, "Switch to Sell-Cursor:", () => keyConfig.SellKey, k => keyConfig.SellKey = k);
			specialHotkeyList.AddChild(sellKey);

			var powerDownKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(powerDownKey, "Switch to Power-Down-Cursor:", () => keyConfig.PowerDownKey, k => keyConfig.PowerDownKey = k);
			specialHotkeyList.AddChild(powerDownKey);

			var repairKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(repairKey, "Switch to Repair-Cursor:", () => keyConfig.RepairKey, k => keyConfig.RepairKey = k);
			specialHotkeyList.AddChild(repairKey);

			var tabCycleKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(tabCycleKey, "Cycle Tabs (+Shift to Reverse):", () => keyConfig.CycleTabsKey, k => keyConfig.CycleTabsKey = k);
			specialHotkeyList.AddChild(tabCycleKey);

			var unitCommandHotkeyList = keys.Get<ScrollPanelWidget>("UNITCOMMANDHOTKEY_LIST");
			var unitCommandHotkeyTemplate = unitCommandHotkeyList.Get<ScrollItemWidget>("UNITCOMMANDHOTKEY_TEMPLATE");

			var attackKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(attackKey, "Attack Move:", () => keyConfig.AttackMoveKey, k => keyConfig.AttackMoveKey = k);
			unitCommandHotkeyList.AddChild(attackKey);

			var stopKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(stopKey, "Stop:", () => keyConfig.StopKey, k => keyConfig.StopKey = k);
			unitCommandHotkeyList.AddChild(stopKey);

			var scatterKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(scatterKey, "Scatter:", () => keyConfig.ScatterKey, k => keyConfig.ScatterKey = k);
			unitCommandHotkeyList.AddChild(scatterKey);

			var stanceCycleKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(stanceCycleKey, "Cycle Stance:", () => keyConfig.StanceCycleKey, k => keyConfig.StanceCycleKey = k);
			unitCommandHotkeyList.AddChild(stanceCycleKey);

			var deployKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			SetupKeyBinding(deployKey, "Deploy:", () => keyConfig.DeployKey, k => keyConfig.DeployKey = k);
			unitCommandHotkeyList.AddChild(deployKey);

			var guardKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => { });
			SetupKeyBinding(guardKey, "Guard: ", () => keyConfig.GuardKey, k => keyConfig.GuardKey = k);
			unitCommandHotkeyList.AddChild(guardKey);

			// Debug
			var debug = bg.Get("DEBUG_PANE");

			var perfgraphCheckbox = debug.Get<CheckboxWidget>("PERFGRAPH_CHECKBOX");
			perfgraphCheckbox.IsChecked = () => Game.Settings.Debug.PerfGraph;
			perfgraphCheckbox.OnClick = () => Game.Settings.Debug.PerfGraph ^= true;

			var perftextCheckbox = debug.Get<CheckboxWidget>("PERFTEXT_CHECKBOX");
			perftextCheckbox.IsChecked = () => Game.Settings.Debug.PerfText;
			perftextCheckbox.OnClick = () => Game.Settings.Debug.PerfText ^= true;

			var sampleSlider = debug.Get<SliderWidget>("PERFTEXT_SAMPLE_AMOUNT");
			sampleSlider.Value = sampleSlider.MaximumValue-Game.Settings.Debug.Samples;
			sampleSlider.OnChange += x => Game.Settings.Debug.Samples = (int)sampleSlider.MaximumValue-(int)Math.Round(x);

			var checkunsyncedCheckbox = debug.Get<CheckboxWidget>("CHECKUNSYNCED_CHECKBOX");
			checkunsyncedCheckbox.IsChecked = () => Game.Settings.Debug.SanityCheckUnsyncedCode;
			checkunsyncedCheckbox.OnClick = () => Game.Settings.Debug.SanityCheckUnsyncedCode ^= true;

			var botdebugCheckbox = debug.Get<CheckboxWidget>("BOTDEBUG_CHECKBOX");
			botdebugCheckbox.IsChecked = () => Game.Settings.Debug.BotDebug;
			botdebugCheckbox.OnClick = () => Game.Settings.Debug.BotDebug ^= true;

			var verboseNatDiscoveryCheckbox = debug.Get<CheckboxWidget>("VERBOSE_NAT_DISCOVERY_CHECKBOX");
			verboseNatDiscoveryCheckbox.IsChecked = () => Game.Settings.Server.VerboseNatDiscovery;
			verboseNatDiscoveryCheckbox.OnClick = () => Game.Settings.Server.VerboseNatDiscovery ^= true;

			var developerMenuCheckbox = debug.Get<CheckboxWidget>("DEVELOPER_MENU_CHECKBOX");
			developerMenuCheckbox.IsChecked = () => Game.Settings.Debug.DeveloperMenu;
			developerMenuCheckbox.OnClick = () => Game.Settings.Debug.DeveloperMenu ^= true;

			bg.Get<ButtonWidget>("BUTTON_CLOSE").OnClick = () =>
			{
				int x, y;
				int.TryParse(windowWidth.Text, out x);
				int.TryParse(windowHeight.Text, out y);
				gs.WindowedSize = new int2(x,y);
				int.TryParse(maxFrameRate.Text, out gs.MaxFramerate);
				soundSettings.Device = soundDevice.Device;
				soundSettings.Engine = soundDevice.Engine;
				Game.Settings.Save();
				Ui.CloseWindow();
				onExit();
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

		
		public static bool ShowSoundTickDropdown(DropDownButtonWidget dropdown, SoundSettings audio)
		{
			var options = new Dictionary<string, SoundCashTicks>()
			{
				{ "Extreme", SoundCashTicks.Extreme },
				{ "Normal", SoundCashTicks.Normal },
				{ "Disabled", SoundCashTicks.Disabled },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => audio.SoundCashTickType == options[o],
					() => audio.SoundCashTickType = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
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

		void SetupKeyBinding(ScrollItemWidget keyWidget, string description, Func<string> getValue, Action<string> setValue)
		{
			keyWidget.Get<LabelWidget>("FUNCTION").GetText = () => description;

			var textBox = keyWidget.Get<TextFieldWidget>("HOTKEY");

			textBox.Text = getValue();
			textBox.OnLoseFocus = () =>
			{
				textBox.Text.Trim();
				if (textBox.Text.Length == 0)
					textBox.Text = getValue();
				else
					setValue(textBox.Text);
			};
			textBox.OnEnterKey = () => { textBox.YieldKeyboardFocus(); return true; };
		}

		static bool ShowRendererDropdown(DropDownButtonWidget dropdown, GraphicSettings s)
		{
			var options = new Dictionary<string, string>()
			{
				{ "OpenGL", "Gl" },
				{ "Cg Toolkit", "Cg" },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.Renderer == options[o],
					() => s.Renderer = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
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
