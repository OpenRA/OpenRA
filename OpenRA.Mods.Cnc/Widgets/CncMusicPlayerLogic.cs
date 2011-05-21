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
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.FileFormats.Graphics;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets
{
	public class CncMusicPlayerLogic : IWidgetDelegate
	{
		bool installed;
		MusicInfo currentSong = null;
		Widget panel;
		MusicInfo[] music;
		MusicInfo[] random;
		
		ScrollItemWidget itemTemplate;

		[ObjectCreator.UseCtor]
		public CncMusicPlayerLogic([ObjectCreator.Param] Widget widget,
		                           [ObjectCreator.Param] Action onExit)
		{
			panel = widget.GetWidget("MUSIC_PANEL");
			
			var ml = panel.GetWidget<ScrollPanelWidget>("MUSIC_LIST");
			itemTemplate = ml.GetWidget<ScrollItemWidget>("MUSIC_TEMPLATE");
			
			BuildMusicTable(ml);

			currentSong = Sound.CurrentMusic ?? GetNextSong();
			installed = Rules.Music.Where(m => m.Value.Exists).Any();
			Func<bool> noMusic = () => !installed;
			
			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => { Widget.CloseWindow(); onExit(); };
			
			Action<string> afterInstall = path =>
			{
				// Mount the new mixfile and rebuild the scores list
				try
				{
					FileSystem.Mount(path);
					Rules.Music.Do(m => m.Value.Reload());
				}
				catch (Exception) { }
				
				installed = Rules.Music.Where(m => m.Value.Exists).Any();
				BuildMusicTable(ml);
			};
			
			var installButton = panel.GetWidget<ButtonWidget>("INSTALL_BUTTON");
			installButton.OnClick = () =>
				Widget.OpenWindow("INSTALL_MUSIC_PANEL", new WidgetArgs() {{ "afterInstall", afterInstall }});
			installButton.IsVisible = () => music.Length < 2; // Hack around ra shipping (only) hellmarch by default
			
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
			musicSlider.OnChange += x => { Sound.MusicVolume = x; };
			musicSlider.GetOffset = () => { return Sound.MusicVolume; };
			musicSlider.SetOffset(Sound.MusicVolume);
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
	
	
	public class CncInstallMusicLogic : IWidgetDelegate
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action<string> afterInstall;
		
		[ObjectCreator.UseCtor]
		public CncInstallMusicLogic([ObjectCreator.Param] Widget widget,
		                       [ObjectCreator.Param] Action<string> afterInstall)
		{
			this.afterInstall = afterInstall;
			panel = widget.GetWidget("INSTALL_MUSIC_PANEL");
			progressBar = panel.GetWidget<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.GetWidget<LabelWidget>("STATUS_LABEL");
			
			var backButton = panel.GetWidget<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Widget.CloseWindow;
			backButton.IsVisible = () => false;
			
			var retryButton = panel.GetWidget<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = PromptForCD;
			retryButton.IsVisible = () => false;
			
			// TODO: Search obvious places (platform dependent) for CD
			PromptForCD();
		}
		
		void PromptForCD()
		{
			if (Game.Settings.Graphics.Mode == WindowMode.Fullscreen)
			{
				statusLabel.GetText = () => "Error: Installing from Fullscreen mode is not supported";
				panel.GetWidget("BACK_BUTTON").IsVisible = () => true;
				return;
			}

			progressBar.SetIndeterminate(true);
			statusLabel.GetText = () => "Waiting for file";
			Game.Utilities.PromptFilepathAsync("Select SCORES.MIX on the C&C CD", path => Install(path));
		}
		
		public void OnError(string message)
		{
			Game.RunAfterTick(() => 
			{
				progressBar.SetIndeterminate(false);
				statusLabel.GetText = () => "Error: "+message;
				panel.GetWidget("RETRY_BUTTON").IsVisible = () => true;
				panel.GetWidget("BACK_BUTTON").IsVisible = () => true;
			});
		}

		void Install(string path)
		{
			var dest = new string[] { Platform.SupportDir, "Content", "cnc" }.Aggregate(Path.Combine);
			Game.RunAfterTick(() => statusLabel.GetText = () => "Installing");

			// Mount the package and check that it contains the correct files
			try
			{
				var mixFile = new MixFile(path, 0);
				
				if (!mixFile.Exists("aoi.aud"))
				{
					OnError("Not the C&C SCORES.MIX");
					return;
				}
				
				var t = new Thread( _ =>
				{
					var destPath = Path.Combine(dest, "scores.mix");
					try
					{
						File.Copy(path, destPath, true);
						Game.RunAfterTick(() =>
						{
							Widget.CloseWindow(); // Progress panel
							afterInstall(destPath);
						});
					}
					catch (Exception e)
					{
						OnError("File copy failed");
						Log.Write("debug", e.Message);
					}
				}) { IsBackground = true };
				t.Start();
			}
			catch
			{
				OnError("Invalid mix file");
			}
		}
	}
}
