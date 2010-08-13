using System.Linq;
using OpenRA.FileFormats;
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
		string CurrentSong = null;
		public MusicPlayerDelegate()
		{
			var bg = Widget.RootWidget.GetWidget("MUSIC_BG");
			bg.Visible = Game.Settings.MusicPlayer;
			CurrentSong = GetNextSong();

			bg.GetWidget("BUTTON_PLAY").OnMouseUp = mi =>
			{
				if (CurrentSong == null)
					return true;
				
				Sound.PlayMusic(Rules.Music[CurrentSong].Filename);
				bg.GetWidget("BUTTON_PLAY").Visible = false;
				bg.GetWidget("BUTTON_PAUSE").Visible = true;

				return true;
			};
			
			bg.GetWidget("BUTTON_PAUSE").OnMouseUp = mi =>
			{				
				Sound.PauseMusic();
				bg.GetWidget("BUTTON_PAUSE").Visible = false;
				bg.GetWidget("BUTTON_PLAY").Visible = true;
				return true;
			};
			
			bg.GetWidget("BUTTON_STOP").OnMouseUp = mi =>
			{
				Sound.StopMusic();
				bg.GetWidget("BUTTON_PAUSE").Visible = false;
				bg.GetWidget("BUTTON_PLAY").Visible = true;
				
				return true;
			};
			
			bg.GetWidget("BUTTON_NEXT").OnMouseUp = mi =>
			{
				CurrentSong = GetNextSong();
				return bg.GetWidget("BUTTON_PLAY").OnMouseUp(mi);
			};

			bg.GetWidget("BUTTON_PREV").OnMouseUp = mi =>
			{
				CurrentSong = GetPrevSong();
				return bg.GetWidget("BUTTON_PLAY").OnMouseUp(mi);
			};
		}
		
		string GetNextSong()
		{
			var songs = Rules.Music.Select(a => a.Key)
				.Where(a => FileSystem.Exists(Rules.Music[a].Filename));
			
			var nextSong = songs
				.SkipWhile(m => m != CurrentSong)
				.Skip(1)
				.FirstOrDefault();
			
			if (nextSong == null)
				nextSong = songs.FirstOrDefault();
			
			return nextSong;
		}

		string GetPrevSong()
		{
			var songs = Rules.Music.Select(a => a.Key)
				.Where(a => FileSystem.Exists(Rules.Music[a].Filename))
				.Reverse();
			
			var nextSong = songs
				.SkipWhile(m => m != CurrentSong)
				.Skip(1)
				.FirstOrDefault();
			
			if (nextSong == null)
				nextSong = songs.FirstOrDefault();
			
			return nextSong;
		}
	}
}
