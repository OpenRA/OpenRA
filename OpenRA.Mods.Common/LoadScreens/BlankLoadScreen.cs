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
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Widgets.Logic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.LoadScreens
{
	public class BlankLoadScreen : ILoadScreen
	{
		public virtual void Init(Manifest m, Dictionary<string, string> info) { }

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
			Ui.ResetAll();
			Game.Settings.Save();

			// Check whether the mod content is installed
			// TODO: The installation code has finally been beaten into shape, so we can
			// finally move it all into the planned "Manage Content" panel in the modchooser mod.
			var installData = Game.ModData.Manifest.Get<ContentInstaller>();
			var installModContent = !installData.TestFiles.All(f => GlobalFileSystem.Exists(f));
			var installModMusic = args != null && args.Contains("Install.Music");

			if (installModContent || installModMusic)
			{
				var widgetArgs = new WidgetArgs()
				{
					{ "continueLoading", () => Game.InitializeMod(Game.Settings.Game.Mod, args) },
				};

				if (installData.BackgroundWidget != null)
					Ui.LoadWidget(installData.BackgroundWidget, Ui.Root, widgetArgs);

				var menu = installModContent ? installData.MenuWidget : installData.MusicMenuWidget;
				Ui.OpenWindow(menu, widgetArgs);

				return;
			}

			// Join a server directly
			var connect = string.Empty;
			if (args != null)
			{
				if (args.Contains("Launch.Connect"))
					connect = args.GetValue("Launch.Connect", null);

				if (args.Contains("Launch.URI"))
				{
					connect = args.GetValue("Launch.URI", null);
					if (connect != null)
					{
						connect = connect.Replace("openra://", "");
						connect = connect.TrimEnd('/');
					}
				}
			}

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
			var replayFilename = args != null ? args.GetValue("Launch.Replay", null) : null;
			if (!string.IsNullOrEmpty(replayFilename))
			{
				var replayMeta = ReplayMetadata.Read(replayFilename);
				if (ReplayUtils.PromptConfirmReplayCompatibility(replayMeta, Game.LoadShellMap))
					Game.JoinReplay(replayFilename);

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

		public virtual void Dispose() { }
	}
}