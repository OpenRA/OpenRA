#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SettingsLogic : ChromeLogic
	{
		enum PanelType { Display, Audio, Input, Hotkeys, Advanced }

		static readonly string OriginalSoundDevice;
		static readonly WindowMode OriginalGraphicsMode;
		static readonly int2 OriginalGraphicsWindowedSize;
		static readonly int2 OriginalGraphicsFullscreenSize;
		static readonly bool OriginalServerDiscoverNatDevices;

		readonly Dictionary<PanelType, Action> leavePanelActions = new Dictionary<PanelType, Action>();
		readonly Dictionary<PanelType, Action> resetPanelActions = new Dictionary<PanelType, Action>();
		readonly Widget panelContainer, tabContainer;

		readonly ModData modData;
		readonly WorldRenderer worldRenderer;
		readonly Dictionary<string, MiniYaml> logicArgs;

		SoundDevice soundDevice;
		PanelType settingsPanel = PanelType.Display;

		ButtonWidget selectedHotkeyButton;
		HotkeyEntryWidget hotkeyEntryWidget;
		HotkeyDefinition duplicateHotkeyDefinition, selectedHotkeyDefinition;
		bool isHotkeyValid;
		bool isHotkeyDefault;

		static SettingsLogic()
		{
			var original = Game.Settings;
			OriginalSoundDevice = original.Sound.Device;
			OriginalGraphicsMode = original.Graphics.Mode;
			OriginalGraphicsWindowedSize = original.Graphics.WindowedSize;
			OriginalGraphicsFullscreenSize = original.Graphics.FullscreenSize;
			OriginalServerDiscoverNatDevices = original.Server.DiscoverNatDevices;
		}

		[ObjectCreator.UseCtor]
		public SettingsLogic(Widget widget, Action onExit, ModData modData, WorldRenderer worldRenderer, Dictionary<string, MiniYaml> logicArgs)
		{
			this.worldRenderer = worldRenderer;
			this.modData = modData;
			this.logicArgs = logicArgs;

			panelContainer = widget.Get("SETTINGS_PANEL");
			tabContainer = widget.Get("TAB_CONTAINER");

			RegisterSettingsPanel(PanelType.Display, InitDisplayPanel, ResetDisplayPanel, "DISPLAY_PANEL", "DISPLAY_TAB");
			RegisterSettingsPanel(PanelType.Audio, InitAudioPanel, ResetAudioPanel, "AUDIO_PANEL", "AUDIO_TAB");
			RegisterSettingsPanel(PanelType.Input, InitInputPanel, ResetInputPanel, "INPUT_PANEL", "INPUT_TAB");
			RegisterSettingsPanel(PanelType.Hotkeys, InitHotkeysPanel, ResetHotkeysPanel, "HOTKEYS_PANEL", "HOTKEYS_TAB");
			RegisterSettingsPanel(PanelType.Advanced, InitAdvancedPanel, ResetAdvancedPanel, "ADVANCED_PANEL", "ADVANCED_TAB");

			panelContainer.Get<ButtonWidget>("BACK_BUTTON").OnClick = () =>
			{
				leavePanelActions[settingsPanel]();
				var current = Game.Settings;
				current.Save();

				Action closeAndExit = () => { Ui.CloseWindow(); onExit(); };
				if (current.Sound.Device != OriginalSoundDevice ||
				    current.Graphics.Mode != OriginalGraphicsMode ||
				    current.Graphics.WindowedSize != OriginalGraphicsWindowedSize ||
					current.Graphics.FullscreenSize != OriginalGraphicsFullscreenSize ||
					current.Server.DiscoverNatDevices != OriginalServerDiscoverNatDevices)
				{
					Action restart = () =>
					{
						var external = Game.ExternalMods[ExternalMod.MakeKey(Game.ModData.Manifest)];
						Game.SwitchToExternalMod(external, null, closeAndExit);
					};

					ConfirmationDialogs.ButtonPrompt(
						title: "Restart Now?",
						text: "Some changes will not be applied until\nthe game is restarted. Restart now?",
						onConfirm: restart,
						onCancel: closeAndExit,
						confirmText: "Restart Now",
						cancelText: "Restart Later");
				}
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

		void BindHotkeyPref(HotkeyDefinition hd, Widget template, Widget parent)
		{
			var key = template.Clone() as Widget;
			key.Id = hd.Name;
			key.IsVisible = () => true;

			key.Get<LabelWidget>("FUNCTION").GetText = () => hd.Description + ":";

			var remapButton = key.Get<ButtonWidget>("HOTKEY");
			WidgetUtils.TruncateButtonToTooltip(remapButton, modData.Hotkeys[hd.Name].GetValue().DisplayString());

			remapButton.IsHighlighted = () => selectedHotkeyDefinition == hd;

			var hotkeyValidColor = ChromeMetrics.Get<Color>("HotkeyColor");
			var hotkeyInvalidColor = ChromeMetrics.Get<Color>("HotkeyColorInvalid");

			remapButton.GetColor = () =>
			{
				return hd.HasDuplicates ? hotkeyInvalidColor : hotkeyValidColor;
			};

			if (selectedHotkeyDefinition == hd)
			{
				selectedHotkeyButton = remapButton;
				hotkeyEntryWidget.Key = modData.Hotkeys[hd.Name].GetValue();
				ValidateHotkey();
			}

			remapButton.OnClick = () =>
			{
				selectedHotkeyDefinition = hd;
				selectedHotkeyButton = remapButton;
				hotkeyEntryWidget.Key = modData.Hotkeys[hd.Name].GetValue();
				ValidateHotkey();
				hotkeyEntryWidget.TakeKeyboardFocus();
			};

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
			BindCheckboxPref(panel, "PLAYER_STANCE_COLORS_CHECKBOX", gs, "UsePlayerStanceColors");

			var languageDropDownButton = panel.Get<DropDownButtonWidget>("LANGUAGE_DROPDOWNBUTTON");
			languageDropDownButton.OnMouseDown = _ => ShowLanguageDropdown(languageDropDownButton, modData.Languages);
			languageDropDownButton.GetText = () => FieldLoader.Translate(ds.Language);

			var windowModeDropdown = panel.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => ShowWindowModeDropdown(windowModeDropdown, ds);
			windowModeDropdown.GetText = () => ds.Mode == WindowMode.Windowed ?
				"Windowed" : ds.Mode == WindowMode.Fullscreen ? "Fullscreen (Legacy)" : "Fullscreen";

			var statusBarsDropDown = panel.Get<DropDownButtonWidget>("STATUS_BAR_DROPDOWN");
			statusBarsDropDown.OnMouseDown = _ => ShowStatusBarsDropdown(statusBarsDropDown, gs);
			statusBarsDropDown.GetText = () => gs.StatusBars == StatusBarsType.Standard ?
				"Standard" : gs.StatusBars == StatusBarsType.DamageShow ? "Show On Damage" : "Always Show";

			var targetLinesDropDown = panel.Get<DropDownButtonWidget>("TARGET_LINES_DROPDOWN");
			targetLinesDropDown.OnMouseDown = _ => ShowTargetLinesDropdown(targetLinesDropDown, gs);
			targetLinesDropDown.GetText = () => gs.TargetLines == TargetLinesType.Automatic ?
				"Automatic" : gs.TargetLines == TargetLinesType.Manual ? "Manual" : "Disabled";

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
			colorDropdown.Get<ColorBlockWidget>("COLORBLOCK").GetColor = () => ps.Color;

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
			var ps = Game.Settings.Player;
			var dds = new GraphicSettings();
			var dps = new PlayerSettings();
			return () =>
			{
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
			soundDevice = devices.FirstOrDefault(d => d.Device == ss.Device) ?? devices.First();

			var audioDeviceDropdown = panel.Get<DropDownButtonWidget>("AUDIO_DEVICE");
			audioDeviceDropdown.OnMouseDown = _ => ShowAudioDeviceDropdown(audioDeviceDropdown, devices);

			var deviceFont = Game.Renderer.Fonts[audioDeviceDropdown.Font];
			var deviceLabel = new CachedTransform<SoundDevice, string>(
				s => WidgetUtils.TruncateText(s.Label, audioDeviceDropdown.UsableWidth, deviceFont));
			audioDeviceDropdown.GetText = () => deviceLabel.Update(soundDevice);

			return () =>
			{
				ss.Device = soundDevice.Device;
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

			return () => { };
		}

		Action InitHotkeysPanel(Widget panel)
		{
			var hotkeyDialogRoot = panel.Get("HOTKEY_DIALOG_ROOT");
			var hotkeyList = panel.Get<ScrollPanelWidget>("HOTKEY_LIST");
			hotkeyList.Layout = new GridLayout(hotkeyList);
			var hotkeyHeader = hotkeyList.Get<ScrollItemWidget>("HEADER");
			var templates = hotkeyList.Get("TEMPLATES");
			hotkeyList.RemoveChildren();

			Func<bool> returnTrue = () => true;
			Action doNothing = () => { };

			MiniYaml hotkeyGroups;
			if (logicArgs.TryGetValue("HotkeyGroups", out hotkeyGroups))
			{
				InitHotkeyRemapDialog(panel);

				foreach (var hg in hotkeyGroups.Nodes)
				{
					var templateNode = hg.Value.Nodes.FirstOrDefault(n => n.Key == "Template");
					var typesNode = hg.Value.Nodes.FirstOrDefault(n => n.Key == "Types");
					if (templateNode == null || typesNode == null)
						continue;

					var header = ScrollItemWidget.Setup(hotkeyHeader, returnTrue, doNothing);
					header.Get<LabelWidget>("LABEL").GetText = () => hg.Key;
					hotkeyList.AddChild(header);

					var types = FieldLoader.GetValue<string[]>("Types", typesNode.Value.Value);
					var added = new HashSet<HotkeyDefinition>();
					var template = templates.Get(templateNode.Value.Value);

					foreach (var t in types)
					{
						foreach (var hd in modData.Hotkeys.Definitions.Where(k => k.Types.Contains(t)))
						{
							if (added.Add(hd))
							{
								if (selectedHotkeyDefinition == null)
									selectedHotkeyDefinition = hd;

								if (modData.Hotkeys.GetFirstDuplicate(hd.Name, modData.Hotkeys[hd.Name].GetValue(), hd) != null)
									hd.HasDuplicates = true;

								BindHotkeyPref(hd, template, hotkeyList);
							}
						}
					}
				}
			}

			return () =>
			{
				hotkeyEntryWidget.Key = modData.Hotkeys[selectedHotkeyDefinition.Name].GetValue();
				hotkeyEntryWidget.ForceYieldKeyboardFocus();
			};
		}

		Action ResetInputPanel(Widget panel)
		{
			var gs = Game.Settings.Game;
			var dgs = new GameSettings();

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

				panel.Get<SliderWidget>("SCROLLSPEED_SLIDER").Value = gs.ViewportEdgeScrollStep;
				panel.Get<SliderWidget>("UI_SCROLLSPEED_SLIDER").Value = gs.UIScrollSpeed;

				MakeMouseFocusSettingsLive();
			};
		}

		Action ResetHotkeysPanel(Widget panel)
		{
			return () =>
			{
				foreach (var hd in modData.Hotkeys.Definitions)
				{
					modData.Hotkeys.Set(hd.Name, hd.Default);
					WidgetUtils.TruncateButtonToTooltip(panel.Get(hd.Name).Get<ButtonWidget>("HOTKEY"), hd.Default.DisplayString());
				}
			};
		}

		Action InitAdvancedPanel(Widget panel)
		{
			var ds = Game.Settings.Debug;
			var ss = Game.Settings.Server;
			var gs = Game.Settings.Game;

			// Advanced
			BindCheckboxPref(panel, "NAT_DISCOVERY", ss, "DiscoverNatDevices");
			BindCheckboxPref(panel, "PERFTEXT_CHECKBOX", ds, "PerfText");
			BindCheckboxPref(panel, "PERFGRAPH_CHECKBOX", ds, "PerfGraph");
			BindCheckboxPref(panel, "FETCH_NEWS_CHECKBOX", gs, "FetchNews");
			BindCheckboxPref(panel, "SENDSYSINFO_CHECKBOX", ds, "SendSystemInformation");
			BindCheckboxPref(panel, "CHECK_VERSION_CHECKBOX", ds, "CheckVersion");

			var ssi = panel.Get<CheckboxWidget>("SENDSYSINFO_CHECKBOX");
			ssi.IsDisabled = () => !gs.FetchNews;

			// Developer
			BindCheckboxPref(panel, "BOTDEBUG_CHECKBOX", ds, "BotDebug");
			BindCheckboxPref(panel, "LUADEBUG_CHECKBOX", ds, "LuaDebug");
			BindCheckboxPref(panel, "REPLAY_COMMANDS_CHECKBOX", ds, "EnableDebugCommandsInReplays");
			BindCheckboxPref(panel, "CHECKUNSYNCED_CHECKBOX", ds, "SyncCheckUnsyncedCode");
			BindCheckboxPref(panel, "CHECKBOTSYNC_CHECKBOX", ds, "SyncCheckBotModuleCode");

			panel.Get("DEBUG_OPTIONS").IsVisible = () => ds.DisplayDeveloperSettings;
			panel.Get("DEBUG_HIDDEN_LABEL").IsVisible = () => !ds.DisplayDeveloperSettings;

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
				ds.SyncCheckUnsyncedCode = dds.SyncCheckUnsyncedCode;
				ds.SyncCheckBotModuleCode = dds.SyncCheckBotModuleCode;
				ds.BotDebug = dds.BotDebug;
				ds.LuaDebug = dds.LuaDebug;
				ds.SendSystemInformation = dds.SendSystemInformation;
				ds.CheckVersion = dds.CheckVersion;
				ds.EnableDebugCommandsInReplays = dds.EnableDebugCommandsInReplays;
			};
		}

		static void ShowMouseScrollDropdown(DropDownButtonWidget dropdown, GameSettings s, bool rightMouse)
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
		}

		static void ShowZoomModifierDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, Modifiers>()
			{
				{ "Alt", Modifiers.Alt },
				{ "Ctrl", Modifiers.Ctrl },
				{ "Meta", Modifiers.Meta },
				{ "Shift", Modifiers.Shift },
				{ "None", Modifiers.None }
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
		}

		void ShowAudioDeviceDropdown(DropDownButtonWidget dropdown, SoundDevice[] devices)
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
		}

		static void ShowWindowModeDropdown(DropDownButtonWidget dropdown, GraphicSettings s)
		{
			var options = new Dictionary<string, WindowMode>()
			{
				{ "Fullscreen", WindowMode.PseudoFullscreen },
				{ "Fullscreen (Legacy)", WindowMode.Fullscreen },
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
		}

		static void ShowLanguageDropdown(DropDownButtonWidget dropdown, IEnumerable<string> languages)
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
		}

		static void ShowStatusBarsDropdown(DropDownButtonWidget dropdown, GameSettings s)
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
		}

		static void ShowTargetLinesDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, TargetLinesType>()
			{
				{ "Automatic", TargetLinesType.Automatic },
				{ "Manual", TargetLinesType.Manual },
				{ "Disabled", TargetLinesType.Disabled },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.TargetLines == options[o],
					() => s.TargetLines = options[o]);

				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
		}

		void MakeMouseFocusSettingsLive()
		{
			var gameSettings = Game.Settings.Game;

			if (gameSettings.LockMouseWindow)
				Game.Renderer.GrabWindowMouseFocus();
			else
				Game.Renderer.ReleaseWindowMouseFocus();
		}

		void InitHotkeyRemapDialog(Widget panel)
		{
			var label = new CachedTransform<HotkeyDefinition, string>(hd => hd.Description + ":");
			panel.Get<LabelWidget>("HOTKEY_LABEL").GetText = () => label.Update(selectedHotkeyDefinition);

			var duplicateNotice = panel.Get<LabelWidget>("DUPLICATE_NOTICE");
			duplicateNotice.TextColor = ChromeMetrics.Get<Color>("NoticeErrorColor");
			duplicateNotice.IsVisible = () => !isHotkeyValid;
			var duplicateNoticeText = new CachedTransform<HotkeyDefinition, string>(hd => hd != null ? duplicateNotice.Text.F(hd.Description) : duplicateNotice.Text);
			duplicateNotice.GetText = () => duplicateNoticeText.Update(duplicateHotkeyDefinition);

			var defaultNotice = panel.Get<LabelWidget>("DEFAULT_NOTICE");
			defaultNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			defaultNotice.IsVisible = () => isHotkeyValid && isHotkeyDefault;

			var originalNotice = panel.Get<LabelWidget>("ORIGINAL_NOTICE");
			originalNotice.TextColor = ChromeMetrics.Get<Color>("NoticeInfoColor");
			originalNotice.IsVisible = () => isHotkeyValid && !isHotkeyDefault;
			var originalNoticeText = new CachedTransform<HotkeyDefinition, string>(hd => originalNotice.Text.F(hd.Default.DisplayString()));
			originalNotice.GetText = () => originalNoticeText.Update(selectedHotkeyDefinition);

			var resetButton = panel.Get<ButtonWidget>("RESET_HOTKEY_BUTTON");
			resetButton.IsDisabled = () => isHotkeyDefault;
			resetButton.OnClick = ResetHotkey;

			var clearButton = panel.Get<ButtonWidget>("CLEAR_HOTKEY_BUTTON");
			clearButton.IsDisabled = () => !hotkeyEntryWidget.Key.IsValid();
			clearButton.OnClick = ClearHotkey;

			hotkeyEntryWidget = panel.Get<HotkeyEntryWidget>("HOTKEY_ENTRY");
			hotkeyEntryWidget.IsValid = () => isHotkeyValid;
			hotkeyEntryWidget.OnLoseFocus = ValidateHotkey;
		}

		void ValidateHotkey()
		{
			duplicateHotkeyDefinition = modData.Hotkeys.GetFirstDuplicate(selectedHotkeyDefinition.Name, hotkeyEntryWidget.Key, selectedHotkeyDefinition);
			isHotkeyValid = duplicateHotkeyDefinition == null;
			isHotkeyDefault = hotkeyEntryWidget.Key == selectedHotkeyDefinition.Default || (!hotkeyEntryWidget.Key.IsValid() && !selectedHotkeyDefinition.Default.IsValid());

			if (isHotkeyValid)
				SaveHotkey();
		}

		void SaveHotkey()
		{
			WidgetUtils.TruncateButtonToTooltip(selectedHotkeyButton, hotkeyEntryWidget.Key.DisplayString());
			modData.Hotkeys.Set(selectedHotkeyDefinition.Name, hotkeyEntryWidget.Key);
			Game.Settings.Save();

			foreach (var hd in modData.Hotkeys.Definitions)
				hd.HasDuplicates = modData.Hotkeys.GetFirstDuplicate(hd.Name, modData.Hotkeys[hd.Name].GetValue(), hd) != null;
		}

		void ResetHotkey()
		{
			hotkeyEntryWidget.Key = selectedHotkeyDefinition.Default;
			hotkeyEntryWidget.YieldKeyboardFocus();
		}

		void ClearHotkey()
		{
			hotkeyEntryWidget.Key = Hotkey.Invalid;
			hotkeyEntryWidget.YieldKeyboardFocus();
		}
	}
}
