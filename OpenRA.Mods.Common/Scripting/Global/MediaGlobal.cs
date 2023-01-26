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
using Eluant;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Scripting;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Media")]
	public class MediaGlobal : ScriptGlobal
	{
		readonly World world;
		readonly MusicPlaylist playlist;

		public MediaGlobal(ScriptContext context)
			: base(context)
		{
			world = context.World;
			playlist = world.WorldActor.Trait<MusicPlaylist>();
		}

		[Desc("Play an announcer voice listed in notifications.yaml")]
		public void PlaySpeechNotification(Player player, string notification)
		{
			Game.Sound.PlayNotification(world.Map.Rules, player, "Speech", notification, player?.Faction.InternalName);
		}

		[Desc("Play a sound listed in notifications.yaml")]
		public void PlaySoundNotification(Player player, string notification)
		{
			Game.Sound.PlayNotification(world.Map.Rules, player, "Sounds", notification, player?.Faction.InternalName);
		}

		[Desc("Play a sound file")]
		public void PlaySound(string file)
		{
			// TODO: Investigate how scripts use this function, and think about exposing the UI vs World distinction if needed
			Game.Sound.Play(SoundType.World, file);
		}

		[Desc("Play track defined in music.yaml or map.yaml, or keep track empty for playing a random song.")]
		public void PlayMusic(string track = null, LuaFunction onPlayComplete = null)
		{
			if (!playlist.IsMusicAvailable)
				return;

			var musicInfo = !string.IsNullOrEmpty(track)
				? GetMusicTrack(track)
				: playlist.GetNextSong();

			var onComplete = WrapOnPlayComplete(onPlayComplete);
			playlist.Play(musicInfo, onComplete);
		}

		[Desc("Play track defined in music.yaml or map.yaml as background music." +
			" If music is already playing use Media.StopMusic() to stop it" +
			" and the background music will start automatically." +
			" Keep the track empty to disable background music.")]
		public void SetBackgroundMusic(string track = null)
		{
			if (!playlist.IsMusicAvailable)
				return;

			playlist.SetBackgroundMusic(string.IsNullOrEmpty(track) ? null : GetMusicTrack(track));
		}

		MusicInfo GetMusicTrack(string track)
		{
			var music = world.Map.Rules.Music;
			if (music.ContainsKey(track))
				return music[track];

			Log.Write("lua", "Missing music track: " + track);
			return null;
		}

		[Desc("Stop the current song.")]
		public void StopMusic()
		{
			playlist.Stop();
		}

		[Desc("Play a video fullscreen. File name has to include the file extension.")]
		public void PlayMovieFullscreen(string videoFileName, LuaFunction onPlayComplete = null)
		{
			var onComplete = WrapOnPlayComplete(onPlayComplete);
			Media.PlayFMVFullscreen(world, videoFileName, onComplete);
		}

		[Desc("Play a video in the radar window. File name has to include the file extension.")]
		public void PlayMovieInRadar(string videoFileName, LuaFunction onPlayComplete = null)
		{
			var onComplete = WrapOnPlayComplete(onPlayComplete);
			Media.PlayFMVInRadar(videoFileName, onComplete);
		}

		[Desc("Display a text message to all players.")]
		public void DisplayMessage(string text, string prefix = "Mission", Color? color = null)
		{
			if (string.IsNullOrEmpty(text))
				return;

			var c = color ?? Color.White;
			TextNotificationsManager.AddMissionLine(prefix, text, c);
		}

		[Desc("Display a text message only to this player.")]
		public void DisplayMessageToPlayer(Player player, string text, string prefix = "Mission", Color? color = null)
		{
			if (world.LocalPlayer != player)
				return;

			DisplayMessage(text, prefix, color);
		}

		[Desc("Display a system message to the player. If 'prefix' is nil the default system prefix is used.")]
		public void DisplaySystemMessage(string text, string prefix = null)
		{
			if (string.IsNullOrEmpty(text))
				return;

			if (string.IsNullOrEmpty(prefix))
				TextNotificationsManager.AddSystemLine(text);
			else
				TextNotificationsManager.AddSystemLine(prefix, text);
		}

		[Desc("Displays a debug message to the player, if \"Show Map Debug Messages\" is checked in the settings.")]
		public void Debug(string text)
		{
			if (string.IsNullOrEmpty(text) || !Game.Settings.Debug.LuaDebug)
				return;

			TextNotificationsManager.Debug(text);
		}

		[Desc("Display a text message at the specified location.")]
		public void FloatingText(string text, WPos position, int duration = 30, Color? color = null)
		{
			if (string.IsNullOrEmpty(text) || !world.Map.Contains(world.Map.CellContaining(position)))
				return;

			var c = color ?? Color.White;
			world.AddFrameEndTask(w => w.Add(new FloatingText(position, c, text, duration)));
		}

		Action WrapOnPlayComplete(LuaFunction onPlayComplete)
		{
			Action onComplete;
			if (onPlayComplete != null)
			{
				var f = (LuaFunction)onPlayComplete.CopyReference();
				onComplete = () =>
				{
					try
					{
						using (f)
							f.Call().Dispose();
					}
					catch (LuaException e)
					{
						Context.FatalError(e.Message);
					}
				};
			}
			else
				onComplete = () => { };

			return onComplete;
		}
	}
}
