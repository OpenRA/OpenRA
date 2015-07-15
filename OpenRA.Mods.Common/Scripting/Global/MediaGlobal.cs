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
using System.IO;
using Eluant;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Effects;
using OpenRA.Scripting;
using OpenRA.Traits;

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
			Sound.PlayNotification(world.Map.Rules, player, "Speech", notification, player != null ? player.Faction.InternalName : null);
		}

		[Desc("Play a sound listed in notifications.yaml")]
		public void PlaySoundNotification(Player player, string notification)
		{
			Sound.PlayNotification(world.Map.Rules, player, "Sounds", notification, player != null ? player.Faction.InternalName : null);
		}

		[Desc("Play a sound file")]
		public void PlaySound(string file)
		{
			Sound.Play(file);
		}

		Action onComplete;
		[Desc("Play track defined in music.yaml or keep it empty for a random song.")]
		public void PlayMusic(string track = null, LuaFunction func = null)
		{
			if (!playlist.IsMusicAvailable)
				return;

			var musicInfo = !string.IsNullOrEmpty(track) ? world.Map.Rules.Music[track]
			: playlist.GetNextSong();

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

				playlist.Play(musicInfo, onComplete);
			}
			else
				playlist.Play(musicInfo);
		}

		[Desc("Stop the current song.")]
		public void StopMusic()
		{
			playlist.Stop();
		}

		Action onCompleteFullscreen;
		[Desc("Play a VQA video fullscreen. File name has to include the file extension.")]
		public void PlayMovieFullscreen(string movie, LuaFunction func = null)
		{
			if (func != null)
			{
				var f = func.CopyReference() as LuaFunction;
				onCompleteFullscreen = () =>
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
				onCompleteFullscreen = () => { };

			Media.PlayFMVFullscreen(world, movie, onCompleteFullscreen);
		}

		Action onLoadComplete;
		Action onCompleteRadar;
		[Desc("Play a VQA video in the radar window. File name has to include the file extension. " +
			"Returns true on success, if the movie wasn't found the function returns false and the callback is executed.")]
		public bool PlayMovieInRadar(string movie, LuaFunction playComplete = null)
		{
			if (playComplete != null)
			{
				var f = playComplete.CopyReference() as LuaFunction;
				onCompleteRadar = () =>
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
				onCompleteRadar = () => { };

			Stream s = null;
			try
			{
				s = GlobalFileSystem.Open(movie);
			}
			catch (FileNotFoundException e)
			{
				Log.Write("lua", "Couldn't play movie {0}! File doesn't exist.", e.FileName);
				onCompleteRadar();
				return false;
			}

			AsyncLoader l = new AsyncLoader(Media.LoadVqa);
			IAsyncResult ar = l.BeginInvoke(s, null, null);
			onLoadComplete = () =>
			{
				Media.StopFMVInRadar();
				world.AddFrameEndTask(_ => Media.PlayFMVInRadar(world, l.EndInvoke(ar), onCompleteRadar));
			};

			world.AddFrameEndTask(w => w.Add(new AsyncAction(ar, onLoadComplete)));
			return true;
		}

		[Desc("Display a text message to the player.")]
		public void DisplayMessage(string text, string prefix = "Mission", HSLColor? color = null)
		{
			if (string.IsNullOrEmpty(text))
				return;

			Color c = color.HasValue ? HSLColor.RGBFromHSL(color.Value.H / 255f, color.Value.S / 255f, color.Value.L / 255f) : Color.White;
			Game.AddChatLine(c, prefix, text);
		}

		[Desc("Display a text message at the specified location.")]
		public void FloatingText(string text, WPos position, int duration = 30, HSLColor? color = null)
		{
			if (string.IsNullOrEmpty(text) || !world.Map.Contains(world.Map.CellContaining(position)))
				return;

			Color c = color.HasValue ? HSLColor.RGBFromHSL(color.Value.H / 255f, color.Value.S / 255f, color.Value.L / 255f) : Color.White;
			world.AddFrameEndTask(w => w.Add(new FloatingText(position, c, text, duration)));
		}

		public delegate VqaReader AsyncLoader(Stream s);
	}
}
