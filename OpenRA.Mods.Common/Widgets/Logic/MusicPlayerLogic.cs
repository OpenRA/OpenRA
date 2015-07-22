#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MusicPlayerLogic
	{
		readonly ScrollPanelWidget musicList;
		readonly ScrollItemWidget itemTemplate;

		readonly MusicPlaylist musicPlaylist;
		MusicInfo currentSong = null;

		[ObjectCreator.UseCtor]
		public MusicPlayerLogic(Widget widget, Ruleset modRules, World world, Action onExit)
		{
			var panel = widget.Get("MUSIC_PANEL");

			musicList = panel.Get<ScrollPanelWidget>("MUSIC_LIST");
			itemTemplate = musicList.Get<ScrollItemWidget>("MUSIC_TEMPLATE");
			musicPlaylist = world.WorldActor.Trait<MusicPlaylist>();

			BuildMusicTable();

			Func<bool> noMusic = () => !musicPlaylist.IsMusicAvailable;
			Func<bool> disableControls = () => !musicPlaylist.IsMusicAvailable || musicPlaylist.AmbientMusic;
			panel.Get("NO_MUSIC_LABEL").IsVisible = noMusic;

			var playButton = panel.Get<ButtonWidget>("BUTTON_PLAY");
			playButton.OnClick = Play;
			playButton.IsDisabled = disableControls;
			playButton.IsVisible = () => !Sound.MusicPlaying;

			var pauseButton = panel.Get<ButtonWidget>("BUTTON_PAUSE");
			pauseButton.OnClick = Sound.PauseMusic;
			pauseButton.IsDisabled = disableControls;
			pauseButton.IsVisible = () => Sound.MusicPlaying;

			var stopButton = panel.Get<ButtonWidget>("BUTTON_STOP");
			stopButton.OnClick = () => { musicPlaylist.Stop(); };
			stopButton.IsDisabled = disableControls;

			var nextButton = panel.Get<ButtonWidget>("BUTTON_NEXT");
			nextButton.OnClick = () => { currentSong = musicPlaylist.GetNextSong(); Play(); };
			nextButton.IsDisabled = disableControls;

			var prevButton = panel.Get<ButtonWidget>("BUTTON_PREV");
			prevButton.OnClick = () => { currentSong = musicPlaylist.GetPrevSong(); Play(); };
			prevButton.IsDisabled = disableControls;

			var shuffleCheckbox = panel.Get<CheckboxWidget>("SHUFFLE");
			shuffleCheckbox.IsChecked = () => Game.Settings.Sound.Shuffle;
			shuffleCheckbox.OnClick = () => Game.Settings.Sound.Shuffle ^= true;

			var repeatCheckbox = panel.Get<CheckboxWidget>("REPEAT");
			repeatCheckbox.IsChecked = () => Game.Settings.Sound.Repeat;
			repeatCheckbox.OnClick = () => Game.Settings.Sound.Repeat ^= true;

			panel.Get<LabelWidget>("TIME_LABEL").GetText = () => (currentSong == null) ? "" :
				"{0:D2}:{1:D2} / {2:D2}:{3:D2}".F((int)Sound.MusicSeekPosition / 60, (int)Sound.MusicSeekPosition % 60,
					currentSong.Length / 60, currentSong.Length % 60);

			var musicSlider = panel.Get<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => Sound.MusicVolume = x;
			musicSlider.Value = Sound.MusicVolume;

			var installButton = widget.GetOrNull<ButtonWidget>("INSTALL_BUTTON");
			if (installButton != null)
			{
				installButton.IsDisabled = () => world == null || world.Type != WorldType.Shellmap;
				var args = new string[] { "Install.Music=true" };
				installButton.OnClick = () =>
					Game.RunAfterTick(() =>
						Game.InitializeMod(Game.Settings.Game.Mod, new Arguments(args)));

				var installData = Game.ModData.Manifest.Get<ContentInstaller>();
				installButton.IsVisible = () => modRules.InstalledMusic.ToArray().Length <= installData.ShippedSoundtracks;
			}

			var songWatcher = widget.GetOrNull<LogicTickerWidget>("SONG_WATCHER");
			if (songWatcher != null)
			{
				songWatcher.OnTick = () =>
				{
					if (!musicPlaylist.CurrentSong().Hidden && currentSong != musicPlaylist.CurrentSong())
						currentSong = musicPlaylist.CurrentSong();
				};
			}

			panel.Get<ButtonWidget>("BACK_BUTTON").OnClick = () => { Game.Settings.Save(); Ui.CloseWindow(); onExit(); };
		}

		public void BuildMusicTable()
		{
			if (!musicPlaylist.IsMusicAvailable)
				return;

			var music = musicPlaylist.AvailablePlaylist();
			currentSong = musicPlaylist.CurrentSong();
			if (currentSong == null && music.Any())
				currentSong = musicPlaylist.GetNextSong();

			musicList.RemoveChildren();
			foreach (var s in music)
			{
				var song = s;
				if (currentSong == null)
					currentSong = song;

				var item = ScrollItemWidget.Setup(song.Filename, itemTemplate, () => currentSong == song, () => { currentSong = song; Play(); }, () => { });
				item.Get<LabelWidget>("TITLE").GetText = () => song.Title;
				item.Get<LabelWidget>("LENGTH").GetText = () => SongLengthLabel(song);
				musicList.AddChild(item);
			}

			if (currentSong != null)
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
