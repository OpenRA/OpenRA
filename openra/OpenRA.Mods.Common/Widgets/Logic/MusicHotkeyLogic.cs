#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
		public MusicHotkeyLogic(Widget widget, ModData modData, World world, Dictionary<string, MiniYaml> logicArgs)
		{
			musicPlaylist = world.WorldActor.Trait<MusicPlaylist>();

			var stopKey = new HotkeyReference();
			if (logicArgs.TryGetValue("StopMusicKey", out var yaml))
				stopKey = modData.Hotkeys[yaml.Value];

			var pauseKey = new HotkeyReference();
			if (logicArgs.TryGetValue("PauseMusicKey", out yaml))
				pauseKey = modData.Hotkeys[yaml.Value];

			var prevKey = new HotkeyReference();
			if (logicArgs.TryGetValue("PrevMusicKey", out yaml))
				prevKey = modData.Hotkeys[yaml.Value];

			var nextKey = new HotkeyReference();
			if (logicArgs.TryGetValue("NextMusicKey", out yaml))
				nextKey = modData.Hotkeys[yaml.Value];

			var keyhandler = widget.Get<LogicKeyListenerWidget>("GLOBAL_KEYHANDLER");
			keyhandler.AddHandler(e =>
			{
				if (e.Event == KeyInputEvent.Down)
				{
					if (nextKey.IsActivatedBy(e))
						musicPlaylist.Play(musicPlaylist.GetNextSong());
					else if (prevKey.IsActivatedBy(e))
						musicPlaylist.Play(musicPlaylist.GetPrevSong());
					else if (stopKey.IsActivatedBy(e))
						StopMusic();
					else if (pauseKey.IsActivatedBy(e))
						PauseOrResumeMusic();
				}

				return false;
			});
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
