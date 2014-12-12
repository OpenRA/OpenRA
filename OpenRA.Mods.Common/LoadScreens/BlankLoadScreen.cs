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

		void TestAndContinue()
		{
			Ui.ResetAll();
			var installData = Game.modData.Manifest.ContentInstaller;
			if (!installData.TestFiles.All(f => GlobalFileSystem.Exists(f)))
			{
				var args = new WidgetArgs()
				{
					{ "continueLoading", () => Game.InitializeMod(Game.Settings.Game.Mod, null) },
				};

				if (installData.InstallerBackgroundWidget != null)
					Ui.LoadWidget(installData.InstallerBackgroundWidget, Ui.Root, args);

				Ui.OpenWindow(installData.InstallerMenuWidget, args);
			}
			else
				Game.LoadShellMap();

			Game.Settings.Save();
		}

		public void StartGame(Arguments args)
		{
			var window = args != null ? args.GetValue("Launch.Window", null) : null;
			if (!string.IsNullOrEmpty(window))
			{
				var installData = Game.modData.Manifest.ContentInstaller;
				if (installData.InstallerBackgroundWidget != null)
					Ui.LoadWidget(installData.InstallerBackgroundWidget, Ui.Root, new WidgetArgs());

				Ui.OpenWindow(window, new WidgetArgs());
			}
			else
			{
				TestAndContinue();

				var replay = args != null ? args.GetValue("Launch.Replay", null) : null;
				if (!string.IsNullOrEmpty(replay))
					Game.JoinReplay(replay);
			}
		}

		public virtual void Dispose() { }
	}
}

