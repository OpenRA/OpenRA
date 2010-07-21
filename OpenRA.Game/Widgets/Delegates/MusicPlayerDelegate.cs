#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

namespace OpenRA.Widgets.Delegates
{
	public class MusicPlayerDelegate : IWidgetDelegate
	{
		public MusicPlayerDelegate()
		{
			var bg = Widget.RootWidget.GetWidget("MUSIC_BG");
			bg.Visible = Game.Settings.MusicPlayer;

			bg.GetWidget("BUTTON_PLAY").OnMouseUp = mi =>
			{
				if (Sound.MusicStopped)
					Sound.PlayMusic(GetSong());
				Sound.MusicStopped = false;
				Sound.MusicPaused = false;
				bg.GetWidget("BUTTON_PLAY").Visible = false;
				bg.GetWidget("BUTTON_PAUSE").Visible = true;
				return true;
			};

			bg.GetWidget("BUTTON_PAUSE").OnMouseUp = mi =>
			{
				Sound.MusicPaused = true;
				bg.GetWidget("BUTTON_PAUSE").Visible = false;
				bg.GetWidget("BUTTON_PLAY").Visible = true;
				return true;
			};

			bg.GetWidget("BUTTON_STOP").OnMouseUp = mi =>
			{
				Sound.MusicStopped = true;
				bg.GetWidget("BUTTON_PAUSE").Visible = false;
				bg.GetWidget("BUTTON_PLAY").Visible = true;
				return true;
			};

			bg.GetWidget("BUTTON_NEXT").OnMouseUp = mi =>
			{
				Sound.PlayMusic(GetNextSong());
				Sound.MusicStopped = false;
				Sound.MusicPaused = false;
				bg.GetWidget("BUTTON_PLAY").Visible = false;
				bg.GetWidget("BUTTON_PAUSE").Visible = true;
				return true;
			};

			bg.GetWidget("BUTTON_PREV").OnMouseUp = mi =>
			{
				Sound.PlayMusic(GetPrevSong());
				Sound.MusicStopped = false;
				Sound.MusicPaused = false;
				bg.GetWidget("BUTTON_PLAY").Visible = false;
				bg.GetWidget("BUTTON_PAUSE").Visible = true;
				return true;
			};
		}

		string GetNextSong()
		{
			if (!Rules.Music.ContainsKey("allmusic")) return null;
			return Rules.Music["allmusic"].Pool.GetNext();
		}

		string GetPrevSong()
		{
			if (!Rules.Music.ContainsKey("allmusic")) return null;
			return Rules.Music["allmusic"].Pool.GetPrev();
		}

		string GetSong()
		{
			if (!Rules.Music.ContainsKey("allmusic")) return null;
			return Rules.Music["allmusic"].Pool.GetCurrent();
		}
	}
}
