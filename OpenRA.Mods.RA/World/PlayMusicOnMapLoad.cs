#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class PlayMusicOnMapLoadInfo : ITraitInfo
	{
		[Desc("Start with this song name from the music YAML definitions.")]
		public readonly string Song = null;

		[Desc("Don't stop after one song.")]
		public readonly bool Loop = false;

		[Desc("Play the same song all over again.")]
		public readonly bool Repeat = false;

		[Desc("Play a random song.")]
		public readonly bool Shuffle = false;

		public object Create(ActorInitializer init) { return new PlayMusicOnMapLoad(init.world, this); }
	}

	class PlayMusicOnMapLoad : IWorldLoaded
	{
		readonly PlayMusicOnMapLoadInfo info;
		readonly World world;

		public PlayMusicOnMapLoad(World world, PlayMusicOnMapLoadInfo info)
		{
			this.world = world;
			this.info = info;
		}

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			PlayMusic();
		}

		void PlayMusic()
		{
			var onComplete = info.Loop ? (Action)PlayMusic : () => {};
			var music = world.Map.Rules.InstalledMusic;
			if (!music.Any())
				return;

			var playlist = world.Map.Rules.Music;

			if (Game.Settings.Sound.MapMusic)
			{
				if (info.Shuffle || info.Song == null || !playlist.ContainsKey(info.Song) || (!info.Repeat && playlist[info.Song] == Sound.CurrentMusic))
					Sound.PlayMusicThen(music.Shuffle(Game.CosmeticRandom).First().Value, onComplete);
				else
					Sound.PlayMusicThen(playlist[info.Song], onComplete);
			}
		}
	}
}

