#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.LoadScreens
{
	public class BlankLoadScreen : ILoadScreen
	{
		public LaunchArguments Launch;
		ModData modData;

		public virtual void Init(ModData modData, Dictionary<string, string> info)
		{
			this.modData = modData;
		}

		public virtual void Display()
		{
			if (Game.Renderer == null)
				return;

			// Draw a black screen
			Game.Renderer.BeginFrame(int2.Zero, 1f);
			Game.Renderer.EndFrame(new NullInputHandler());
		}

		public virtual void StartGame(Arguments args)
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
				ReplayMetadata replayMeta = null;
				try
				{
					replayMeta = ReplayMetadata.Read(Launch.Replay);
				}
				catch { }

				if (ReplayUtils.PromptConfirmReplayCompatibility(replayMeta, Game.LoadShellMap))
					Game.JoinReplay(Launch.Replay);

				if (replayMeta != null)
				{
					var mod = replayMeta.GameInfo.Mod;
					if (mod != null && mod != Game.ModData.Manifest.Id && Game.Mods.ContainsKey(mod))
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

		public bool BeforeLoad()
		{
			// If a ModContent section is defined then we need to make sure that the
			// required content is installed or switch to the defined content installer.
			if (!modData.Manifest.Contains<ModContent>())
				return true;

			var content = modData.Manifest.Get<ModContent>();
			var contentInstalled = content.Packages
				.Where(p => p.Value.Required)
				.All(p => p.Value.TestFiles.All(f => File.Exists(Platform.ResolvePath(f))));

			if (contentInstalled)
				return true;

			Game.InitializeMod(content.ContentInstallerMod, new Arguments(new[] { "Content.Mod=" + modData.Manifest.Id }));
			return false;
		}
	}
}
