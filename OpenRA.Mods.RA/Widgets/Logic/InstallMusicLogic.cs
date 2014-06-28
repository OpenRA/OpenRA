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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class InstallMusicLogic
	{
		[ObjectCreator.UseCtor]
		public InstallMusicLogic(Widget widget)
		{
			var installMusicContainer = widget.Get("INSTALL_MUSIC_PANEL");

			var cancelButton = installMusicContainer.GetOrNull<ButtonWidget>("CANCEL_BUTTON");
			if (cancelButton != null)
			{
				cancelButton.OnClick = () => Game.InitializeMod(Game.Settings.Game.Mod, null);
			}

			var copyFromDiscButton = installMusicContainer.GetOrNull<ButtonWidget>("COPY_FROM_CD_BUTTON");
			if (copyFromDiscButton != null)
			{
				copyFromDiscButton.OnClick = () =>
				{
					Ui.OpenWindow("INSTALL_FROMCD_PANEL", new WidgetArgs() {
						{ "continueLoading", () => Game.InitializeMod(Game.Settings.Game.Mod, null) },
					});
				};
			}

			var downloadButton = installMusicContainer.GetOrNull<ButtonWidget>("DOWNLOAD_BUTTON");
			if (downloadButton != null)
			{
				var installData = Game.modData.Manifest.ContentInstaller;
				downloadButton.IsVisible = () => !string.IsNullOrEmpty(installData.MusicPackageMirrorList);
				downloadButton.OnClick = () =>
				{
					Ui.OpenWindow("INSTALL_DOWNLOAD_PANEL", new WidgetArgs() {
						{ "afterInstall", () => Game.InitializeMod(Game.Settings.Game.Mod, null) },
					});
				};
			}
		}
	}
}
