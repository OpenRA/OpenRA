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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class AudioSettingsLogic : ChromeLogic
	{
		static readonly string OriginalSoundDevice;

		readonly WorldRenderer worldRenderer;

		SoundDevice soundDevice;

		static AudioSettingsLogic()
		{
			var original = Game.Settings;
			OriginalSoundDevice = original.Sound.Device;
		}

		[ObjectCreator.UseCtor]
		public AudioSettingsLogic(Action<string, string, Func<Widget, Func<bool>>, Func<Widget, Action>> registerPanel, string panelID, string label, WorldRenderer worldRenderer)
		{
			this.worldRenderer = worldRenderer;

			registerPanel(panelID, label, InitPanel, ResetPanel);
		}

		Func<bool> InitPanel(Widget panel)
		{
			var musicPlaylist = worldRenderer.World.WorldActor.Trait<MusicPlaylist>();
			var ss = Game.Settings.Sound;
			var scrollPanel = panel.Get<ScrollPanelWidget>("SETTINGS_SCROLLPANEL");

			SettingsUtils.BindCheckboxPref(panel, "CASH_TICKS", ss, "CashTicks");
			SettingsUtils.BindCheckboxPref(panel, "MUTE_SOUND", ss, "Mute");
			SettingsUtils.BindCheckboxPref(panel, "MUTE_BACKGROUND_MUSIC", ss, "MuteBackgroundMusic");

			SettingsUtils.BindSliderPref(panel, "SOUND_VOLUME", ss, "SoundVolume");
			SettingsUtils.BindSliderPref(panel, "MUSIC_VOLUME", ss, "MusicVolume");
			SettingsUtils.BindSliderPref(panel, "VIDEO_VOLUME", ss, "VideoVolume");

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
			var noDeviceLabel = panel.GetOrNull("NO_AUDIO_DEVICE_CONTAINER");
			if (noDeviceLabel != null)
				noDeviceLabel.Visible = Game.Sound.DummyEngine;

			panel.Get("CASH_TICKS_CONTAINER").Visible = !Game.Sound.DummyEngine;
			panel.Get("MUTE_SOUND_CONTAINER").Visible = !Game.Sound.DummyEngine;
			panel.Get("MUTE_BACKGROUND_MUSIC_CONTAINER").Visible = !Game.Sound.DummyEngine;
			panel.Get("SOUND_VOLUME_CONTAINER").Visible = !Game.Sound.DummyEngine;
			panel.Get("MUSIC_VOLUME_CONTAINER").Visible = !Game.Sound.DummyEngine;
			panel.Get("VIDEO_VOLUME_CONTAINER").Visible = !Game.Sound.DummyEngine;

			var soundVolumeSlider = panel.Get<SliderWidget>("SOUND_VOLUME");
			soundVolumeSlider.OnChange += x => Game.Sound.SoundVolume = x;

			var musicVolumeSlider = panel.Get<SliderWidget>("MUSIC_VOLUME");
			musicVolumeSlider.OnChange += x => Game.Sound.MusicVolume = x;

			var videoVolumeSlider = panel.Get<SliderWidget>("VIDEO_VOLUME");
			videoVolumeSlider.OnChange += x => Game.Sound.VideoVolume = x;

			var devices = Game.Sound.AvailableDevices();
			soundDevice = devices.FirstOrDefault(d => d.Device == ss.Device) ?? devices.First();

			var audioDeviceDropdown = panel.Get<DropDownButtonWidget>("AUDIO_DEVICE");
			audioDeviceDropdown.OnMouseDown = _ => ShowAudioDeviceDropdown(audioDeviceDropdown, devices, scrollPanel);

			var deviceFont = Game.Renderer.Fonts[audioDeviceDropdown.Font];
			var deviceLabel = new CachedTransform<SoundDevice, string>(
				s => WidgetUtils.TruncateText(s.Label, audioDeviceDropdown.UsableWidth, deviceFont));
			audioDeviceDropdown.GetText = () => deviceLabel.Update(soundDevice);

			var restartDesc = panel.Get("RESTART_REQUIRED_DESC");
			restartDesc.IsVisible = () => soundDevice.Device != OriginalSoundDevice;

			SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);

			return () =>
			{
				ss.Device = soundDevice.Device;

				return ss.Device != OriginalSoundDevice;
			};
		}

		Action ResetPanel(Widget panel)
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

		void ShowAudioDeviceDropdown(DropDownButtonWidget dropdown, SoundDevice[] devices, ScrollPanelWidget scrollPanel)
		{
			var i = 0;
			var options = devices.ToDictionary(d => i++.ToString(), d => d);

			ScrollItemWidget SetupItem(string o, ScrollItemWidget itemTemplate)
			{
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => soundDevice == options[o],
					() =>
					{
						soundDevice = options[o];
						SettingsUtils.AdjustSettingsScrollPanelLayout(scrollPanel);
					});

				var deviceLabel = item.Get<LabelWidget>("LABEL");
				var font = Game.Renderer.Fonts[deviceLabel.Font];
				var label = WidgetUtils.TruncateText(options[o].Label, deviceLabel.Bounds.Width, font);
				deviceLabel.GetText = () => label;
				return item;
			}

			dropdown.ShowDropDown("LABEL_DROPDOWN_TEMPLATE", 500, options.Keys, SetupItem);
		}
	}
}
