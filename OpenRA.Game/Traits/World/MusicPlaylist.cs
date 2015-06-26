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
using System.Linq;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	[Desc("Trait for music handling. Attach this to the world actor.")]
	public class MusicPlaylistInfo : ITraitInfo
	{
		public readonly string StartingMusic = null;
		public readonly bool LoopStartingMusic = false;

		public object Create(ActorInitializer init) { return new MusicPlaylist(init.World, this); }
	}

	public class MusicPlaylist : INotifyActorDisposing
	{
		readonly MusicInfo[] random;
		readonly MusicInfo[] playlist;

		public readonly bool IsMusicAvailable;

		MusicInfo currentSong;
		bool repeat;

		public MusicPlaylist(World world, MusicPlaylistInfo info)
		{
			IsMusicAvailable = world.Map.Rules.InstalledMusic.Any();

			playlist = world.Map.Rules.InstalledMusic.Select(a => a.Value).ToArray();

			if (!IsMusicAvailable)
				return;

			random = playlist.Shuffle(Game.CosmeticRandom).ToArray();

			if (Game.Settings.Sound.MapMusic
				&& !string.IsNullOrEmpty(info.StartingMusic)
				&& world.Map.Rules.Music.ContainsKey(info.StartingMusic)
				&& world.Map.Rules.Music[info.StartingMusic].Exists)
			{
				currentSong = world.Map.Rules.Music[info.StartingMusic];
				repeat = info.LoopStartingMusic;
			}
			else
			{
				currentSong = Game.Settings.Sound.Shuffle ? random.First() : playlist.First();
				repeat = Game.Settings.Sound.Repeat;
			}

			Play();
		}

		public MusicInfo CurrentSong()
		{
			return currentSong;
		}

		public MusicInfo[] AvailablePlaylist()
		{
			// TO-DO: add filter options for Race-specific music
			return playlist;
		}

		void Play()
		{
			if (currentSong == null || !IsMusicAvailable)
				return;

			Sound.PlayMusicThen(currentSong, () =>
			{
				if (!repeat)
					currentSong = GetNextSong();

				Play();
			});
		}

		public void Play(MusicInfo music)
		{
			if (music == null || !IsMusicAvailable)
				return;

			currentSong = music;
			repeat = Game.Settings.Sound.Repeat;

			Sound.PlayMusicThen(music, () =>
			{
				if (!repeat)
					currentSong = GetNextSong();

				Play();
			});
		}

		public void Play(MusicInfo music, Action onComplete)
		{
			if (music == null || !IsMusicAvailable)
				return;

			currentSong = music;
			Sound.PlayMusicThen(music, onComplete);
		}

		public MusicInfo GetNextSong()
		{
			return GetSong(false);
		}

		public MusicInfo GetPrevSong()
		{
			return GetSong(true);
		}

		MusicInfo GetSong(bool reverse)
		{
			if (!IsMusicAvailable)
				return null;

			var songs = Game.Settings.Sound.Shuffle ? random : playlist;

			return reverse ? songs.SkipWhile(m => m != currentSong)
				.Skip(1).FirstOrDefault() ?? songs.FirstOrDefault() :
				songs.Reverse().SkipWhile(m => m != currentSong)
				.Skip(1).FirstOrDefault() ?? songs.Reverse().FirstOrDefault();
		}

		public void Stop()
		{
			currentSong = null;
			Sound.StopMusic();
		}

		public void Disposing(Actor self)
		{
			if (currentSong != null)
				Stop();
		}
	}
}
