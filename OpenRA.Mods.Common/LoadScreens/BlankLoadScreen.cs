#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		protected ModData ModData { get; private set; }

		public virtual void Init(ModData modData, Dictionary<string, string> info)
		{
			ModData = modData;
		}

		public virtual void Display()
		{
			if (Game.Renderer == null)
				return;

			// Draw a black screen
			Game.Renderer.BeginUI();
			Game.Renderer.EndFrame(new NullInputHandler());
		}

		public virtual void StartGame(Arguments args)
		{
			Launch = new LaunchArguments(args);
			Ui.ResetAll();
			Game.Settings.Save();

			if (!string.IsNullOrEmpty(Launch.Benchmark))
			{
				Console.WriteLine("Saving benchmark data into {0}".F(Path.Combine(Platform.SupportDir, "Logs")));

				Game.BenchmarkMode(Launch.Benchmark);
			}

			// Join a server directly
			var connect = Launch.GetConnectEndPoint();
			if (connect != null)
			{
				Game.LoadShellMap();
				Game.RemoteDirectConnect(connect);
				return;
			}

			// Start a map directly
			if (!string.IsNullOrEmpty(Launch.Map))
			{
				Game.LoadMap(Launch.Map);
				return;
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

		public virtual bool BeforeLoad()
		{
			// Reset the UI scaling if the user has configured a UI scale that pushes us below the minimum allowed effective resolution
			var minResolution = ModData.Manifest.Get<WorldViewportSizes>().MinEffectiveResolution;
			var resolution = Game.Renderer.Resolution;
			if ((resolution.Width < minResolution.Width || resolution.Height < minResolution.Height) && Game.Settings.Graphics.UIScale > 1.0f)
			{
				Game.Settings.Graphics.UIScale = 1.0f;
				Game.Renderer.SetUIScale(1.0f);
			}

			// Saved settings may have been invalidated by a hardware change
			Game.Settings.Graphics.GLProfile = Game.Renderer.GLProfile;
			Game.Settings.Graphics.VideoDisplay = Game.Renderer.CurrentDisplay;

			// If a ModContent section is defined then we need to make sure that the
			// required content is installed or switch to the defined content installer.
			if (!ModData.Manifest.Contains<ModContent>())
				return true;

			var content = ModData.Manifest.Get<ModContent>();
			var contentInstalled = content.Packages
				.Where(p => p.Value.Required)
				.All(p => p.Value.TestFiles.All(f => File.Exists(Platform.ResolvePath(f))));

			if (contentInstalled)
				return true;

			Game.InitializeMod(content.ContentInstallerMod, new Arguments(new[] { "Content.Mod=" + ModData.Manifest.Id }));
			return false;
		}
	}
}
