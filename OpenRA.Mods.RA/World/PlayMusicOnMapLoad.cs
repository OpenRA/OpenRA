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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Attach this to the World: actor.")]
	class PlayMusicOnMapLoadInfo : ITraitInfo
	{
		[Desc("A key listed in music.yaml (no file extension).")]
		public readonly string Music = null;
		public readonly bool Loop = false;

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

			if (Game.Settings.Sound.MapMusic &&
				world.Map.Rules.Music.ContainsKey(info.Music))
				Sound.PlayMusicThen(world.Map.Rules.Music[info.Music], onComplete);
		}
	}
}

