#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Lint;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	[ChromeLogicArgsHotkeys("StopMusicKey", "PauseMusicKey", "PrevMusicKey", "NextMusicKey")]
	public class MusicHotkeyLogic : ChromeLogic
	{
		readonly MusicPlaylist musicPlaylist;

		[ObjectCreator.UseCtor]
		public MusicHotkeyLogic(Widget widget, World world, Dictionary<string, MiniYaml> logicArgs)
		{
			musicPlaylist = world.WorldActor.Trait<MusicPlaylist>();

			var ks = Game.Settings.Keys;
			MiniYaml yaml;

			var stopKey = new NamedHotkey();
			if (logicArgs.TryGetValue("StopMusicKey", out yaml))
				stopKey = new NamedHotkey(yaml.Value, ks);

			var pauseKey = new NamedHotkey();
			if (logicArgs.TryGetValue("PauseMusicKey", out yaml))
				pauseKey = new NamedHotkey(yaml.Value, ks);

			var prevKey = new NamedHotkey();
			if (logicArgs.TryGetValue("PrevMusicKey", out yaml))
				prevKey = new NamedHotkey(yaml.Value, ks);

			var nextKey = new NamedHotkey();
			if (logicArgs.TryGetValue("NextMusicKey", out yaml))
				nextKey = new NamedHotkey(yaml.Value, ks);

			var keyhandler = widget.Get<LogicKeyListenerWidget>("GLOBAL_KEYHANDLER");
			keyhandler.OnKeyPress += e =>
			{
				if (e.Event == KeyInputEvent.Down)
				{
					var key = Hotkey.FromKeyInput(e);

					if (key == nextKey.GetValue())
						musicPlaylist.Play(musicPlaylist.GetNextSong());
					else if (key == prevKey.GetValue())
						musicPlaylist.Play(musicPlaylist.GetPrevSong());
					else if (key == stopKey.GetValue())
						StopMusic();
					else if (key == pauseKey.GetValue())
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
