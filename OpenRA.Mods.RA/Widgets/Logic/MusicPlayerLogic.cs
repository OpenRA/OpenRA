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
using System.Collections.Generic;
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
		Widget bg;

		public void Play(string song)
		{
			CurrentSong = song;
			if (CurrentSong == null) return;

			Sound.PlayMusicThen(Rules.Music[CurrentSong],
				() => Play( Game.Settings.Sound.Repeat ? CurrentSong : GetNextSong() ));
		}

		[ObjectCreator.UseCtor]
		public MusicPlayerLogic(Action onExit)
		{
			bg = Ui.Root.Get("MUSIC_MENU");
			CurrentSong = GetNextSong();

			bg.Get( "BUTTON_PAUSE" ).IsVisible = () => Sound.MusicPlaying;
			bg.Get( "BUTTON_PLAY" ).IsVisible = () => !Sound.MusicPlaying;

			bg.Get<ButtonWidget>("BUTTON_CLOSE").OnClick =
				() => { Game.Settings.Save(); Ui.CloseWindow(); onExit(); };

			bg.Get("BUTTON_INSTALL").IsVisible = () => false;

			bg.Get<ButtonWidget>("BUTTON_PLAY").OnClick = () => Play( CurrentSong );
			bg.Get<ButtonWidget>("BUTTON_PAUSE").OnClick = Sound.PauseMusic;
			bg.Get<ButtonWidget>("BUTTON_STOP").OnClick = Sound.StopMusic;
			bg.Get<ButtonWidget>("BUTTON_NEXT").OnClick = () => Play( GetNextSong() );
			bg.Get<ButtonWidget>("BUTTON_PREV").OnClick = () => Play( GetPrevSong() );

			var shuffleCheckbox = bg.Get<CheckboxWidget>("SHUFFLE");
			shuffleCheckbox.IsChecked = () => Game.Settings.Sound.Shuffle;
			shuffleCheckbox.OnClick = () => Game.Settings.Sound.Shuffle ^= true;

			var repeatCheckbox = bg.Get<CheckboxWidget>("REPEAT");
			repeatCheckbox.IsChecked = () => Game.Settings.Sound.Repeat;
			repeatCheckbox.OnClick = () => Game.Settings.Sound.Repeat ^= true;

			bg.Get<LabelWidget>("TIME").GetText = () =>
			{
				if (CurrentSong == null)
					return "";
				return "{0} / {1}".F(
					WidgetUtils.FormatTimeSeconds( (int)Sound.MusicSeekPosition ),
					WidgetUtils.FormatTimeSeconds( Rules.Music[CurrentSong].Length ));
			};

			var ml = bg.Get<ScrollPanelWidget>("MUSIC_LIST");
			var itemTemplate = ml.Get<ScrollItemWidget>("MUSIC_TEMPLATE");

			if (!Rules.InstalledMusic.Any())
			{
				itemTemplate.IsVisible = () => true;
				itemTemplate.Get<LabelWidget>("TITLE").GetText = () => "No Music Installed";
				itemTemplate.Get<LabelWidget>("TITLE").Align = TextAlign.Center;
			}

			foreach (var kv in Rules.InstalledMusic)
			{
				var song = kv.Key;
				var item = ScrollItemWidget.Setup(itemTemplate,
					() => CurrentSong == song,
					() => Play( song ));
				item.Get<LabelWidget>("TITLE").GetText = () => Rules.Music[song].Title;
				item.Get<LabelWidget>("LENGTH").GetText =
					() => WidgetUtils.FormatTimeSeconds( Rules.Music[song].Length );
				ml.AddChild(item);
			}
		}

		string ChooseSong( IEnumerable<string> songs )
		{
			if (!songs.Any())
				return null;

			if (Game.Settings.Sound.Shuffle)
				return songs.Random(Game.CosmeticRandom);

			return songs.SkipWhile(m => m != CurrentSong)
				.Skip(1).FirstOrDefault() ?? songs.FirstOrDefault();
		}

		string GetNextSong() { return ChooseSong( Rules.InstalledMusic.Select( a => a.Key ) ); }
		string GetPrevSong() { return ChooseSong( Rules.InstalledMusic.Select( a => a.Key ).Reverse() ); }
	}
}
