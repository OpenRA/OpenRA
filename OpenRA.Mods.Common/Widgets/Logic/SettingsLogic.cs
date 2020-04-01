#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SettingsLogic : ChromeLogic
	{
		enum PanelType { Display, Audio, Input, Hotkeys, Advanced }

		static readonly int OriginalVideoDisplay;
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
		readonly WorldViewportSizes viewportSizes;
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
			OriginalVideoDisplay = original.Graphics.VideoDisplay;
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
			viewportSizes = modData.Manifest.Get<WorldViewportSizes>();

			panelContainer = widget.Get("SETTINGS_PANEL");
			tabContainer = widget.Get("TAB_CONTAINER");

			var panelNames = new Dictionary<PanelType, string>()
			{
				{ PanelType.Display, "Display" },
				{ PanelType.Audio, "Audio" },
				{ PanelType.Input, "Input" },
				{ PanelType.Hotkeys, "Hotkeys" },
				{ PanelType.Advanced, "Advanced" },
			};

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
					current.Graphics.VideoDisplay != OriginalVideoDisplay ||
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
				Action reset = () =>
				{
					resetPanelActions[settingsPanel]();
					Game.Settings.Save();
				};

				ConfirmationDialogs.ButtonPrompt(
					title: "Reset \"{0}\"".F(panelNames[settingsPanel]),
					text: "Are you sure you want to reset\nall settings in this panel?",
					onConfirm: reset,
					onCancel: () => { },
					confirmText: "Reset",
					cancelText: "Cancel");
			};
		}

		public static void BindCheckboxPref(Widget parent, string id, object group, string pref)
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

		static void BindIntSliderPref(Widget parent, string id, object group, string pref)
		{
			var field = group.GetType().GetField(pref);
			if (field == null)
				throw new InvalidOperationException("{0} does not contain a preference type {1}".F(group.GetType().Name, pref));

			var ss = parent.Get<SliderWidget>(id);
			ss.Value = (float)(int)field.GetValue(group);
			ss.OnChange += x => field.SetValue(group, (int)x);
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

		public static readonly Dictionary<WorldViewport, string> ViewportSizeNames = new Dictionary<WorldViewport, string>()
		{
			{ WorldViewport.Close, "Close" },
			{ WorldViewport.Medium, "Medium" },
			{ WorldViewport.Far, "Far" },
			{ WorldViewport.Native, "Furthest" }
		};

		Action InitDisplayPanel(Widget panel)
		{
			var ds = Game.Settings.Graphics;
			var gs = Game.Settings.Game;

			BindCheckboxPref(panel, "CURSORDOUBLE_CHECKBOX", ds, "CursorDouble");
			BindCheckboxPref(panel, "VSYNC_CHECKBOX", ds, "VSync");
			BindCheckboxPref(panel, "FRAME_LIMIT_CHECKBOX", ds, "CapFramerate");
			BindIntSliderPref(panel, "FRAME_LIMIT_SLIDER", ds, "MaxFramerate");
			BindCheckboxPref(panel, "PLAYER_STANCE_COLORS_CHECKBOX", gs, "UsePlayerStanceColors");

			var languageDropDownButton = panel.GetOrNull<DropDownButtonWidget>("LANGUAGE_DROPDOWNBUTTON");
			if (languageDropDownButton != null)
			{
				languageDropDownButton.OnMouseDown = _ => ShowLanguageDropdown(languageDropDownButton, modData.Languages);
				languageDropDownButton.GetText = () => FieldLoader.Translate(ds.Language);
			}

			var windowModeDropdown = panel.Get<DropDownButtonWidget>("MODE_DROPDOWN");
			windowModeDropdown.OnMouseDown = _ => ShowWindowModeDropdown(windowModeDropdown, ds);
			windowModeDropdown.GetText = () => ds.Mode == WindowMode.Windowed ?
				"Windowed" : ds.Mode == WindowMode.Fullscreen ? "Fullscreen (Legacy)" : "Fullscreen";

			var modeChangesDesc = panel.Get("MODE_CHANGES_DESC");
			modeChangesDesc.IsVisible = () => ds.Mode != WindowMode.Windowed && (ds.Mode != OriginalGraphicsMode ||
				ds.VideoDisplay != OriginalVideoDisplay);

			var displaySelectionDropDown = panel.Get<DropDownButtonWidget>("DISPLAY_SELECTION_DROPDOWN");
			displaySelectionDropDown.OnMouseDown = _ => ShowDisplaySelectionDropdown(displaySelectionDropDown, ds);
			var displaySelectionLabel = new CachedTransform<int, string>(i => "Display {0}".F(i + 1));
			displaySelectionDropDown.GetText = () => displaySelectionLabel.Update(ds.VideoDisplay);
			displaySelectionDropDown.IsDisabled = () => Game.Renderer.DisplayCount < 2;

			var statusBarsDropDown = panel.Get<DropDownButtonWidget>("STATUS_BAR_DROPDOWN");
			statusBarsDropDown.OnMouseDown = _ => ShowStatusBarsDropdown(statusBarsDropDown, gs);
			statusBarsDropDown.GetText = () => gs.StatusBars == StatusBarsType.Standard ?
				"Standard" : gs.StatusBars == StatusBarsType.DamageShow ? "Show On Damage" : "Always Show";

			var targetLinesDropDown = panel.Get<DropDownButtonWidget>("TARGET_LINES_DROPDOWN");
			targetLinesDropDown.OnMouseDown = _ => ShowTargetLinesDropdown(targetLinesDropDown, gs);
			targetLinesDropDown.GetText = () => gs.TargetLines == TargetLinesType.Automatic ?
				"Automatic" : gs.TargetLines == TargetLinesType.Manual ? "Manual" : "Disabled";

			var battlefieldCameraDropDown = panel.Get<DropDownButtonWidget>("BATTLEFIELD_CAMERA_DROPDOWN");
			var battlefieldCameraLabel = new CachedTransform<WorldViewport, string>(vs => ViewportSizeNames[vs]);
			battlefieldCameraDropDown.OnMouseDown = _ => ShowBattlefieldCameraDropdown(battlefieldCameraDropDown, viewportSizes, ds);
			battlefieldCameraDropDown.GetText = () => battlefieldCameraLabel.Update(ds.ViewportDistance);

			// Update vsync immediately
			var vsyncCheckbox = panel.Get<CheckboxWidget>("VSYNC_CHECKBOX");
			var vsyncOnClick = vsyncCheckbox.OnClick;
			vsyncCheckbox.OnClick = () =>
			{
				vsyncOnClick();
				Game.Renderer.SetVSyncEnabled(ds.VSync);
			};

			var uiScaleDropdown = panel.Get<DropDownButtonWidget>("UI_SCALE_DROPDOWN");
			var uiScaleLabel = new CachedTransform<float, string>(s => "{0}%".F((int)(100 * s)));
			uiScaleDropdown.OnMouseDown = _ => ShowUIScaleDropdown(uiScaleDropdown, ds);
			uiScaleDropdown.GetText = () => uiScaleLabel.Update(ds.UIScale);

			var minResolution = viewportSizes.MinEffectiveResolution;
			var resolution = Game.Renderer.Resolution;
			var disableUIScale = worldRenderer.World.Type != WorldType.Shellmap ||
				resolution.Width * ds.UIScale < 1.25f * minResolution.Width ||
				resolution.Height * ds.UIScale < 1.25f * minResolution.Height;

			uiScaleDropdown.IsDisabled = () => disableUIScale;

			panel.Get("DISPLAY_SELECTION").IsVisible = () => ds.Mode != WindowMode.Windowed;
			panel.Get("WINDOW_RESOLUTION").IsVisible = () => ds.Mode == WindowMode.Windowed;
			var windowWidth = panel.Get<TextFieldWidget>("WINDOW_WIDTH");
			var origWidthText = windowWidth.Text = ds.WindowedSize.X.ToString();

			var windowHeight = panel.Get<TextFieldWidget>("WINDOW_HEIGHT");
			var origHeightText = windowHeight.Text = ds.WindowedSize.Y.ToString();

			var windowChangesDesc = panel.Get("WINDOW_CHANGES_DESC");
			windowChangesDesc.IsVisible = () => ds.Mode == WindowMode.Windowed &&
				(ds.Mode != OriginalGraphicsMode || origWidthText != windowWidth.Text || origHeightText != windowHeight.Text);

			var frameLimitCheckbox = panel.Get<CheckboxWidget>("FRAME_LIMIT_CHECKBOX");
			var frameLimitOrigLabel = frameLimitCheckbox.Text;
			var frameLimitLabel = new CachedTransform<int, string>(fps => frameLimitOrigLabel + " ({0} FPS)".F(fps));
			frameLimitCheckbox.GetText = () => frameLimitLabel.Update(ds.MaxFramerate);

			// Player profile
			var ps = Game.Settings.Player;

			var escPressed = false;
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
				ds.VideoDisplay = dds.VideoDisplay;
				ds.WindowedSize = dds.WindowedSize;
				ds.CursorDouble = dds.CursorDouble;
				ds.ViewportDistance = dds.ViewportDistance;

				if (ds.UIScale != dds.UIScale)
				{
					var oldScale = ds.UIScale;
					ds.UIScale = dds.UIScale;
					Game.Renderer.SetUIScale(dds.UIScale);
					RecalculateWidgetLayout(Ui.Root);
					Viewport.LastMousePos = (Viewport.LastMousePos.ToFloat2() * oldScale / ds.UIScale).ToInt2();
				}

				ps.Color = dps.Color;
				ps.Name = dps.Name;
			};
		}

		Action InitAudioPanel(Widget panel)
		{
			var musicPlaylist = worldRenderer.World.WorldActor.Trait<MusicPlaylist>();
			var ss = Game.Settings.Sound;

			BindCheckboxPref(panel, "CASH_TICKS", ss, "CashTicks");
			BindCheckboxPref(panel, "MUTE_SOUND", ss, "Mute");
			BindCheckboxPref(panel, "MUTE_BACKGROUND_MUSIC", ss, "MuteBackgroundMusic");

			BindSliderPref(panel, "SOUND_VOLUME", ss, "SoundVolume");
			BindSliderPref(panel, "MUSIC_VOLUME", ss, "MusicVolume");
			BindSliderPref(panel, "VIDEO_VOLUME", ss, "VideoVolume");

			var muteCheckbox = panel.Get<CheckboxWidget>("MUTE_SOUND");
			var muteCheckboxOnClick = muteCheckbox.OnClick;
			var muteCheckboxIsChecked = muteCheckbox.IsChecked;
			muteCheckbox.IsChecked = () => muteCheckboxIsChecked() || Game.Sound.DummyEngine;
			muteCheckbox.IsDisabled = () => Game.Sound.DummyEngine;
			muteCheckbox.OnClick = () =>
			{
				muteCheckboxOnClick();

				if (ss.Mute)
					Game.Sound.MuteAudio();
				else
					Game.Sound.UnmuteAudio();
			};

			var muteBackgroundMusicCheckbox = panel.Get<CheckboxWidget>("MUTE_BACKGROUND_MUSIC");
			var muteBackgroundMusicCheckboxOnClick = muteBackgroundMusicCheckbox.OnClick;
			muteBackgroundMusicCheckbox.OnClick = () =>
			{
				muteBackgroundMusicCheckboxOnClick();

				if (!musicPlaylist.AllowMuteBackgroundMusic)
					return;

				if (musicPlaylist.CurrentSongIsBackground)
					musicPlaylist.Stop();
			};

			// Replace controls with a warning label if sound is disabled
			var noDeviceLabel = panel.GetOrNull("NO_AUDIO_DEVICE");
			if (noDeviceLabel != null)
				noDeviceLabel.Visible = Game.Sound.DummyEngine;

			var controlsContainer = panel.GetOrNull("AUDIO_CONTROLS");
			if (controlsContainer != null)
				controlsContainer.Visible = !Game.Sound.DummyEngine;

			var soundVolumeSlider = panel.Get<SliderWidget>("SOUND_VOLUME");
			soundVolumeSlider.OnChange += x => Game.Sound.SoundVolume = x;

			var musicVolumeSlider = panel.Get<SliderWidget>("MUSIC_VOLUME");
			musicVolumeSlider.OnChange += x => Game.Sound.MusicVolume = x;

			var videoVolumeSlider = panel.Get<SliderWidget>("VIDEO_VOLUME");
			videoVolumeSlider.OnChange += x => Game.Sound.VideoVolume = x;

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
				ss.MuteBackgroundMusic = dss.MuteBackgroundMusic;
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

			BindCheckboxPref(panel, "ALTERNATE_SCROLL_CHECKBOX", gs, "UseAlternateScrollButton");
			BindCheckboxPref(panel, "EDGESCROLL_CHECKBOX", gs, "ViewportEdgeScroll");
			BindCheckboxPref(panel, "LOCKMOUSE_CHECKBOX", gs, "LockMouseWindow");
			BindSliderPref(panel, "ZOOMSPEED_SLIDER", gs, "ZoomSpeed");
			BindSliderPref(panel, "SCROLLSPEED_SLIDER", gs, "ViewportEdgeScrollStep");
			BindSliderPref(panel, "UI_SCROLLSPEED_SLIDER", gs, "UIScrollSpeed");

			var mouseControlDropdown = panel.Get<DropDownButtonWidget>("MOUSE_CONTROL_DROPDOWN");
			mouseControlDropdown.OnMouseDown = _ => ShowMouseControlDropdown(mouseControlDropdown, gs);
			mouseControlDropdown.GetText = () => gs.UseClassicMouseStyle ? "Classic" : "Modern";

			var mouseScrollDropdown = panel.Get<DropDownButtonWidget>("MOUSE_SCROLL_TYPE_DROPDOWN");
			mouseScrollDropdown.OnMouseDown = _ => ShowMouseScrollDropdown(mouseScrollDropdown, gs);
			mouseScrollDropdown.GetText = () => gs.MouseScroll.ToString();

			var mouseControlDescClassic = panel.Get("MOUSE_CONTROL_DESC_CLASSIC");
			mouseControlDescClassic.IsVisible = () => gs.UseClassicMouseStyle;

			var mouseControlDescModern = panel.Get("MOUSE_CONTROL_DESC_MODERN");
			mouseControlDescModern.IsVisible = () => !gs.UseClassicMouseStyle;

			foreach (var container in new[] { mouseControlDescClassic, mouseControlDescModern })
			{
				var classicScrollRight = container.Get("DESC_SCROLL_RIGHT");
				classicScrollRight.IsVisible = () => gs.UseClassicMouseStyle ^ gs.UseAlternateScrollButton;

				var classicScrollMiddle = container.Get("DESC_SCROLL_MIDDLE");
				classicScrollMiddle.IsVisible = () => !gs.UseClassicMouseStyle ^ gs.UseAlternateScrollButton;

				var zoomDesc = container.Get("DESC_ZOOM");
				zoomDesc.IsVisible = () => gs.ZoomModifier == Modifiers.None;

				var zoomDescModifier = container.Get<LabelWidget>("DESC_ZOOM_MODIFIER");
				zoomDescModifier.IsVisible = () => gs.ZoomModifier != Modifiers.None;

				var zoomDescModifierTemplate = zoomDescModifier.Text;
				var zoomDescModifierLabel = new CachedTransform<Modifiers, string>(
					mod => zoomDescModifierTemplate.Replace("MODIFIER", mod.ToString()));
				zoomDescModifier.GetText = () => zoomDescModifierLabel.Update(gs.ZoomModifier);

				var edgescrollDesc = container.Get<LabelWidget>("DESC_EDGESCROLL");
				edgescrollDesc.IsVisible = () => gs.ViewportEdgeScroll;
			}

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
				gs.MouseScroll = dgs.MouseScroll;
				gs.UseAlternateScrollButton = dgs.UseAlternateScrollButton;
				gs.LockMouseWindow = dgs.LockMouseWindow;
				gs.ViewportEdgeScroll = dgs.ViewportEdgeScroll;
				gs.ViewportEdgeScrollStep = dgs.ViewportEdgeScrollStep;
				gs.ZoomSpeed = dgs.ZoomSpeed;
				gs.UIScrollSpeed = dgs.UIScrollSpeed;
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
			BindCheckboxPref(panel, "NETTEXT_CHECKBOX", ds, "NetText");
			BindCheckboxPref(panel, "NETGRAPH_CHECKBOX", ds, "NetGraph");
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
				ds.NetText = dds.NetText;
				ds.NetGraph = dds.NetGraph;
				ds.SyncCheckUnsyncedCode = dds.SyncCheckUnsyncedCode;
				ds.SyncCheckBotModuleCode = dds.SyncCheckBotModuleCode;
				ds.BotDebug = dds.BotDebug;
				ds.LuaDebug = dds.LuaDebug;
				ds.SendSystemInformation = dds.SendSystemInformation;
				ds.CheckVersion = dds.CheckVersion;
				ds.EnableDebugCommandsInReplays = dds.EnableDebugCommandsInReplays;
			};
		}

		public static void ShowMouseControlDropdown(DropDownButtonWidget dropdown, GameSettings s)
		{
			var options = new Dictionary<string, bool>()
			{
				{ "Classic", true },
				{ "Modern", false },
			};

			Func<string, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.UseClassicMouseStyle == options[o],
					() => s.UseClassicMouseStyle = options[o]);
				item.Get<LabelWidget>("LABEL").GetText = () => o;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, setupItem);
		}

		static void ShowMouseScrollDropdown(DropDownButtonWidget dropdown, GameSettings s)
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
					() => s.MouseScroll == options[o],
					() => s.MouseScroll = options[o]);
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

		static void ShowDisplaySelectionDropdown(DropDownButtonWidget dropdown, GraphicSettings s)
		{
			Func<int, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => s.VideoDisplay == o,
					() => s.VideoDisplay = o);

				var label = "Display {0}".F(o + 1);
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			};

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, Enumerable.Range(0, Game.Renderer.DisplayCount), setupItem);
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

		public static void ShowBattlefieldCameraDropdown(DropDownButtonWidget dropdown, WorldViewportSizes viewportSizes, GraphicSettings gs)
		{
			Func<WorldViewport, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gs.ViewportDistance == o,
					() => gs.ViewportDistance = o);

				var label = ViewportSizeNames[o];
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			};

			var windowHeight = Game.Renderer.NativeResolution.Height;

			var validSizes = new List<WorldViewport>() { WorldViewport.Close };
			if (viewportSizes.GetSizeRange(WorldViewport.Medium).X < windowHeight)
				validSizes.Add(WorldViewport.Medium);

			var farRange = viewportSizes.GetSizeRange(WorldViewport.Far);
			if (farRange.X < windowHeight)
				validSizes.Add(WorldViewport.Far);

			if (farRange.Y < windowHeight)
				validSizes.Add(WorldViewport.Native);

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, validSizes, setupItem);
		}

		static void RecalculateWidgetLayout(Widget w, bool insideScrollPanel = false)
		{
			// HACK: Recalculate the widget bounds to fit within the new effective window bounds
			// This is fragile, and only works when called when Settings is opened via the main menu.

			// HACK: Skip children badges container on the main menu
			// This has a fixed size, with calculated size and children positions that break if we adjust them here
			if (w.Id == "BADGES_CONTAINER")
				return;

			var parentBounds = w.Parent == null
				? new Rectangle(0, 0, Game.Renderer.Resolution.Width, Game.Renderer.Resolution.Height)
				: w.Parent.Bounds;

			var substitutions = new Dictionary<string, int>();
			substitutions.Add("WINDOW_RIGHT", Game.Renderer.Resolution.Width);
			substitutions.Add("WINDOW_BOTTOM", Game.Renderer.Resolution.Height);
			substitutions.Add("PARENT_RIGHT", parentBounds.Width);
			substitutions.Add("PARENT_LEFT", parentBounds.Left);
			substitutions.Add("PARENT_TOP", parentBounds.Top);
			substitutions.Add("PARENT_BOTTOM", parentBounds.Height);

			var width = Evaluator.Evaluate(w.Width, substitutions);
			var height = Evaluator.Evaluate(w.Height, substitutions);

			substitutions.Add("WIDTH", width);
			substitutions.Add("HEIGHT", height);

			if (insideScrollPanel)
				w.Bounds = new Rectangle(w.Bounds.X, w.Bounds.Y, width, w.Bounds.Height);
			else
				w.Bounds = new Rectangle(Evaluator.Evaluate(w.X, substitutions),
									   Evaluator.Evaluate(w.Y, substitutions),
									   width,
									   height);

			foreach (var c in w.Children)
				RecalculateWidgetLayout(c, insideScrollPanel || w is ScrollPanelWidget);
		}

		public static void ShowUIScaleDropdown(DropDownButtonWidget dropdown, GraphicSettings gs)
		{
			Func<float, ScrollItemWidget, ScrollItemWidget> setupItem = (o, itemTemplate) =>
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => gs.UIScale == o,
					() =>
					{
						Game.RunAfterTick(() =>
						{
							var oldScale = gs.UIScale;
							gs.UIScale = o;

							Game.Renderer.SetUIScale(o);
							RecalculateWidgetLayout(Ui.Root);
							Viewport.LastMousePos = (Viewport.LastMousePos.ToFloat2() * oldScale / gs.UIScale).ToInt2();
						});
					});

				var label = "{0}%".F((int)(100 * o));
				item.Get<LabelWidget>("LABEL").GetText = () => label;
				return item;
			};

			var viewportSizes = Game.ModData.Manifest.Get<WorldViewportSizes>();
			var maxScales = new float2(Game.Renderer.NativeResolution) / new float2(viewportSizes.MinEffectiveResolution);
			var maxScale = Math.Min(maxScales.X, maxScales.Y);

			var validScales = new[] { 1f, 1.25f, 1.5f, 1.75f, 2f }.Where(x => x <= maxScale);
			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, validScales, setupItem);
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
