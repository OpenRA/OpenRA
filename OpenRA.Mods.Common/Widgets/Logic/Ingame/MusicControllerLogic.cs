#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic.Ingame
{
	public class MusicControllerLogic : ChromeLogic
    {
		MusicPlaylist musicPlaylist;

		[ObjectCreator.UseCtor]
		public MusicControllerLogic(Widget widget, World world, WorldRenderer worldRenderer)
		{
			musicPlaylist = world.WorldActor.Trait<MusicPlaylist>();

			var keyhandler = widget.Get<LogicKeyListenerWidget>("MUSICCONTROLLER_KEYHANDLER");
			keyhandler.OnKeyPress = e =>
			{
				if (e.Event == KeyInputEvent.Down)
				{
					var key = Hotkey.FromKeyInput(e);

					if (key == Game.Settings.Keys.NextTrack)
						musicPlaylist.Play(musicPlaylist.GetNextSong());
					else if (key == Game.Settings.Keys.PreviousTrack)
						musicPlaylist.Play(musicPlaylist.GetPrevSong());
					else if (key == Game.Settings.Keys.StopMusic)
						StopMusic();
					else if (key == Game.Settings.Keys.PauseMusic)
						PauseOrResumeMusic();
				}

				return false;
			};
		}

		void PauseOrResumeMusic()
		{
			if (Game.Sound.MusicPlaying)
				Game.Sound.PauseMusic();
			else if (Game.Sound.CurrentMusic != null)
				Game.Sound.PlayMusic();
			else
			{
				musicPlaylist.Play(musicPlaylist.GetNextSong());
			}
		}

		void StopMusic()
		{
			if (!musicPlaylist.CurrentSongIsBackground)
				musicPlaylist.Stop();
			else
				musicPlaylist.Play(musicPlaylist.GetNextSong());
		}
	}
}
