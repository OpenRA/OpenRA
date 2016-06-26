#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SettingsLogic : ChromeLogic
	{
		enum PanelType { Display, Audio, Input, Advanced }

		static readonly string OriginalSoundDevice;
		static readonly string OriginalSoundEngine;
		static readonly WindowMode OriginalGraphicsMode;
		static readonly string OriginalGraphicsRenderer;
		static readonly int2 OriginalGraphicsWindowedSize;
		static readonly int2 OriginalGraphicsFullscreenSize;

		readonly Dictionary<PanelType, Action> leavePanelActions = new Dictionary<PanelType, Action>();
		readonly Dictionary<PanelType, Action> resetPanelActions = new Dictionary<PanelType, Action>();
		readonly Widget panelContainer, tabContainer;

		readonly ModData modData;
		readonly WorldRenderer worldRenderer;

		SoundDevice soundDevice;
		PanelType settingsPanel = PanelType.Display;

		static SettingsLogic()
		{
			var original = Game.Settings;
			OriginalSoundDevice = original.Sound.Device;
			OriginalSoundEngine = original.Sound.Engine;
			OriginalGraphicsMode = original.Graphics.Mode;
			OriginalGraphicsRenderer = original.Graphics.Renderer;
			OriginalGraphicsWindowedSize = original.Graphics.WindowedSize;
			OriginalGraphicsFullscreenSize = original.Graphics.FullscreenSize;
		}

		[ObjectCreator.UseCtor]
		public SettingsLogic(Widget widget, Action onExit, ModData modData, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;
			this.modData = modData;

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
				if (OriginalSoundDevice != current.Sound.Device ||
					OriginalSoundEngine != current.Sound.Engine ||
					OriginalGraphicsMode != current.Graphics.Mode ||
					OriginalGraphicsRenderer != current.Graphics.Renderer ||
					OriginalGraphicsWindowedSize != current.Graphics.WindowedSize ||
					OriginalGraphicsFullscreenSize != current.Graphics.FullscreenSize)
					ConfirmationDialogs.ButtonPrompt(
						title: "Restart Now?",
						text: "Some changes will not be applied until\nthe game is restarted. Restart now?",
						onConfirm: Game.Restart,
						onCancel: closeAndExit,
						confirmText: "Restart Now",
						cancelText: "Restart Later");
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
			textBox.OnLoseFocus = () => field.SetValue(ks, textBox.Key);
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

			BindCheckboxPref(panel, "HARDWARECURSORS_CHECKBOX", ds, "HardwareCursors");
			BindCheckboxPref(panel, "PIXELDOUBLE_CHECKBOX", ds, "PixelDouble");
			BindCheckboxPref(panel, "CURSORDOUBLE_CHECKBOX", ds, "CursorDouble");
			BindCheckboxPref(panel, "FRAME_LIMIT_CHECKBOX", ds, "CapFramerate");
			BindCheckboxPref(panel, "SHOW_SHELLMAP", gs, "ShowShellmap");
			BindCheckboxPref(panel, "DISPLAY_TARGET_LINES_CHECKBOX", gs, "DrawTargetLine");
			BindCheckboxPref(panel, "PLAYER_STANCE_COLORS_CHECKBOX", gs, "UsePlayerStanceColors");

			var languageDropDownButton = panel.Get<DropDownButtonWidget>("LANGUAGE_DROPDOWNBUTTON");
			languageDropDownButton.OnMouseDown = _ => ShowLanguageDropdown(languageDropDownButton, modData.Languages);
			languageDropDownButton.GetText = () => FieldLoader.Translate(ds.Language);

			var windowModeDropdown = panel.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => ShowWindowModeDropdown(windowModeDropdown, ds);
			windowModeDropdown.GetText = () => ds.Mode == WindowMode.Windowed ?
				"Windowed" : ds.Mode == WindowMode.Fullscreen ? "Fullscreen" : "Pseudo-Fullscreen";

			var statusBarsDropDown = panel.Get<DropDownButtonWidget>("STATUS_BAR_DROPDOWN");
			statusBarsDropDown.OnMouseDown = _ => ShowStatusBarsDropdown(statusBarsDropDown, gs);
			statusBarsDropDown.GetText = () => gs.StatusBars.ToString() == "Standard" ?
				"Standard" : gs.StatusBars.ToString() == "DamageShow" ? "Show On Damage" : "Always Show";

			// Update zoom immediately
			var pixelDoubleCheckbox = panel.Get<CheckboxWidget>("PIXELDOUBLE_CHECKBOX");
			var pixelDoubleOnClick = pixelDoubleCheckbox.OnClick;
			pixelDoubleCheckbox.OnClick = () =>
			{
				pixelDoubleOnClick();
				worldRenderer.Viewport.Zoom = ds.PixelDouble ? 2 : 1;
			};

			// Cursor doubling is only supported with software cursors and when pixel doubling is enabled
			var cursorDoubleCheckbox = panel.Get<CheckboxWidget>("CURSORDOUBLE_CHECKBOX");
			cursorDoubleCheckbox.IsDisabled = () => !ds.PixelDouble || Game.Cursor is HardwareCursor;

			var cursorDoubleIsChecked = cursorDoubleCheckbox.IsChecked;
			cursorDoubleCheckbox.IsChecked = () => !cursorDoubleCheckbox.IsDisabled() && cursorDoubleIsChecked();

			panel.Get("WINDOW_RESOLUTION").IsVisible = () => ds.Mode == WindowMode.Windowed;
			var windowWidth = panel.Get<TextFieldWidget>("WINDOW_WIDTH");
			windowWidth.Text = ds.WindowedSize.X.ToString();

			var windowHeight = panel.Get<TextFieldWidget>("WINDOW_HEIGHT");
			windowHeight.Text = ds.WindowedSize.Y.ToString();

			var frameLimitTextfield = panel.Get<TextFieldWidget>("FRAME_LIMIT_TEXTFIELD");
			frameLimitTextfield.Text = ds.MaxFramerate.ToString();
			var escPressed = false;
			frameLimitTextfield.OnLoseFocus = () =>
			{
				if (escPressed)
				{
					escPressed = false;
					return;
				}

				int fps;
				Exts.TryParseIntegerInvariant(frameLimitTextfield.Text, out fps);
				ds.MaxFramerate = fps.Clamp(1, 1000);
				frameLimitTextfield.Text = ds.MaxFramerate.ToString();
			};

			frameLimitTextfield.OnEnterKey = () => { frameLimitTextfield.YieldKeyboardFocus(); return true; };
			frameLimitTextfield.OnEscKey = () =>
			{
				frameLimitTextfield.Text = ds.MaxFramerate.ToString();
				escPressed = true;
				frameLimitTextfield.YieldKeyboardFocus();
				return true;
			};

			frameLimitTextfield.IsDisabled = () => !ds.CapFramerate;

			// Player profile
			var ps = Game.Settings.Player;

			var nameTextfield = panel.Get<TextFieldWidget>("PLAYERNAME");
			nameTextfield.IsDisabled = () => worldRenderer.World.Type != WorldType.Shellmap;
			nameTextfield.Text = Settings.SanitizedPlayerName(ps.Name);
			nameTextfield.OnLoseFocus = () =>
			{
				if (escPressed)
				{
					escPressed = false;
					return;
				}

				nameTextfield.Text = nameTextfield.Text.Trim();
				if (nameTextfield.Text.Length == 0)
					nameTextfield.Text = Settings.SanitizedPlayerName(ps.Name);
				else
				{
					nameTextfield.Text = Settings.SanitizedPlayerName(nameTextfield.Text);
					ps.Name = nameTextfield.Text;
				}
			};

			nameTextfield.OnEnterKey = () => { nameTextfield.YieldKeyboardFocus(); return true; };
			nameTextfield.OnEscKey = () =>
			{
				nameTextfield.Text = Settings.SanitizedPlayerName(ps.Name);
				escPressed = true;
				nameTextfield.YieldKeyboardFocus();
				return true;
			};

			var colorPreview = panel.Get<ColorPreviewManagerWidget>("COLOR_MANAGER");
			colorPreview.Color = ps.Color;

			var colorDropdown = panel.Get<DropDownButtonWidget>("PLAYERCOLOR");
			colorDropdown.IsDisabled = () => worldRenderer.World.Type != WorldType.Shellmap;
			colorDropdown.OnMouseDown = _ => ColorPickerLogic.ShowColorDropDown(colorDropdown, colorPreview, worldRenderer.World);
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

			BindCheckboxPref(panel, "CASH_TICKS", ss, "CashTicks");
			BindCheckboxPref(panel, "MUTE_SOUND", ss, "Mute");

			BindSliderPref(panel, "SOUND_VOLUME", ss, "SoundVolume");
			BindSliderPref(panel, "MUSIC_VOLUME", ss, "MusicVolume");
			BindSliderPref(panel, "VIDEO_VOLUME", ss, "VideoVolume");

			var muteCheckbox = panel.Get<CheckboxWidget>("MUTE_SOUND");
			var muteCheckboxOnClick = muteCheckbox.OnClick;
			muteCheckbox.OnClick = () =>
			{
				muteCheckboxOnClick();

				if (ss.Mute)
					Game.Sound.MuteAudio();
				else
					Game.Sound.UnmuteAudio();
			};

			if (!ss.Mute)
			{
				panel.Get<SliderWidget>("SOUND_VOLUME").OnChange += x => Game.Sound.SoundVolume = x;
				panel.Get<SliderWidget>("MUSIC_VOLUME").OnChange += x => Game.Sound.MusicVolume = x;
				panel.Get<SliderWidget>("VIDEO_VOLUME").OnChange += x => Game.Sound.VideoVolume = x;
			}

			var devices = Game.Sound.AvailableDevices();
			soundDevice = devices.FirstOrDefault(d => d.Engine == ss.Engine && d.Device == ss.Device) ?? devices.First();

			var audioDeviceDropdown = panel.Get<DropDownButtonWidget>("AUDIO_DEVICE");
			audioDeviceDropdown.OnMouseDown = _ => ShowAudioDeviceDropdown(audioDeviceDropdown, devices);

			var deviceFont = Game.Renderer.Fonts[audioDeviceDropdown.Font];
			var deviceLabel = new CachedTransform<SoundDevice, string>(
				s => WidgetUtils.TruncateText(s.Label, audioDeviceDropdown.UsableWidth, deviceFont));
			audioDeviceDropdown.GetText = () => deviceLabel.Update(soundDevice);

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
				ss.SoundVolume = dss.SoundVolume;
				ss.MusicVolume = dss.MusicVolume;
				ss.VideoVolume = dss.VideoVolume;
				ss.CashTicks = dss.CashTicks;
				ss.Mute = dss.Mute;
				ss.Device = dss.Device;
				ss.Engine = dss.Engine;

				panel.Get<SliderWidget>("SOUND_VOLUME").Value = ss.SoundVolume;
				Game.Sound.SoundVolume = ss.SoundVolume;
				panel.Get<SliderWidget>("MUSIC_VOLUME").Value = ss.MusicVolume;
				Game.Sound.MusicVolume = ss.MusicVolume;
				panel.Get<SliderWidget>("VIDEO_VOLUME").Value = ss.VideoVolume;
				Game.Sound.VideoVolume = ss.VideoVolume;
				Game.Sound.UnmuteAudio();
				soundDevice = Game.Sound.AvailableDevices().First();
			};
		}

		Action InitInputPanel(Widget panel)
		{
			var gs = Game.Settings.Game;
			var ks = Game.Settings.Keys;

			BindCheckboxPref(panel, "CLASSICORDERS_CHECKBOX", gs, "UseClassicMouseStyle");
			BindCheckboxPref(panel, "EDGESCROLL_CHECKBOX", gs, "ViewportEdgeScroll");
			BindCheckboxPref(panel, "LOCKMOUSE_CHECKBOX", gs, "LockMouseWindow");
			BindCheckboxPref(panel, "ALLOW_ZOOM_CHECKBOX", gs, "AllowZoom");
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

			var middleMouseScrollDropdown = panel.Get<DropDownButtonWidget>("MIDDLE_MOUSE_SCROLL");
			middleMouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(middleMouseScrollDropdown, gs, false);
			middleMouseScrollDropdown.GetText = () => gs.MiddleMouseScroll.ToString();

			var rightMouseScrollDropdown = panel.Get<DropDownButtonWidget>("RIGHT_MOUSE_SCROLL");
			rightMouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(rightMouseScrollDropdown, gs, true);
			rightMouseScrollDropdown.GetText = () => gs.RightMouseScroll.ToString();

			var zoomModifierDropdown = panel.Get<DropDownButtonWidget>("ZOOM_MODIFIER");
			zoomModifierDropdown.OnMouseDown = _ => ShowZoomModifierDropdown(zoomModifierDropdown, gs);
			zoomModifierDropdown.GetText = () => gs.ZoomModifier.ToString();

			var hotkeyList = panel.Get<ScrollPanelWidget>("HOTKEY_LIST");
			hotkeyList.Layout = new GridLayout(hotkeyList);
			var hotkeyHeader = hotkeyList.Get<ScrollItemWidget>("HEADER");
			var globalTemplate = hotkeyList.Get("GLOBAL_TEMPLATE");
			var unitTemplate = hotkeyList.Get("UNIT_TEMPLATE");
			var productionTemplate = hotkeyList.Get("PRODUCTION_TEMPLATE");
			var developerTemplate = hotkeyList.Get("DEVELOPER_TEMPLATE");
			hotkeyList.RemoveChildren();

			Func<bool> returnTrue = () => true;
			Action doNothing = () => { };

			// Game
			{
				var hotkeys = new Dictionary<string, string>()
				{
					{ "CycleBaseKey", "Jump to base" },
					{ "ToLastEventKey", "Jump to last radar event" },
					{ "ToSelectionKey", "Jump to selection" },
					{ "SelectAllUnitsKey", "Select all combat units" },
					{ "SelectUnitsByTypeKey", "Select units by type" },

					{ "PlaceBeaconKey", "Place beacon" },

					{ "PauseKey", "Pause / Unpause" },
					{ "SellKey", "Sell mode" },
					{ "PowerDownKey", "Power-down mode" },
					{ "RepairKey", "Repair mode" },

					{ "NextProductionTabKey", "Next production tab" },
					{ "PreviousProductionTabKey", "Previous production tab" },
					{ "CycleProductionBuildingsKey", "Cycle production facilities" },

					{ "CycleStatusBarsKey", "Cycle status bars display" },
					{ "TogglePixelDoubleKey", "Toggle pixel doubling" },
					{ "ToggleMuteKey", "Toggle audio mute" },
					{ "TogglePlayerStanceColorsKey", "Toggle player stance colors" },

					{ "MapScrollUp", "Map scroll up" },
					{ "MapScrollDown", "Map scroll down" },
					{ "MapScrollLeft", "Map scroll left" },
					{ "MapScrollRight", "Map scroll right" },

					{ "MapPushTop", "Map push to top" },
					{ "MapPushBottom", "Map push to bottom" },
					{ "MapPushLeftEdge", "Map push to left edge" },
					{ "MapPushRightEdge", "Map push to right edge" }
				};

				var header = ScrollItemWidget.Setup(hotkeyHeader, returnTrue, doNothing);
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
					{ "ObserverWorldView", "Disable Shroud" },
					{ "PauseKey", "Pause/Play" },
					{ "ReplaySpeedSlowKey", "Slow speed" },
					{ "ReplaySpeedRegularKey", "Regular speed" },
					{ "ReplaySpeedFastKey", "Fast speed" },
					{ "ReplaySpeedMaxKey", "Maximum speed" }
				};

				var header = ScrollItemWidget.Setup(hotkeyHeader, returnTrue, doNothing);
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

				var header = ScrollItemWidget.Setup(hotkeyHeader, returnTrue, doNothing);
				header.Get<LabelWidget>("LABEL").GetText = () => "Unit Commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, unitTemplate, hotkeyList);
			}

			// Production
			{
				var hotkeys = new Dictionary<string, string>()
				{
					{ "ProductionTypeBuildingKey", "Building Tab" },
					{ "ProductionTypeDefenseKey", "Defense Tab" },
					{ "ProductionTypeInfantryKey", "Infantry Tab" },
					{ "ProductionTypeVehicleKey", "Vehicle Tab" },
					{ "ProductionTypeAircraftKey", "Aircraft Tab" },
					{ "ProductionTypeNavalKey", "Naval Tab" },
					{ "ProductionTypeTankKey", "Tank Tab" },
					{ "ProductionTypeMerchantKey", "Starport Tab" },
					{ "ProductionTypeUpgradeKey", "Upgrade Tab" }
				};

				for (var i = 1; i <= 24; i++)
					hotkeys.Add("Production{0:D2}Key".F(i), "Slot {0}".F(i));

				var header = ScrollItemWidget.Setup(hotkeyHeader, returnTrue, doNothing);
				header.Get<LabelWidget>("LABEL").GetText = () => "Production Commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, productionTemplate, hotkeyList);
			}

			// Support powers
			{
				var hotkeys = new Dictionary<string, string>();
				for (var i = 1; i <= 6; i++)
					hotkeys.Add("SupportPower{0:D2}Key".F(i), "Slot {0}".F(i));

				var header = ScrollItemWidget.Setup(hotkeyHeader, returnTrue, doNothing);
				header.Get<LabelWidget>("LABEL").GetText = () => "Support Power Commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, productionTemplate, hotkeyList);
			}

			// Developer
			{
				var hotkeys = new Dictionary<string, string>()
				{
					{ "DevReloadChromeKey", "Reload Chrome" },
					{ "HideUserInterfaceKey", "Hide UI" },
					{ "TakeScreenshotKey", "Take screenshot" }
				};

				var header = ScrollItemWidget.Setup(hotkeyHeader, returnTrue, doNothing);
				header.Get<LabelWidget>("LABEL").GetText = () => "Developer commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, developerTemplate, hotkeyList);
			}

			// Music
			{
				var hotkeys = new Dictionary<string, string>()
				{
					{ "NextTrack", "Next" },
					{ "PreviousTrack", "Previous" },
					{ "StopMusic", "Stop" },
					{ "PauseMusic", "Pause or Resume" }
				};

				var header = ScrollItemWidget.Setup(hotkeyHeader, returnTrue, doNothing);
				header.Get<LabelWidget>("LABEL").GetText = () => "Music commands";
				hotkeyList.AddChild(header);

				foreach (var kv in hotkeys)
					BindHotkeyPref(kv, ks, developerTemplate, hotkeyList);
			}

			return () => { };
		}

		Action ResetInputPanel(Widget panel)
		{
			var gs = Game.Settings.Game;
			var ks = Game.Settings.Keys;
			var dgs = new GameSettings();
			var dks = new KeySettings();

			return () =>
			{
				gs.UseClassicMouseStyle = dgs.UseClassicMouseStyle;
				gs.MiddleMouseScroll = dgs.MiddleMouseScroll;
				gs.RightMouseScroll = dgs.RightMouseScroll;
				gs.LockMouseWindow = dgs.LockMouseWindow;
				gs.ViewportEdgeScroll = dgs.ViewportEdgeScroll;
				gs.ViewportEdgeScrollStep = dgs.ViewportEdgeScrollStep;
				gs.UIScrollSpeed = dgs.UIScrollSpeed;
				gs.AllowZoom = dgs.AllowZoom;
				gs.ZoomModifier = dgs.ZoomModifier;

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
			BindCheckboxPref(panel, "PERFTEXT_CHECKBOX", ds, "PerfText");
			BindCheckboxPref(panel, "PERFGRAPH_CHECKBOX", ds, "PerfGraph");
			BindCheckboxPref(panel, "CHECKUNSYNCED_CHECKBOX", ds, "SanityCheckUnsyncedCode");
			BindCheckboxPref(panel, "BOTDEBUG_CHECKBOX", ds, "BotDebug");
			BindCheckboxPref(panel, "FETCH_NEWS_CHECKBOX", gs, "FetchNews");
			BindCheckboxPref(panel, "LUADEBUG_CHECKBOX", ds, "LuaDebug");
			BindCheckboxPref(panel, "SENDSYSINFO_CHECKBOX", ds, "SendSystemInformation");

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
				ds.PerfText = dds.PerfText;
				ds.PerfGraph = dds.PerfGraph;
				ds.SanityCheckUnsyncedCode = dds.SanityCheckUnsyncedCode;
				ds.BotDebug = dds.BotDebug;
				ds.LuaDebug = dds.LuaDebug;
			};
		}

		static bool ShowMouseScrollDropdown(DropDownButtonWidget dropdown, GameSettings s, bool rightMouse)
		{
			var options = new Dictionary<string, MouseScrollType>()
			{
				{ "Disabled", MouseScrollType.Disabled },
				{ "Standard", MouseScrollType.Standard },
				{ "Inverted", MouseScrollType.Inverted },
				{ "Joystick", MouseScrollType.Joystick },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => (rightMouse ? s.RightMouseScroll : s.MiddleMouseScroll) == options[o],
					() => { if (rightMouse) s.RightMouseScroll = options[o]; else s.MiddleMouseScroll = options[o]; });
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
			return true;
		}

		static bool ShowZoomModifierDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, Modifiers>()
			{
				{ "Alt", Modifiers.Alt },
				{ "Ctrl", Modifiers.Ctrl },
				{ "Meta", Modifiers.Meta },
				{ "Shift", Modifiers.Shift },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.ZoomModifier == options[o],
					() => s.ZoomModifier = options[o]);
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

				var deviceLabel = item.Get<LabelWidget>("LABEL");
				var font = Game.Renderer.Fonts[deviceLabel.Font];
				var label = WidgetUtils.TruncateText(options[o].Label, deviceLabel.Bounds.Width, font);
				deviceLabel.GetText = () => label;
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

		static bool ShowLanguageDropdown(DropDownButtonWidget dropdown, IEnumerable<string> languages)
		{
			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => Game.Settings.Graphics.Language == o,
					() => Game.Settings.Graphics.Language = o);

				item.Get<LabelWidget>("LABEL").GetText = () => FieldLoader.Translate(o);
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, languages, setupItem);
			return true;
		}

		static bool ShowStatusBarsDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, StatusBarsType>()
			{
				{ "Standard", StatusBarsType.Standard },
				{ "Show On Damage", StatusBarsType.DamageShow },
				{ "Always Show", StatusBarsType.AlwaysShow },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.StatusBars == options[o],
					() => s.StatusBars = options[o]);

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
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
