#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Scripting
{
	public static class Media
	{
		public static void PlayFMVFullscreen(World w, string movie, Action onComplete)
		{
			var playerRoot = Game.OpenWindow(w, "FMVPLAYER");
			var player = playerRoot.Get<VqaPlayerWidget>("PLAYER");

			try
			{
				player.Load(movie);
			}
			catch (FileNotFoundException)
			{
				Ui.CloseWindow();
				onComplete();
				return;
			}

			w.SetPauseState(true);

			// Mute world sounds
			var oldModifier = Sound.SoundVolumeModifier;

			// TODO: this also modifies vqa audio
			// Sound.SoundVolumeModifier = 0f;

			// Stop music while fmv plays
			var music = Sound.MusicPlaying;
			if (music)
				Sound.PauseMusic();

			player.PlayThen(() =>
			{
				if (music)
					Sound.PlayMusic();

				Ui.CloseWindow();
				Sound.SoundVolumeModifier = oldModifier;
				w.SetPauseState(false);
				onComplete();
			});
		}

		public static void PlayFMVInRadar(World w, VqaReader movie, Action onComplete)
		{
			var player = Ui.Root.Get<VqaPlayerWidget>("PLAYER");
			player.Open(movie);

			player.PlayThen(() =>
			{
				onComplete();
				player.CloseVideo();
			});
		}

		public static void StopFMVInRadar()
		{
			var player = Ui.Root.Get<VqaPlayerWidget>("PLAYER");
			player.Stop();
		}

		public static VqaReader LoadVqa(Stream s)
		{
			return new VqaReader(s);
		}
	}
}
