#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA
{
	class PlayMusicOnMapLoadInfo : ITraitInfo
	{
		public readonly string Music = null;
		public readonly bool Loop = false;

		public object Create(ActorInitializer init) { return new PlayMusicOnMapLoad(this); }
	}

	class PlayMusicOnMapLoad : IWorldLoaded
	{
		PlayMusicOnMapLoadInfo Info;

		public PlayMusicOnMapLoad(PlayMusicOnMapLoadInfo info) { Info = info; }

		public void WorldLoaded(World w) { PlayMusic(); }

		void PlayMusic()
		{
			var onComplete = Info.Loop ? (Action)PlayMusic : () => {};

			if (Game.Settings.Sound.MapMusic &&
				Rules.Music.ContainsKey(Info.Music))
				Sound.PlayMusicThen(Rules.Music[Info.Music], onComplete);
		}
	}
}

