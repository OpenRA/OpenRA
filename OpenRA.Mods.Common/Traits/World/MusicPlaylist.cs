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

using System;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Trait for music handling. Attach this to the world actor.")]
	public class MusicPlaylistInfo : ITraitInfo
	{
		[Desc("Music to play when the map starts.", "Plays the first song on the playlist when undefined.")]
		public readonly string StartingMusic = null;

		[Desc("Music to play when the game has been won.")]
		public readonly string VictoryMusic = null;

		[Desc("Music to play when the game has been lost.")]
		public readonly string DefeatMusic = null;

		[Desc("This track is played when no other music is playing.",
			"It cannot be paused, but can be overridden by selecting a new track.")]
		public readonly string BackgroundMusic = null;

		public object Create(ActorInitializer init) { return new MusicPlaylist(init.World, this); }
	}

	public class MusicPlaylist : INotifyActorDisposing, IGameOver
	{
		readonly MusicPlaylistInfo info;
		readonly World world;

		readonly MusicInfo[] random;
		readonly MusicInfo[] playlist;

		public readonly bool IsMusicInstalled;
		public readonly bool IsMusicAvailable;
		public bool CurrentSongIsBackground { get; private set; }

		MusicInfo currentSong;
		MusicInfo currentBackgroundSong;

		public MusicPlaylist(World world, MusicPlaylistInfo info)
		{
			this.info = info;
			this.world = world;

			IsMusicInstalled = world.Map.Rules.InstalledMusic.Any();
			if (!IsMusicInstalled)
				return;

			playlist = world.Map.Rules.InstalledMusic
				.Where(a => !a.Value.Hidden)
				.Select(a => a.Value)
				.ToArray();

			random = playlist.Shuffle(Game.CosmeticRandom).ToArray();
			IsMusicAvailable = playlist.Any();

			if (SongExists(info.BackgroundMusic))
			{
				currentSong = currentBackgroundSong = world.Map.Rules.Music[info.BackgroundMusic];
				CurrentSongIsBackground = true;
			}
			else
			{
				// Start playback with a random song
				currentSong = random.FirstOrDefault();
			}

			if (SongExists(info.StartingMusic))
			{
				currentSong = world.Map.Rules.Music[info.StartingMusic];
				CurrentSongIsBackground = false;
			}

			Play();
		}

		bool SongExists(string song)
		{
			return !string.IsNullOrEmpty(song)
				&& world.Map.Rules.Music.ContainsKey(song)
				&& world.Map.Rules.Music[song].Exists;
		}

		bool SongExists(MusicInfo song)
		{
			return song != null && song.Exists;
		}

		public MusicInfo CurrentSong()
		{
			return currentSong;
		}

		public MusicInfo[] AvailablePlaylist()
		{
			// TODO: add filter options for faction-specific music
			return playlist;
		}

		public void GameOver(World world)
		{
			if (world.LocalPlayer != null && world.LocalPlayer.WinState == WinState.Won)
			{
				if (SongExists(info.VictoryMusic))
				{
					currentBackgroundSong = world.Map.Rules.Music[info.VictoryMusic];
					Stop();
				}
			}
			else
			{
				// Most RTS treats observers losing the game,
				// no need for a special handling involving them here.
				if (SongExists(info.DefeatMusic))
				{
					currentBackgroundSong = world.Map.Rules.Music[info.DefeatMusic];
					Stop();
				}
			}
		}

		void Play()
		{
			if (!SongExists(currentSong))
				return;

			Game.Sound.PlayMusicThen(currentSong, () =>
			{
				if (!CurrentSongIsBackground && !Game.Settings.Sound.Repeat)
					currentSong = GetNextSong();

				Play();
			});
		}

		public void Play(MusicInfo music)
		{
			if (music == null)
				return;

			currentSong = music;
			CurrentSongIsBackground = false;

			Play();
		}

		public void Play(MusicInfo music, Action onComplete)
		{
			if (music == null)
				return;

			currentSong = music;
			CurrentSongIsBackground = false;
			Game.Sound.PlayMusicThen(music, onComplete);
		}

		public void SetBackgroundMusic(MusicInfo music)
		{
			currentBackgroundSong = music;

			if (CurrentSongIsBackground)
				Stop();
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

			var next = reverse ? songs.Reverse().SkipWhile(m => m != currentSong)
				.Skip(1).FirstOrDefault() ?? songs.Reverse().FirstOrDefault() :
				songs.SkipWhile(m => m != currentSong)
				.Skip(1).FirstOrDefault() ?? songs.FirstOrDefault();

			if (SongExists(next))
				return next;

			return null;
		}

		public void Stop()
		{
			currentSong = null;
			Game.Sound.StopMusic();

			if (currentBackgroundSong != null)
			{
				currentSong = currentBackgroundSong;
				CurrentSongIsBackground = true;
				Play();
			}
		}

		public void Disposing(Actor self)
		{
			if (currentSong != null)
				Game.Sound.StopMusic();
		}
	}
}
