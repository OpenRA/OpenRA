#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Traits.Activities;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using System;

namespace OpenRA.Scripting
{
	public class Media
	{		
		public static void PlayFMVFullscreen(World w, string movie, Action onComplete)
		{
			var playerRoot = w.OpenWindow("FMVPLAYER");
			var player = playerRoot.GetWidget<VqaPlayerWidget>("PLAYER");
			w.DisableTick = true;
			player.Load(movie);	
			
			// Mute world sounds
			var oldModifier = Sound.SoundVolumeModifier;
			// Todo: this also modifies vqa audio
			//Sound.SoundVolumeModifier = 0f;
			
			// Stop music while fmv plays
			var music = Sound.MusicPlaying;
			if (music)
				Sound.PauseMusic();
			
			player.PlayThen(() =>
			{
				if (music)
					Sound.PlayMusic();
				
				Widget.CloseWindow();
				Sound.SoundVolumeModifier = oldModifier;
				w.DisableTick = false;
				onComplete();
			});
		}
	}
}
