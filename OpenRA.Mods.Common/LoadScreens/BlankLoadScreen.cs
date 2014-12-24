#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Widgets;
using OpenRA.Mods.Common.Widgets.Logic;

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
			var installData = Game.modData.Manifest.ContentInstaller;
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
			var connect = args != null ? args.GetValue("Launch.Connect", null) : null;
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
			var replay = args != null ? args.GetValue("Launch.Replay", null) : null;
			if (!string.IsNullOrEmpty(replay))
			{
				Game.JoinReplay(replay);
				return;
			}

			Game.LoadShellMap();
			Game.Settings.Save();
		}

		public virtual void Dispose() { }
	}
}