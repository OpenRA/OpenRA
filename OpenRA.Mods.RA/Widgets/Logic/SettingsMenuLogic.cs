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

		public SettingsMenuLogic()
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
			name.OnEnterKey = () => { name.LoseFocus(); return true; };

			var edgescrollCheckbox = general.Get<CheckboxWidget>("EDGE_SCROLL");
			edgescrollCheckbox.IsChecked = () => Game.Settings.Game.ViewportEdgeScroll;
			edgescrollCheckbox.OnClick = () => Game.Settings.Game.ViewportEdgeScroll ^= true;

			var edgeScrollSlider = general.Get<SliderWidget>("EDGE_SCROLL_AMOUNT");
			edgeScrollSlider.Value = Game.Settings.Game.ViewportEdgeScrollStep;
			edgeScrollSlider.OnChange += x => Game.Settings.Game.ViewportEdgeScrollStep = x;

			var inversescroll = general.Get<CheckboxWidget>("INVERSE_SCROLL");
			inversescroll.IsChecked = () => Game.Settings.Game.MouseScroll == MouseScrollType.Inverted;
			inversescroll.OnClick = () => Game.Settings.Game.MouseScroll = (Game.Settings.Game.MouseScroll == MouseScrollType.Inverted) ?
												MouseScrollType.Standard : MouseScrollType.Inverted;

			var teamchatCheckbox = general.Get<CheckboxWidget>("TEAMCHAT_TOGGLE");
			teamchatCheckbox.IsChecked = () => Game.Settings.Game.TeamChatToggle;
			teamchatCheckbox.OnClick = () => Game.Settings.Game.TeamChatToggle ^= true;

			var showShellmapCheckbox = general.Get<CheckboxWidget>("SHOW_SHELLMAP");
			showShellmapCheckbox.IsChecked = () => Game.Settings.Game.ShowShellmap;
			showShellmapCheckbox.OnClick = () => Game.Settings.Game.ShowShellmap ^= true;

			var useClassicMouseStyleCheckbox = general.Get<CheckboxWidget>("USE_CLASSIC_MOUSE_STYLE_CHECKBOX");
			useClassicMouseStyleCheckbox.IsChecked = () => Game.Settings.Game.UseClassicMouseStyle;
			useClassicMouseStyleCheckbox.OnClick = () => Game.Settings.Game.UseClassicMouseStyle ^= true;

			// Audio
			var audio = bg.Get("AUDIO_PANE");
			var soundSettings = Game.Settings.Sound;

			var soundslider = audio.Get<SliderWidget>("SOUND_VOLUME");
			soundslider.OnChange += x => Sound.SoundVolume = x;
			soundslider.Value = Sound.SoundVolume;

			var musicslider = audio.Get<SliderWidget>("MUSIC_VOLUME");
			musicslider.OnChange += x => Sound.MusicVolume = x;
			musicslider.Value = Sound.MusicVolume;

			var cashticksdropdown = audio.Get<DropDownButtonWidget>("CASH_TICK_TYPE");
			cashticksdropdown.OnMouseDown = _ => ShowSoundTickDropdown(cashticksdropdown, soundSettings);
			cashticksdropdown.GetText = () => soundSettings.SoundCashTickType == SoundCashTicks.Extreme ?
				"Extreme" : soundSettings.SoundCashTickType == SoundCashTicks.Normal ? "Normal" : "Disabled";

			
			// Display
			var display = bg.Get("DISPLAY_PANE");
			var gs = Game.Settings.Graphics;

			var GraphicsRendererDropdown = display.Get<DropDownButtonWidget>("GRAPHICS_RENDERER");
			GraphicsRendererDropdown.OnMouseDown = _ => ShowRendererDropdown(GraphicsRendererDropdown, gs);
			GraphicsRendererDropdown.GetText = () => gs.Renderer == "Gl" ?
				"OpenGL" : gs.Renderer == "Cg" ? "Cg Toolkit" : "OpenGL";

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

			// Keys
			var keys = bg.Get("KEYS_PANE");

			var keyConfig = Game.Settings.Keys;

			var modifierToBuildDropdown = keys.Get<DropDownButtonWidget>("MODIFIERTOBUILD_DROPDOWN");
			modifierToBuildDropdown.OnMouseDown = _
				=> ShowHotkeyModifierDropdown(modifierToBuildDropdown, keyConfig.ModifierToBuild, m => keyConfig.ModifierToBuild = m);
			modifierToBuildDropdown.GetText = ()
				=> keyConfig.ModifierToBuild == Modifiers.None ? "<Hotkey>"
					: keyConfig.ModifierToBuild == Modifiers.Alt ? "Alt + <Hotkey>"
					: "Ctrl + <Hotkey>";

			var modifierToSelectTabDropdown = keys.Get<DropDownButtonWidget>("MODIFIERTOSELECTTAB_DROPDOWN");
			modifierToSelectTabDropdown.OnMouseDown = _
				=> ShowHotkeyModifierDropdown(modifierToSelectTabDropdown, keyConfig.ModifierToSelectTab,
								m => keyConfig.ModifierToSelectTab = m);
			modifierToSelectTabDropdown.GetText = ()
				=> keyConfig.ModifierToSelectTab == Modifiers.None ? "<Hotkey>"
					: keyConfig.ModifierToSelectTab == Modifiers.Alt ? "Alt + <Hotkey>"
					: "Ctrl + <Hotkey>";

			var specialHotkeyList = keys.Get<ScrollPanelWidget>("SPECIALHOTKEY_LIST");

			var specialHotkeyTemplate = specialHotkeyList.Get<ScrollItemWidget>("SPECIALHOTKEY_TEMPLATE");

			var pauseKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			pauseKey.Get<LabelWidget>("FUNCTION").GetText = () => "Pause the game:";
			SetupKeyBinding(pauseKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.PauseKey, k => keyConfig.PauseKey = k);
			specialHotkeyList.AddChild(pauseKey);

			var viewportToBase = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			viewportToBase.Get<LabelWidget>("FUNCTION").GetText = () => "Move Viewport to Base:";
			SetupKeyBinding(viewportToBase.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.FocusBaseKey, k => keyConfig.FocusBaseKey = k);
			specialHotkeyList.AddChild(viewportToBase);

			var lastEventKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			lastEventKey.Get<LabelWidget>("FUNCTION").GetText = () => "Move Viewport to Last Event:";
			SetupKeyBinding(lastEventKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.FocusLastEventKey, k => keyConfig.FocusLastEventKey = k);
			specialHotkeyList.AddChild(lastEventKey);

			var sellKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			sellKey.Get<LabelWidget>("FUNCTION").GetText = () => "Switch to Sell-Cursor:";
			SetupKeyBinding(sellKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.SellKey, k => keyConfig.SellKey = k);
			specialHotkeyList.AddChild(sellKey);

			var powerDownKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			powerDownKey.Get<LabelWidget>("FUNCTION").GetText = () => "Switch to Power-Down-Cursor:";
			SetupKeyBinding(powerDownKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.PowerDownKey, k => keyConfig.PowerDownKey = k);
			specialHotkeyList.AddChild(powerDownKey);

			var repairKey = ScrollItemWidget.Setup(specialHotkeyTemplate, () => false, () => {});
			repairKey.Get<LabelWidget>("FUNCTION").GetText = () => "Switch to Repair-Cursor:";
			SetupKeyBinding(repairKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.RepairKey, k => keyConfig.RepairKey = k);
			specialHotkeyList.AddChild(repairKey);

			var unitCommandHotkeyList = keys.Get<ScrollPanelWidget>("UNITCOMMANDHOTKEY_LIST");

			var unitCommandHotkeyTemplate = unitCommandHotkeyList.Get<ScrollItemWidget>("UNITCOMMANDHOTKEY_TEMPLATE");

			var attackKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			attackKey.Get<LabelWidget>("FUNCTION").GetText = () => "Attack Move:";
			SetupKeyBinding(attackKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.AttackMoveKey, k => keyConfig.AttackMoveKey = k);
			unitCommandHotkeyList.AddChild(attackKey);

			var stopKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			stopKey.Get<LabelWidget>("FUNCTION").GetText = () => "Stop:";
			SetupKeyBinding(stopKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.StopKey, k => keyConfig.StopKey = k);
			unitCommandHotkeyList.AddChild(stopKey);

			var scatterKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			scatterKey.Get<LabelWidget>("FUNCTION").GetText = () => "Scatter:";
			SetupKeyBinding(scatterKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.ScatterKey, k => keyConfig.ScatterKey = k);
			unitCommandHotkeyList.AddChild(scatterKey);

			var stanceCycleKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			stanceCycleKey.Get<LabelWidget>("FUNCTION").GetText = () => "Cycle Stance:";
			SetupKeyBinding(stanceCycleKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.StanceCycleKey, k => keyConfig.StanceCycleKey = k);
			unitCommandHotkeyList.AddChild(stanceCycleKey);

			var deployKey = ScrollItemWidget.Setup(unitCommandHotkeyTemplate, () => false, () => {});
			deployKey.Get<LabelWidget>("FUNCTION").GetText = () => "Deploy:";
			SetupKeyBinding(deployKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.DeployKey, k => keyConfig.DeployKey = k);
			unitCommandHotkeyList.AddChild(deployKey);

			var productionQueueHotkeyList = keys.Get<ScrollPanelWidget>("PRODUCTIONQUEUEHOTKEY_LIST");

			var productionQueueTemplate = productionQueueHotkeyList.Get<ScrollItemWidget>("PRODUCTIONQUEUEHOTKEY_TEMPLATE");

			var cycleTabsKey = ScrollItemWidget.Setup(productionQueueTemplate, () => false, () => {});
			cycleTabsKey.Get<LabelWidget>("FUNCTION").GetText = () => "Cycle through build palette:";
			SetupKeyBinding(cycleTabsKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.CycleTabsKey, k => keyConfig.CycleTabsKey = k);
			productionQueueHotkeyList.AddChild(cycleTabsKey);

			var buildingsTabKey = ScrollItemWidget.Setup(productionQueueTemplate, () => false, () => {});
			buildingsTabKey.Get<LabelWidget>("FUNCTION").GetText = () => "Select 1st Tab:";
			SetupKeyBinding(buildingsTabKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.FirstTabKey, k => keyConfig.FirstTabKey = k);
			productionQueueHotkeyList.AddChild(buildingsTabKey);

			var defenseTabKey = ScrollItemWidget.Setup(productionQueueTemplate, () => false, () => {});
			defenseTabKey.Get<LabelWidget>("FUNCTION").GetText = () => "Select 2nd Tab:";
			SetupKeyBinding(defenseTabKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.SecondTabKey, k => keyConfig.SecondTabKey = k);
			productionQueueHotkeyList.AddChild(defenseTabKey);

			var vehicleTabKey = ScrollItemWidget.Setup(productionQueueTemplate, () => false, () => {});
			vehicleTabKey.Get<LabelWidget>("FUNCTION").GetText = () => "Select 3rd Tab:";
			SetupKeyBinding(vehicleTabKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.ThirdTabKey, k => keyConfig.ThirdTabKey = k);
			productionQueueHotkeyList.AddChild(vehicleTabKey);

			var infantryTabKey = ScrollItemWidget.Setup(productionQueueTemplate, () => false, () => {});
			infantryTabKey.Get<LabelWidget>("FUNCTION").GetText = () => "Select 4th Tab:";
			SetupKeyBinding(infantryTabKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.FourthTabKey, k => keyConfig.FourthTabKey = k );
			productionQueueHotkeyList.AddChild(infantryTabKey);

			var shipTabKey = ScrollItemWidget.Setup(productionQueueTemplate, () => false, () => {});
			shipTabKey.Get<LabelWidget>("FUNCTION").GetText = () => "Select 5th Tab:";
			SetupKeyBinding(shipTabKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.FifthTabKey, k => keyConfig.FifthTabKey = k );
			productionQueueHotkeyList.AddChild(shipTabKey);

			var planeTabKey = ScrollItemWidget.Setup(productionQueueTemplate, () => false, () => {});
			planeTabKey.Get<LabelWidget>("FUNCTION").GetText = () => "Select 6th Tab:";
			SetupKeyBinding(planeTabKey.Get<TextFieldWidget>("HOTKEY"),
			() => keyConfig.SixthTabKey, k => keyConfig.SixthTabKey = k );
			productionQueueHotkeyList.AddChild(planeTabKey);

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

		public static bool ShowHotkeyModifierDropdown(DropDownButtonWidget dropdown, Modifiers m, Action<Modifiers> am)
		{
			var options = new Dictionary<string, Modifiers>()
			{
				{ "<Hotkey>", Modifiers.None },
				{ "Alt + <Hotkey>", Modifiers.Alt  },
				{ "Ctrl + <Hotkey>", Modifiers.Ctrl },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => m == options[o],
					() => am(options[o]));
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys.ToList(), setupItem);
			return true;
		}

		void SetupKeyBinding(TextFieldWidget textBox, Func<string> getValue, Action<string> setValue)
		{
			textBox.Text = getValue();

			textBox.OnLoseFocus = () =>
			{
				textBox.Text.Trim();
				if (textBox.Text.Length == 0)
				textBox.Text = getValue();
				else
				setValue(textBox.Text);
			};

			textBox.OnEnterKey = () => { textBox.LoseFocus(); return true; };
		}

		public static bool ShowRendererDropdown(DropDownButtonWidget dropdown, GraphicSettings s)
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
	}
}
