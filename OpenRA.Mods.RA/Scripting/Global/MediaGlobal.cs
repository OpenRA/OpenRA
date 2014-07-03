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
using Eluant;
using OpenRA.Scripting;

namespace OpenRA.Mods.RA.Scripting
{
	[ScriptGlobal("Media")]
	public class MediaGlobal : ScriptGlobal
	{
		World world;
		public MediaGlobal(ScriptContext context) : base(context)
		{
			world = context.World;
		}

		[Desc("Play an announcer voice listed in notifications.yaml")]
		public void PlaySpeechNotification(Player player, string notification)
		{
			Sound.PlayNotification(world.Map.Rules, player, "Speech", notification, player != null ? player.Country.Race : null);
		}

		[Desc("Play a sound listed in notifications.yaml")]
		public void PlaySoundNotification(Player player, string notification)
		{
			Sound.PlayNotification(world.Map.Rules, player, "Sounds", notification, player != null ? player.Country.Race : null);
		}

		Action onComplete;
		[Desc("Play a VQA video including the file extension.")]
		public void PlayMovieFullscreen(string movie, LuaFunction func = null)
		{
			if (func != null)
			{
				var f = func.CopyReference() as LuaFunction;
				onComplete = () =>
				{
					try
					{
						using (f)
							f.Call();
					}
					catch (LuaException e)
					{
						context.FatalError(e.Message);
					}
				};
			}
			else
				onComplete = () => { };
			Media.PlayFMVFullscreen(world, movie, onComplete);
		}
	}
}
