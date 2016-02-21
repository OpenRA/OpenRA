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
using System.Collections.Generic;
using System.IO;
using OpenRA.FileFormats;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.LoadScreens
{
	public class BlankLoadScreen : ILoadScreen
	{
		public LaunchArguments Launch;

		public virtual void Init(ModData m, Dictionary<string, string> info) { }

		public virtual void Display()
		{
			if (Game.Renderer == null)
				return;

			// Draw a black screen
			Game.Renderer.BeginFrame(int2.Zero, 1f);
			Game.Renderer.EndFrame(new NullInputHandler());
		}

		public void StartGame(Arguments args)
		{
			Launch = new LaunchArguments(args);
			Ui.ResetAll();
			Game.Settings.Save();

			if (Launch.Benchmark)
			{
				Log.AddChannel("cpu", "cpu.csv");
				Log.Write("cpu", "tick;time [ms]");

				Log.AddChannel("render", "render.csv");
				Log.Write("render", "frame;time [ms]");

				Console.WriteLine("Saving benchmark data into {0}".F(Path.Combine(Platform.SupportDir, "Logs")));

				Game.BenchmarkMode = true;
			}

			// Join a server directly
			var connect = Launch.GetConnectAddress();
			if (!string.IsNullOrEmpty(connect))
			{
				var parts = connect.Split(':');

				if (parts.Length == 2)
				{
					var host = parts[0];
					var port = Exts.ParseIntegerInvariant(parts[1]);
					Game.LoadShellMap();
					Game.RemoteDirectConnect(host, port);
					return;
				}
			}

			// Load a replay directly
			if (!string.IsNullOrEmpty(Launch.Replay))
			{
				var replayMeta = ReplayMetadata.Read(Launch.Replay);
				if (ReplayUtils.PromptConfirmReplayCompatibility(replayMeta, Game.LoadShellMap))
					Game.JoinReplay(Launch.Replay);

				if (replayMeta != null)
				{
					var mod = replayMeta.GameInfo.Mod;
					if (mod != null && mod != Game.ModData.Manifest.Mod.Id && ModMetadata.AllMods.ContainsKey(mod))
						Game.InitializeMod(mod, args);
				}

				return;
			}

			Game.LoadShellMap();
			Game.Settings.Save();
		}

		protected virtual void Dispose(bool disposing) { }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}