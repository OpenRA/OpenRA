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
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World | SystemActors.EditorWorld)]
	[Desc("Trait for music handling. Attach this to the world actor.")]
	public class MusicPlaylistInfo : TraitInfo
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

		[Desc("Allow the background music to be muted by the player.")]
		public readonly bool AllowMuteBackgroundMusic = false;

		[Desc("Disable all world sounds (combat etc).")]
		public readonly bool DisableWorldSounds = false;

		public override object Create(ActorInitializer init) { return new MusicPlaylist(init.World, this); }
	}

	public class MusicPlaylist : INotifyActorDisposing, IGameOver, IWorldLoaded, INotifyGameLoaded
	{
		readonly MusicPlaylistInfo info;
		readonly World world;

		readonly MusicInfo[] random;
		readonly MusicInfo[] playlist;

		public readonly bool IsMusicInstalled;
		public readonly bool IsMusicAvailable;
		public readonly bool AllowMuteBackgroundMusic;

		public bool IsBackgroundMusicMuted => AllowMuteBackgroundMusic && Game.Settings.Sound.MuteBackgroundMusic;

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
			IsMusicAvailable = playlist.Length > 0;
			AllowMuteBackgroundMusic = info.AllowMuteBackgroundMusic;

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
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			// Reset any bogus pre-existing state
			Game.Sound.DisableWorldSounds = info.DisableWorldSounds;

			if (!world.IsLoadingGameSave)
				Play();
		}

		void INotifyGameLoaded.GameLoaded(World world)
		{
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

		void IGameOver.GameOver(World world)
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
			if (!SongExists(currentSong) || (CurrentSongIsBackground && IsBackgroundMusicMuted))
				return;

			Game.Sound.PlayMusicThen(currentSong, PlayNextSong);
		}

		void PlayNextSong()
		{
			if (!CurrentSongIsBackground)
				currentSong = GetNextSong();

			Play();
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

				if (!IsBackgroundMusicMuted)
					Play();
			}
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (currentSong != null)
				Game.Sound.StopMusic();

			Game.Sound.DisableWorldSounds = false;
		}
	}
}
