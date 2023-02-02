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

using System;
using System.IO;
using OpenRA.Mods.Common.Widgets;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Scripting
{
	public static class Media
	{
		public static void PlayFMVFullscreen(World w, string videoFileName, Action onComplete)
		{
			var playerRoot = Game.OpenWindow(w, "FMVPLAYER");
			var player = playerRoot.Get<VideoPlayerWidget>("PLAYER");

			try
			{
				player.LoadAndPlay(videoFileName);
			}
			catch (FileNotFoundException)
			{
				Ui.CloseWindow();
				onComplete();
				return;
			}

			w.SetPauseState(true);

			// Mute world sounds
			var oldModifier = Game.Sound.SoundVolumeModifier;

			// TODO: this also modifies vqa audio
			// Game.Sound.SoundVolumeModifier = 0f;

			// Stop music while fmv plays
			var music = Game.Sound.MusicPlaying;
			if (music)
				Game.Sound.PauseMusic();

			player.PlayThen(() =>
			{
				if (music)
					Game.Sound.PlayMusic();

				Ui.CloseWindow();
				Game.Sound.SoundVolumeModifier = oldModifier;
				w.SetPauseState(false);
				onComplete();
			});
		}

		public static void PlayFMVInRadar(string videoFileName, Action onComplete)
		{
			var player = Ui.Root.Get<VideoPlayerWidget>("PLAYER");
			player.LoadAndPlayAsync(videoFileName, onComplete);
		}
	}
}
