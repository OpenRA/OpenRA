#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class MusicPlayerLogic
	{
		string CurrentSong = null;
		public MusicPlayerLogic()
		{
			var bg = Widget.RootWidget.GetWidget("MUSIC_MENU");
			CurrentSong = GetNextSong();

			bg.GetWidget<ButtonWidget>("BUTTON_CLOSE").OnClick =
				() => { Game.Settings.Save(); Widget.CloseWindow(); };
			
			bg.GetWidget("BUTTON_INSTALL").IsVisible = () => false;
			
			bg.GetWidget<ButtonWidget>("BUTTON_PLAY").OnClick = () =>
			{
				if (CurrentSong == null)
					return;
				
				Sound.PlayMusicThen(Rules.Music[CurrentSong],
				      () => bg.GetWidget<ButtonWidget>(Game.Settings.Sound.Repeat ? "BUTTON_PLAY" : "BUTTON_NEXT")
				              .OnClick());
				bg.GetWidget("BUTTON_PLAY").Visible = false;
				bg.GetWidget("BUTTON_PAUSE").Visible = true;
			};
			
			bg.GetWidget<ButtonWidget>("BUTTON_PAUSE").OnClick = () =>
			{				
				Sound.PauseMusic();
				bg.GetWidget("BUTTON_PAUSE").Visible = false;
				bg.GetWidget("BUTTON_PLAY").Visible = true;
			};
			
			bg.GetWidget<ButtonWidget>("BUTTON_STOP").OnClick = () =>
			{
				Sound.StopMusic();
				bg.GetWidget("BUTTON_PAUSE").Visible = false;
				bg.GetWidget("BUTTON_PLAY").Visible = true;
			};
			
			bg.GetWidget<ButtonWidget>("BUTTON_NEXT").OnClick = () =>
			{
				CurrentSong = GetNextSong();
				bg.GetWidget<ButtonWidget>("BUTTON_PLAY").OnClick();
			};

			bg.GetWidget<ButtonWidget>("BUTTON_PREV").OnClick = () =>
			{
				CurrentSong = GetPrevSong();
				bg.GetWidget<ButtonWidget>("BUTTON_PLAY").OnClick();
			};
			
			
			var shuffleCheckbox = bg.GetWidget<CheckboxWidget>("SHUFFLE");
			shuffleCheckbox.IsChecked = () => Game.Settings.Sound.Shuffle;
			shuffleCheckbox.OnClick = () => Game.Settings.Sound.Shuffle ^= true;
			
			var repeatCheckbox = bg.GetWidget<CheckboxWidget>("REPEAT");
			repeatCheckbox.IsChecked = () => Game.Settings.Sound.Repeat;
			repeatCheckbox.OnClick = () => Game.Settings.Sound.Repeat ^= true;
			
			bg.GetWidget<LabelWidget>("TIME").GetText = () =>
			{
				if (CurrentSong == null)
					return "";
				return "{0:D2}:{1:D2} / {2:D2}:{3:D2}".F((int)Sound.MusicSeekPosition / 60, (int)Sound.MusicSeekPosition % 60,
			                                                                                    Rules.Music[CurrentSong].Length / 60, Rules.Music[CurrentSong].Length % 60);
			};
			
			var ml = bg.GetWidget<ScrollPanelWidget>("MUSIC_LIST");
			var itemTemplate = ml.GetWidget<ScrollItemWidget>("MUSIC_TEMPLATE");
			
			if (!Rules.Music.Where(m => m.Value.Exists).Any())
			{
				itemTemplate.IsVisible = () => true;
				itemTemplate.GetWidget<LabelWidget>("TITLE").GetText = () => "No Music Installed";
				itemTemplate.GetWidget<LabelWidget>("TITLE").Align = LabelWidget.TextAlign.Center;
			}
			
			foreach (var kv in Rules.Music.Where(m => m.Value.Exists))
			{
				var song = kv.Key;
				if (CurrentSong == null)
					CurrentSong = song;
				
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => CurrentSong == song,
					() => { CurrentSong = song; bg.GetWidget<ButtonWidget>("BUTTON_PLAY").OnClick(); });
				item.GetWidget<LabelWidget>("TITLE").GetText = () => Rules.Music[song].Title;
				item.GetWidget<LabelWidget>("LENGTH").GetText = () => "{0:D1}:{1:D2}".F(Rules.Music[song].Length / 60, Rules.Music[song].Length % 60);
				ml.AddChild(item);
			}
		}
		
		string GetNextSong()
		{
			var songs = Rules.Music.Where(a => a.Value.Exists)
				.Select(a => a.Key);
			
			if (!songs.Any())
				return null;
			
			if (Game.Settings.Sound.Shuffle)
				return songs.Random(Game.CosmeticRandom);
			
			return songs.SkipWhile(m => m != CurrentSong)
				.Skip(1).FirstOrDefault() ?? songs.FirstOrDefault();

		}

		string GetPrevSong()
		{
			var songs = Rules.Music.Where(a => a.Value.Exists)
				.Select(a => a.Key).Reverse();
			
			if (!songs.Any())
				return null;
			
			if (Game.Settings.Sound.Shuffle)
				return songs.Random(Game.CosmeticRandom);
			
			return songs.SkipWhile(m => m != CurrentSong)
				.Skip(1).FirstOrDefault() ?? songs.FirstOrDefault();
		}
	}
}
