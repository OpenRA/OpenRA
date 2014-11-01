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
using System.Drawing;
using System.Linq;
using Eluant;
using OpenRA.GameRules;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Scripting
{
	[ScriptGlobal("Media")]
	public class MediaGlobal : ScriptGlobal
	{
		World world;
		public MediaGlobal(ScriptContext context)
			: base(context)
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

			Media.PlayFMVFullscreen(world, movie, onComplete);
		}

		MusicInfo previousMusic;
		[Desc("Play track defined in music.yaml or keep it empty for a random song.")]
		public void PlayMusic(string track = null, LuaFunction func = null)
		{
			if (!Game.Settings.Sound.MapMusic)
				return;

			var music = world.Map.Rules.InstalledMusic.Select(a => a.Value).ToArray();
			if (!music.Any())
				return;

			var musicInfo = !string.IsNullOrEmpty(track) ? world.Map.Rules.Music[track]
				: Game.Settings.Sound.Repeat && previousMusic != null ? previousMusic
				: Game.Settings.Sound.Shuffle ? music.Random(Game.CosmeticRandom)
				: previousMusic == null ? music.First()
				: music.SkipWhile(s => s != previousMusic).Skip(1).First();

			if (func != null)
			{
				var f = func.CopyReference() as LuaFunction;
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

			Sound.PlayMusicThen(musicInfo, onComplete);
			previousMusic = Sound.CurrentMusic;
		}

		[Desc("Display a text message to the player.")]
		public void DisplayMessage(string text, string prefix = "Mission") // TODO: expose HSLColor to Lua and add as parameter
		{
			if (string.IsNullOrEmpty(text))
				return;

			Game.AddChatLine(Color.White, prefix, text);
		}
	}
}
