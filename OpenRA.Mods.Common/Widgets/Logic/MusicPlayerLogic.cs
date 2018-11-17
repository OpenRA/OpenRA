#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MusicPlayerLogic : ChromeLogic
	{
		readonly ScrollPanelWidget musicList;
		readonly ScrollItemWidget itemTemplate;

		readonly MusicPlaylist musicPlaylist;
		MusicInfo currentSong = null;

		[ObjectCreator.UseCtor]
		public MusicPlayerLogic(Widget widget, ModData modData, World world, Action onExit)
		{
			var panel = widget;

			musicList = panel.Get<ScrollPanelWidget>("MUSIC_LIST");
			itemTemplate = musicList.Get<ScrollItemWidget>("MUSIC_TEMPLATE");
			musicPlaylist = world.WorldActor.Trait<MusicPlaylist>();

			BuildMusicTable();

			Func<bool> noMusic = () => !musicPlaylist.IsMusicAvailable || musicPlaylist.CurrentSongIsBackground || currentSong == null;
			panel.Get("NO_MUSIC_LABEL").IsVisible = () => !musicPlaylist.IsMusicAvailable;

			if (musicPlaylist.IsMusicAvailable)
			{
				panel.Get<LabelWidget>("MUTE_LABEL").GetText = () =>
				{
					if (Game.Settings.Sound.Mute)
						return "Audio has been muted in settings.";

					return "";
				};
			}

			var playButton = panel.Get<ButtonWidget>("BUTTON_PLAY");
			playButton.OnClick = Play;
			playButton.IsDisabled = noMusic;
			playButton.IsVisible = () => !Game.Sound.MusicPlaying;

			var pauseButton = panel.Get<ButtonWidget>("BUTTON_PAUSE");
			pauseButton.OnClick = Game.Sound.PauseMusic;
			pauseButton.IsDisabled = noMusic;
			pauseButton.IsVisible = () => Game.Sound.MusicPlaying;

			var stopButton = panel.Get<ButtonWidget>("BUTTON_STOP");
			stopButton.OnClick = () => { musicPlaylist.Stop(); };
			stopButton.IsDisabled = noMusic;

			var nextButton = panel.Get<ButtonWidget>("BUTTON_NEXT");
			nextButton.OnClick = () => { currentSong = musicPlaylist.GetNextSong(); Play(); };
			nextButton.IsDisabled = noMusic;

			var prevButton = panel.Get<ButtonWidget>("BUTTON_PREV");
			prevButton.OnClick = () => { currentSong = musicPlaylist.GetPrevSong(); Play(); };
			prevButton.IsDisabled = noMusic;

			var shuffleCheckbox = panel.Get<CheckboxWidget>("SHUFFLE");
			shuffleCheckbox.IsChecked = () => Game.Settings.Sound.Shuffle;
			shuffleCheckbox.OnClick = () => Game.Settings.Sound.Shuffle ^= true;
			shuffleCheckbox.IsDisabled = () => musicPlaylist.CurrentSongIsBackground;

			var repeatCheckbox = panel.Get<CheckboxWidget>("REPEAT");
			repeatCheckbox.IsChecked = () => Game.Settings.Sound.Repeat;
			repeatCheckbox.OnClick = () => Game.Settings.Sound.Repeat ^= true;
			repeatCheckbox.IsDisabled = () => musicPlaylist.CurrentSongIsBackground;

			panel.Get<LabelWidget>("TIME_LABEL").GetText = () =>
			{
				if (currentSong == null || musicPlaylist.CurrentSongIsBackground)
					return "";

				var seek = Game.Sound.MusicSeekPosition;
				var minutes = (int)seek / 60;
				var seconds = (int)seek % 60;
				var totalMinutes = currentSong.Length / 60;
				var totalSeconds = currentSong.Length % 60;

				return "{0:D2}:{1:D2} / {2:D2}:{3:D2}".F(minutes, seconds, totalMinutes, totalSeconds);
			};

			var musicTitle = panel.GetOrNull<LabelWidget>("TITLE_LABEL");
			if (musicTitle != null)
				musicTitle.GetText = () => currentSong != null ? currentSong.Title : "No song playing";

			var musicSlider = panel.Get<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => Game.Sound.MusicVolume = x;
			musicSlider.Value = Game.Sound.MusicVolume;

			var songWatcher = widget.GetOrNull<LogicTickerWidget>("SONG_WATCHER");
			if (songWatcher != null)
			{
				songWatcher.OnTick = () =>
				{
					if (musicPlaylist.CurrentSongIsBackground && currentSong != null)
						currentSong = null;

					if (Game.Sound.CurrentMusic == null || currentSong == Game.Sound.CurrentMusic || musicPlaylist.CurrentSongIsBackground)
						return;

					currentSong = Game.Sound.CurrentMusic;
				};
			}

			var backButton = panel.GetOrNull<ButtonWidget>("BACK_BUTTON");
			if (backButton != null)
				backButton.OnClick = () => { Game.Settings.Save(); Ui.CloseWindow(); onExit(); };
		}

		public void BuildMusicTable()
		{
			if (!musicPlaylist.IsMusicAvailable)
				return;

			var music = musicPlaylist.AvailablePlaylist();
			currentSong = musicPlaylist.CurrentSong();

			musicList.RemoveChildren();
			foreach (var s in music)
			{
				var song = s;
				var item = ScrollItemWidget.Setup(song.Filename, itemTemplate, () => currentSong == song, () => { currentSong = song; Play(); }, () => { });
				item.Get<LabelWidget>("TITLE").GetText = () => song.Title;
				item.Get<LabelWidget>("LENGTH").GetText = () => SongLengthLabel(song);
				musicList.AddChild(item);
			}

			if (currentSong != null && !musicPlaylist.CurrentSongIsBackground)
				musicList.ScrollToItem(currentSong.Filename);
		}

		void Play()
		{
			if (currentSong == null)
				return;

			musicList.ScrollToItem(currentSong.Filename);
			musicPlaylist.Play(currentSong);
		}

		static string SongLengthLabel(MusicInfo song)
		{
			return "{0:D1}:{1:D2}".F(song.Length / 60, song.Length % 60);
		}
	}
}
