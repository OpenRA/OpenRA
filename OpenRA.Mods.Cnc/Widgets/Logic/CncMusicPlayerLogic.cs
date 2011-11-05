#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncMusicPlayerLogic
	{
		bool installed;
		MusicInfo currentSong = null;
		Widget panel;
		MusicInfo[] music;
		MusicInfo[] random;

		ScrollItemWidget itemTemplate;

		[ObjectCreator.UseCtor]
		public CncMusicPlayerLogic(Widget widget, Action onExit)
		{
			panel = widget.GetWidget("MUSIC_PANEL");

			var ml = panel.GetWidget<ScrollPanelWidget>("MUSIC_LIST");
			itemTemplate = ml.GetWidget<ScrollItemWidget>("MUSIC_TEMPLATE");

			BuildMusicTable(ml);

			currentSong = Sound.CurrentMusic ?? GetNextSong();
			installed = Rules.Music.Where(m => m.Value.Exists).Any();
			Func<bool> noMusic = () => !installed;

			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => { Widget.CloseWindow(); onExit(); };

			Action afterInstall = () =>
			{
				// Mount the new mixfile and rebuild the scores list
				try
				{
					var path = new string[] { Platform.SupportDir, "Content", "cnc" }.Aggregate(Path.Combine);
					FileSystem.Mount(Path.Combine(path, "scores.mix"));
					FileSystem.Mount(Path.Combine(path, "transit.mix"));
					Rules.Music.Do(m => m.Value.Reload());
				}
				catch (Exception) { }

				installed = Rules.Music.Where(m => m.Value.Exists).Any();
				BuildMusicTable(ml);
			};

			var installButton = panel.GetWidget<ButtonWidget>("INSTALL_BUTTON");
			installButton.OnClick = () =>
				Widget.OpenWindow("INSTALL_MUSIC_PANEL", new WidgetArgs() {
					{ "afterInstall", afterInstall },
					{ "filesToCopy", new [] { "SCORES.MIX" } },
					{ "filesToExtract", new [] { "transit.mix" } },
				});
			installButton.IsVisible = () => music.Length < 3; // Hack around music being split between transit.mix and scores.mix

			panel.GetWidget("NO_MUSIC_LABEL").IsVisible = noMusic;

			var playButton = panel.GetWidget<ButtonWidget>("BUTTON_PLAY");
			playButton.OnClick = Play;
			playButton.IsDisabled = noMusic;

			var pauseButton = panel.GetWidget<ButtonWidget>("BUTTON_PAUSE");
			pauseButton.OnClick = Pause;
			pauseButton.IsDisabled = noMusic;

			var stopButton = panel.GetWidget<ButtonWidget>("BUTTON_STOP");
			stopButton.OnClick = Stop;
			stopButton.IsDisabled = noMusic;

			var nextButton = panel.GetWidget<ButtonWidget>("BUTTON_NEXT");
			nextButton.OnClick = () => { currentSong = GetNextSong(); Play(); };
			nextButton.IsDisabled = noMusic;

			var prevButton = panel.GetWidget<ButtonWidget>("BUTTON_PREV");
			prevButton.OnClick = () => { currentSong = GetPrevSong(); Play(); };
			prevButton.IsDisabled = noMusic;

			var shuffleCheckbox = panel.GetWidget<CheckboxWidget>("SHUFFLE");
			shuffleCheckbox.IsChecked = () => Game.Settings.Sound.Shuffle;
			shuffleCheckbox.OnClick = () => Game.Settings.Sound.Shuffle ^= true;

			var repeatCheckbox = panel.GetWidget<CheckboxWidget>("REPEAT");
			repeatCheckbox.IsChecked = () => Game.Settings.Sound.Repeat;
			repeatCheckbox.OnClick = () => Game.Settings.Sound.Repeat ^= true;

			panel.GetWidget<LabelWidget>("TIME_LABEL").GetText = () => (currentSong == null) ? "" :
					"{0:D2}:{1:D2} / {2:D2}:{3:D2}".F((int)Sound.MusicSeekPosition / 60, (int)Sound.MusicSeekPosition % 60,
													  currentSong.Length / 60, currentSong.Length % 60);
			panel.GetWidget<LabelWidget>("TITLE_LABEL").GetText = () => (currentSong == null) ? "" : currentSong.Title;

			var musicSlider = panel.GetWidget<SliderWidget>("MUSIC_SLIDER");
			musicSlider.OnChange += x => Sound.MusicVolume = x;
			musicSlider.Value = Sound.MusicVolume;
		}

		void BuildMusicTable(Widget list)
		{
			music = Rules.Music.Where(a => a.Value.Exists).Select(a => a.Value).ToArray();
			random = music.Shuffle(Game.CosmeticRandom).ToArray();

			list.RemoveChildren();
			foreach (var s in music)
			{
				var song = s;
				if (currentSong == null)
					currentSong = song;

				var item = ScrollItemWidget.Setup(itemTemplate, () => currentSong == song, () => { currentSong = song; Play(); });
				item.GetWidget<LabelWidget>("TITLE").GetText = () => song.Title;
				item.GetWidget<LabelWidget>("LENGTH").GetText = () => SongLengthLabel(song);
				list.AddChild(item);
			}
		}

		void Play()
		{
			if (currentSong == null)
				return;

			Sound.PlayMusicThen(currentSong, () =>
			{
				if (!Game.Settings.Sound.Repeat)
					currentSong = GetNextSong();
				Play();
			});

			panel.GetWidget("BUTTON_PLAY").Visible = false;
			panel.GetWidget("BUTTON_PAUSE").Visible = true;
		}

		void Pause()
		{
			Sound.PauseMusic();
			panel.GetWidget("BUTTON_PAUSE").Visible = false;
			panel.GetWidget("BUTTON_PLAY").Visible = true;
		}

		void Stop()
		{
			Sound.StopMusic();
			panel.GetWidget("BUTTON_PAUSE").Visible = false;
			panel.GetWidget("BUTTON_PLAY").Visible = true;
		}

		string SongLengthLabel(MusicInfo song)
		{
			return "{0:D1}:{1:D2}".F(song.Length / 60, song.Length % 60);
		}

		MusicInfo GetNextSong()
		{
			if (!music.Any())
				return null;

			var songs = Game.Settings.Sound.Shuffle ? random : music;
			return songs.SkipWhile(m => m != currentSong)
				.Skip(1).FirstOrDefault() ?? songs.FirstOrDefault();
		}

		MusicInfo GetPrevSong()
		{
			if (!music.Any())
				return null;

			var songs = Game.Settings.Sound.Shuffle ? random : music;
			return songs.Reverse().SkipWhile(m => m != currentSong)
				.Skip(1).FirstOrDefault() ?? songs.Reverse().FirstOrDefault();
		}
	}
}
