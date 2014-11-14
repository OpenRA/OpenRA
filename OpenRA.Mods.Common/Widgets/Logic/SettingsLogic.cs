#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SettingsLogic
	{
		enum PanelType { Display, Audio, Input, Advanced }
		Dictionary<PanelType, Action> leavePanelActions = new Dictionary<PanelType, Action>();
		Dictionary<PanelType, Action> resetPanelActions = new Dictionary<PanelType, Action>();
		PanelType settingsPanel = PanelType.Display;
		Widget panelContainer, tabContainer;

		WorldRenderer worldRenderer;
		SoundDevice soundDevice;

		static readonly string originalSoundDevice;
		static readonly string originalSoundEngine;
		static readonly WindowMode originalGraphicsMode;
		static readonly string originalGraphicsRenderer;
		static readonly int2 originalGraphicsWindowedSize;
		static readonly int2 originalGraphicsFullscreenSize;

		static SettingsLogic()
		{
			var original = Game.Settings;
			originalSoundDevice = original.Sound.Device;
			originalSoundEngine = original.Sound.Engine;
			originalGraphicsMode = original.Graphics.Mode;
			originalGraphicsRenderer = original.Graphics.Renderer;
			originalGraphicsWindowedSize = original.Graphics.WindowedSize;
			originalGraphicsFullscreenSize = original.Graphics.FullscreenSize;
		}

		[ObjectCreator.UseCtor]
		public SettingsLogic(Widget widget, Action onExit, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;

			panelContainer = widget.Get("SETTINGS_PANEL");
			tabContainer = widget.Get("TAB_CONTAINER");

			RegisterSettingsPanel(PanelType.Display, InitDisplayPanel, ResetDisplayPanel, "DISPLAY_PANEL", "DISPLAY_TAB");
			RegisterSettingsPanel(PanelType.Audio, InitAudioPanel, ResetAudioPanel, "AUDIO_PANEL", "AUDIO_TAB");
			RegisterSettingsPanel(PanelType.Input, InitInputPanel, ResetInputPanel, "INPUT_PANEL", "INPUT_TAB");
			RegisterSettingsPanel(PanelType.Advanced, InitAdvancedPanel, ResetAdvancedPanel, "ADVANCED_PANEL", "ADVANCED_TAB");

			panelContainer.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				leavePanelActions[settingsPanel]();
				var current = Game.Settings;
				current.Save();

				Action closeAndExit = () => { Ui.CloseWindow(); onExit(); };
				if (originalSoundDevice != current.Sound.Device ||
					originalSoundEngine != current.Sound.Engine ||
					originalGraphicsMode != current.Graphics.Mode ||
					originalGraphicsRenderer != current.Graphics.Renderer ||
					originalGraphicsWindowedSize != current.Graphics.WindowedSize ||
					originalGraphicsFullscreenSize != current.Graphics.FullscreenSize)
					ConfirmationDialogs.PromptConfirmAction(
						"Restart Now?",
						"Some changes will not be applied until\nthe game is restarted. Restart now?",
						Game.Restart,
						closeAndExit,
						"Restart Now",
						"Restart Later");
				else
					closeAndExit();
			};

			panelContainer.Get<ButtonWidget>("RESET_BUTTON").OnClick = () =>
			{
				resetPanelActions[settingsPanel]();
				Game.Settings.Save();
			};
		}

		static void BindCheckboxPref(Widget parent, string id, object group, string pref)
		{
			var field = group.GetType().GetField(pref);
			if (field == null)
				throw new InvalidOperationException("{0} does not contain a preference type {1}".F(group.GetType().Name, pref));

			var cb = parent.Get<CheckboxWidget>(id);
			cb.IsChecked = () => (bool)field.GetValue(group);
			cb.OnClick = () => field.SetValue(group, cb.IsChecked() ^ true);
		}

		static void BindSliderPref(Widget parent, string id, object group, string pref)
		{
			var field = group.GetType().GetField(pref);
			if (field == null)
				throw new InvalidOperationException("{0} does not contain a preference type {1}".F(group.GetType().Name, pref));

			var ss = parent.Get<SliderWidget>(id);
			ss.Value = (float)field.GetValue(group);
			ss.OnChange += x => field.SetValue(group, x);
		}

		static void BindHotkeyPref(KeyValuePair<string, string> kv, KeySettings ks, Widget template, Widget parent)
		{
			var key = template.Clone() as Widget;
			key.Id = kv.Key;
			key.IsVisible = () => true;

			var field = ks.GetType().GetField(kv.Key);
			if (field == null)
				throw new InvalidOperationException("Game.Settings.Keys does not contain {1}".F(kv.Key));

			key.Get<LabelWidget>("FUNCTION").GetText = () => kv.Value + ":";

			var textBox = key.Get<HotkeyEntryWidget>("HOTKEY");
			textBox.Key = (Hotkey)field.GetValue(ks);
			textBox.OnLoseFocus = () =>	field.SetValue(ks, textBox.Key);
			parent.AddChild(key);
		}

		void RegisterSettingsPanel(PanelType type, Func<Widget, Action> init, Func<Widget, Action> reset, string panelID, string buttonID)
		{
			var panel = panelContainer.Get(panelID);
			var tab = tabContainer.Get<ButtonWidget>(buttonID);

			panel.IsVisible = () => settingsPanel == type;
			tab.IsHighlighted = () => settingsPanel == type;
			tab.OnClick = () => { leavePanelActions[settingsPanel](); Game.Settings.Save(); settingsPanel = type; };

			leavePanelActions.Add(type, init(panel));
			resetPanelActions.Add(type, reset(panel));
		}

		Action InitDisplayPanel(Widget panel)
		{
			var ds = Game.Settings.Graphics;
			var gs = Game.Settings.Game;

			BindCheckboxPref(panel, "PIXELDOUBLE_CHECKBOX", ds, "PixelDouble");
			BindCheckboxPref(panel, "CURSORDOUBLE_CHECKBOX", ds, "CursorDouble");
			BindCheckboxPref(panel, "FRAME_LIMIT_CHECKBOX", ds, "CapFramerate");
			BindCheckboxPref(panel, "SHOW_SHELLMAP", gs, "ShowShellmap");
			BindCheckboxPref(panel, "ALWAYS_SHOW_STATUS_BARS_CHECKBOX", gs, "AlwaysShowStatusBars");
			BindCheckboxPref(panel, "TEAM_HEALTH_COLORS_CHECKBOX", gs, "TeamHealthColors");

			var languageDropDownButton = panel.Get<DropDownButtonWidget>("LANGUAGE_DROPDOWNBUTTON");
			languageDropDownButton.OnMouseDown = _ => ShowLanguageDropdown(languageDropDownButton);
			languageDropDownButton.GetText = () => FieldLoader.Translate(ds.Language);

			var windowModeDropdown = panel.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => ShowWindowModeDropdown(windowModeDropdown, ds);
			windowModeDropdown.GetText = () => ds.Mode == WindowMode.Windowed ?
				"Windowed" : ds.Mode == WindowMode.Fullscreen ? "Fullscreen" : "Pseudo-Fullscreen";

			// Update zoom immediately
			var pixelDoubleCheckbox = panel.Get<CheckboxWidget>("PIXELDOUBLE_CHECKBOX");
			var pixelDoubleOnClick = pixelDoubleCheckbox.OnClick;
			pixelDoubleCheckbox.OnClick = () =>
			{
				pixelDoubleOnClick();
				worldRenderer.Viewport.Zoom = ds.PixelDouble ? 2 : 1;
			};

			var cursorDoubleCheckbox = panel.Get<CheckboxWidget>("CURSORDOUBLE_CHECKBOX");
			cursorDoubleCheckbox.IsDisabled = () => !ds.PixelDouble;

			panel.Get("WINDOW_RESOLUTION").IsVisible = () => ds.Mode == WindowMode.Windowed;
			var windowWidth = panel.Get<TextFieldWidget>("WINDOW_WIDTH");
			windowWidth.Text = ds.WindowedSize.X.ToString();

			var windowHeight = panel.Get<TextFieldWidget>("WINDOW_HEIGHT");
			windowHeight.Text = ds.WindowedSize.Y.ToString();

			var frameLimitTextfield = panel.Get<TextFieldWidget>("FRAME_LIMIT_TEXTFIELD");
			frameLimitTextfield.Text = ds.MaxFramerate.ToString();
			frameLimitTextfield.OnLoseFocus = () =>
			{
				int fps;
				Exts.TryParseIntegerInvariant(frameLimitTextfield.Text, out fps);
				ds.MaxFramerate = fps.Clamp(1, 1000);
				frameLimitTextfield.Text = ds.MaxFramerate.ToString();
			};
			frameLimitTextfield.OnEnterKey = () => { frameLimitTextfield.YieldKeyboardFocus(); return true; };
			frameLimitTextfield.IsDisabled = () => !ds.CapFramerate;

			// Player profile
			var ps = Game.Settings.Player;

			var nameTextfield = panel.Get<TextFieldWidget>("PLAYERNAME");
			nameTextfield.Text = ps.Name;
			nameTextfield.OnEnterKey = () => { nameTextfield.YieldKeyboardFocus(); return true; };
			nameTextfield.OnLoseFocus = () => { ps.Name = nameTextfield.Text; };

			var colorPreview = panel.Get<ColorPreviewManagerWidget>("COLOR_MANAGER");
			colorPreview.Color = ps.Color;

			var colorDropdown = panel.Get<DropDownButtonWidget>("PLAYERCOLOR");
			colorDropdown.OnMouseDown = _ => ColorPickerLogic.ShowColorDropDown(colorDropdown, colorPreview, worldRenderer.world);
			colorDropdown.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => ps.Color.RGB;

			return () =>
			{
				int x, y;
				Exts.TryParseIntegerInvariant(windowWidth.Text, out x);
				Exts.TryParseIntegerInvariant(windowHeight.Text, out y);
				ds.WindowedSize = new int2(x, y);
				frameLimitTextfield.YieldKeyboardFocus();
				nameTextfield.YieldKeyboardFocus();
			};
		}

		Action ResetDisplayPanel(Widget panel)
		{
			var ds = Game.Settings.Graphics;
			var gs = Game.Settings.Game;
			var ps = Game.Settings.Player;
			var dds = new GraphicSettings();
			var dgs = new GameSettings();
			var dps = new PlayerSettings();
			return () =>
			{
				gs.ShowShellmap = dgs.ShowShellmap;

				ds.CapFramerate = dds.CapFramerate;
				ds.MaxFramerate = dds.MaxFramerate;
				ds.Language = dds.Language;
				ds.Mode = dds.Mode;
				ds.WindowedSize = dds.WindowedSize;

				ds.PixelDouble = dds.PixelDouble;
				ds.CursorDouble = dds.CursorDouble;
				worldRenderer.Viewport.Zoom = ds.PixelDouble ? 2 : 1;

				ps.Color = dps.Color;
				ps.Name = dps.Name;
			};
		}

		Action InitAudioPanel(Widget panel)
		{
			var ss = Game.Settings.Sound;

			BindCheckboxPref(panel, "SHELLMAP_MUSIC", ss, "MapMusic");
			BindCheckboxPref(panel, "CASH_TICKS", ss, "CashTicks");

			BindSliderPref(panel, "SOUND_VOLUME", ss, "SoundVolume");
			BindSliderPref(panel, "MUSIC_VOLUME", ss, "MusicVolume");
			BindSliderPref(panel, "VIDEO_VOLUME", ss, "VideoVolume");

			// Update volume immediately
			panel.Get<SliderWidget>("SOUND_VOLUME").OnChange += x => Sound.SoundVolume = x;
			panel.Get<SliderWidget>("MUSIC_VOLUME").OnChange += x => Sound.MusicVolume = x;
			panel.Get<SliderWidget>("VIDEO_VOLUME").OnChange += x => Sound.VideoVolume = x;

			var devices = Sound.AvailableDevices();
			soundDevice = devices.FirstOrDefault(d => d.Engine == ss.Engine && d.Device == ss.Device) ?? devices.First();

			var audioDeviceDropdown = panel.Get<DropDownButtonWidget>("AUDIO_DEVICE");
			audioDeviceDropdown.OnMouseDown = _ => ShowAudioDeviceDropdown(audioDeviceDropdown, devices);
			audioDeviceDropdown.GetText = () => soundDevice.Label;

			return () =>
			{
				ss.Device = soundDevice.Device;
				ss.Engine = soundDevice.Engine;
			};
		}

		Action ResetAudioPanel(Widget panel)
		{
			var ss = Game.Settings.Sound;
			var dss = new SoundSettings();
			return () =>
			{
				ss.MapMusic = dss.MapMusic;
				ss.SoundVolume = dss.SoundVolume;
				ss.MusicVolume = dss.MusicVolume;
				ss.VideoVolume = dss.VideoVolume;
				ss.CashTicks = dss.CashTicks;
				ss.Device = dss.Device;
				ss.Engine = dss.Engine;

				panel.Get<SliderWidget>("SOUND_VOLUME").Value = ss.SoundVolume;
				panel.Get<SliderWidget>("MUSIC_VOLUME").Value = ss.MusicVolume;
				panel.Get<SliderWidget>("VIDEO_VOLUME").Value = ss.VideoVolume;
				soundDevice = Sound.AvailableDevices().First();
			};
		}

		Action InitInputPanel(Widget panel)
		{
			var gs = Game.Settings.Game;
			var ks = Game.Settings.Keys;

			BindCheckboxPref(panel, "EDGESCROLL_CHECKBOX", gs, "ViewportEdgeScroll");
			BindCheckboxPref(panel, "LOCKMOUSE_CHECKBOX", gs, "LockMouseWindow");
			BindSliderPref(panel, "SCROLLSPEED_SLIDER", gs, "ViewportEdgeScrollStep");
			BindSliderPref(panel, "UI_SCROLLSPEED_SLIDER", gs, "UIScrollSpeed");

			// Apply mouse focus preferences immediately
			var lockMouseCheckbox = panel.Get<CheckboxWidget>("LOCKMOUSE_CHECKBOX");
			var oldOnClick = lockMouseCheckbox.OnClick;
			lockMouseCheckbox.OnClick = () =>
			{
				// Still perform the old behaviour for clicking the checkbox, before
				// applying the changes live.
				oldOnClick();

				MakeMouseFocusSettingsLive();
			};

			var mouseScrollDropdown = panel.Get<DropDownButtonWidget>("MOUSE_SCROLL");
			mouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(mouseScrollDropdown, gs);
			mouseScrollDropdown.GetText = () => gs.MouseScroll.ToString();

			var hotkeyList = panel.Get<ScrollPanelWidget>("HOTKEY_LIST");
			hotkeyList.Layout = new GridLayout(hotkeyList);
			var hotkeyHeader = hotkeyList.Get<ScrollItemWidget>("HEADER");
			var globalTemplate = hotkeyList.Get("GLOBAL_TEMPLATE");
			var unitTemplate = hotkeyList.Get("UNIT_TEMPLATE");
			var productionTemplate = hotkeyList.Get("PRODUCTION_TEMPLATE");
			hotkeyList.RemoveChildren();

			// Game
			{
				var hotkeys = new Dictionary<string, string>()
				{
					{ "CycleBaseKey", "Jump to base" },
					{ "ToLastEventKey", "Jump to last radar event" },
					{ "ToSelectionKey", "Jump to selection" },
					{ "SelectAllUnitsKey", "Select all units on screen" },
					{ "SelectUnitsByTypeKey", "Select units by type" },

					{ "PlaceBeaconKey", "Place beacon" },

					{ "PauseKey", "Pause / Unpause" },
					{ "SellKey", "Sell mode" },
					{ "PowerDownKey", "Power-down mode" },
					{ "RepairKey", "Repair mode" },

					{ "NextProductionTabKey", "Next production tab" },
					{ "PreviousProductionTabKey", "Previous production tab" },
					{ "CycleProductionBuildingsKey", "Cycle production facilities" },

					{ "ToggleStatusBarsKey", "Toggle status bars" },
					{ "TogglePixelDoubleKey", "Toggle pixel doubling" },
				};

				var header = ScrollItemWidget.Setup(hotkeyHeader, () => true, () => {});
				header.Get<LabelWidget>("LABEL").GetText = () => "Game Commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, globalTemplate, hotkeyList);
			}

			// Observer
			{
				var hotkeys = new Dictionary<string, string>()
				{
					{ "ObserverCombinedView", "All Players" },
					{ "ObserverWorldView", "Disable Shroud" }
				};

				var header = ScrollItemWidget.Setup(hotkeyHeader, () => true, () => {});
				header.Get<LabelWidget>("LABEL").GetText = () => "Observer Commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, globalTemplate, hotkeyList);
			}

			// Unit
			{
				var hotkeys = new Dictionary<string, string>()
				{
					{ "AttackMoveKey", "Attack Move" },
					{ "StopKey", "Stop" },
					{ "ScatterKey", "Scatter" },
					{ "StanceCycleKey", "Cycle Stance" },
					{ "DeployKey", "Deploy" },
					{ "GuardKey", "Guard" }
				};

				var header = ScrollItemWidget.Setup(hotkeyHeader, () => true, () => {});
				header.Get<LabelWidget>("LABEL").GetText = () => "Unit Commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, unitTemplate, hotkeyList);
			}

			// Production
			{
				var hotkeys = new Dictionary<string, string>();
				for (var i = 1; i <= 24; i++)
					hotkeys.Add("Production{0:D2}Key".F(i), "Slot {0}".F(i));

				var header = ScrollItemWidget.Setup(hotkeyHeader, () => true, () => {});
				header.Get<LabelWidget>("LABEL").GetText = () => "Production Commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, productionTemplate, hotkeyList);
			}

			// Developer
			{
				var hotkeys = new Dictionary<string, string>()
				{
					{ "DevReloadChromeKey", "Reload Chrome" }
				};

				var header = ScrollItemWidget.Setup(hotkeyHeader, () => true, () => {});
				header.Get<LabelWidget>("LABEL").GetText = () => "Developer commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, globalTemplate, hotkeyList);
			}

			return () =>
			{
				// Remove focus from the selected hotkey widget
				// This is a bit of a hack, but works
				if (Ui.KeyboardFocusWidget != null && panel.GetOrNull(Ui.KeyboardFocusWidget.Id) != null)
				{
					Ui.KeyboardFocusWidget.YieldKeyboardFocus();
					Ui.KeyboardFocusWidget = null;
				}
			};
		}

		Action ResetInputPanel(Widget panel)
		{
			var gs = Game.Settings.Game;
			var ks = Game.Settings.Keys;
			var dgs = new GameSettings();
			var dks = new KeySettings();

			return () =>
			{
				gs.MouseScroll = dgs.MouseScroll;
				gs.LockMouseWindow = dgs.LockMouseWindow;
				gs.ViewportEdgeScroll = dgs.ViewportEdgeScroll;
				gs.ViewportEdgeScrollStep = dgs.ViewportEdgeScrollStep;
				gs.UIScrollSpeed = dgs.UIScrollSpeed;

				foreach (var f in ks.GetType().GetFields())
				{
					var value = (Hotkey)f.GetValue(dks);
					f.SetValue(ks, value);
					panel.Get(f.Name).Get<HotkeyEntryWidget>("HOTKEY").Key = value;
				}

				panel.Get<SliderWidget>("SCROLLSPEED_SLIDER").Value = gs.ViewportEdgeScrollStep;
				panel.Get<SliderWidget>("UI_SCROLLSPEED_SLIDER").Value = gs.UIScrollSpeed;

				MakeMouseFocusSettingsLive();
			};
		}

		Action InitAdvancedPanel(Widget panel)
		{
			var ds = Game.Settings.Debug;
			var ss = Game.Settings.Server;
			var gs = Game.Settings.Game;

			BindCheckboxPref(panel, "NAT_DISCOVERY", ss, "DiscoverNatDevices");
			BindCheckboxPref(panel, "VERBOSE_NAT_CHECKBOX", ss, "VerboseNatDiscovery");
			BindCheckboxPref(panel, "PERFTEXT_CHECKBOX", ds, "PerfText");
			BindCheckboxPref(panel, "PERFGRAPH_CHECKBOX", ds, "PerfGraph");
			BindCheckboxPref(panel, "CHECKUNSYNCED_CHECKBOX", ds, "SanityCheckUnsyncedCode");
			BindCheckboxPref(panel, "BOTDEBUG_CHECKBOX", ds, "BotDebug");
			BindCheckboxPref(panel, "FETCH_NEWS_CHECKBOX", gs, "FetchNews");

			return () => { };
		}

		Action ResetAdvancedPanel(Widget panel)
		{
			var ds = Game.Settings.Debug;
			var ss = Game.Settings.Server;
			var dds = new DebugSettings();
			var dss = new ServerSettings();

			return () =>
			{
				ss.DiscoverNatDevices = dss.DiscoverNatDevices;
				ss.VerboseNatDiscovery = dss.VerboseNatDiscovery;
				ds.PerfText = dds.PerfText;
				ds.PerfGraph = dds.PerfGraph;
				ds.SanityCheckUnsyncedCode = dds.SanityCheckUnsyncedCode;
				ds.BotDebug = dds.BotDebug;
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

		bool ShowAudioDeviceDropdown(DropDownButtonWidget dropdown, SoundDevice[] devices)
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

		static bool ShowWindowModeDropdown(DropDownButtonWidget dropdown, GraphicSettings s)
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

		static bool ShowLanguageDropdown(DropDownButtonWidget dropdown)
		{
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => Game.Settings.Graphics.Language == o,
					() => Game.Settings.Graphics.Language = o);

				item.Get<LabelWidget>("LABEL").GetText = () => FieldLoader.Translate(o);
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, Game.modData.Languages, setupItem);
			return true;
		}

		void MakeMouseFocusSettingsLive()
		{
			var gameSettings = Game.Settings.Game;

			if (gameSettings.LockMouseWindow)
				Game.Renderer.GrabWindowMouseFocus();
			else
				Game.Renderer.ReleaseWindowMouseFocus();
		}
	}
}
