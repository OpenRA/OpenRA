using System.Linq;
using OpenRA.FileFormats;
using System.Drawing;
using OpenRA.Support;
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
			//bg.Visible = Game.Settings.MusicPlayer;
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
			
			bg.GetWidget<LabelWidget>("TIME").GetText = () => "{0:D2}:{1:D2} / {2:D2}:{3:D2}".F((int)Sound.MusicSeekPosition / 60, (int)Sound.MusicSeekPosition % 60,
			                                                                                    Rules.Music[CurrentSong].Length / 60, Rules.Music[CurrentSong].Length % 60);

			var ml = bg.GetWidget<ListBoxWidget>("MUSIC_LIST");
			var itemTemplate = ml.GetWidget<LabelWidget>("MUSIC_TEMPLATE");
			int offset = itemTemplate.Bounds.Y;
			
			foreach (var kv in Rules.Music)
			{
				var song = kv.Key;
				if (!FileSystem.Exists(Rules.Music[song].Filename))
					continue;

				if (CurrentSong == null)
					CurrentSong = song;

				var template = itemTemplate.Clone() as LabelWidget;
				template.Id = "SONG_{0}".F(song);
				template.GetBackground = () => ((song == CurrentSong) ? "dialog2" : null);
				template.OnMouseDown = mi =>
				{
					CurrentSong = song;
					bg.GetWidget("BUTTON_PLAY").OnMouseUp(mi);
					return true;
				};
				template.Parent = ml;

				template.Bounds = new Rectangle(template.Bounds.X, offset, template.Bounds.Width, template.Bounds.Height);
				template.IsVisible = () => true;
				
				template.GetWidget<LabelWidget>("TITLE").GetText = () => "   " + Rules.Music[song].Title;
				template.GetWidget<LabelWidget>("LENGTH").GetText = () => "{0:D2}:{1:D2}".F(Rules.Music[song].Length / 60, Rules.Music[song].Length % 60);

				ml.AddChild(template);

				offset += template.Bounds.Height;
				ml.ContentHeight += template.Bounds.Height;
			}
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
