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
		ButtonWidget installButton;
		Dictionary<string, string> installData;

		[ObjectCreator.UseCtor]
		public InstallMusicLogic(Widget widget)
		{
			installData = Game.modData.Manifest.ContentInstaller;

			installButton = widget.GetOrNull<ButtonWidget>("INSTALL_BUTTON");
			if (installButton != null)
			{
				installButton.OnClick = () => LoadInstallMusicContainer();
				installButton.IsVisible = () =>
					Rules.InstalledMusic.ToArray().Length <= Exts.ParseIntegerInvariant(installData["ShippedSoundtracks"]);
			}
		}

		void LoadInstallMusicContainer()
		{
			var installMusicContainer = Ui.OpenWindow("INSTALL_MUSIC_PANEL", new WidgetArgs());

			Action after = () =>
			{
				try
				{
					GlobalFileSystem.LoadFromManifest(Game.modData.Manifest);
					Rules.Music.Do(m => m.Value.Reload());
					var musicPlayerLogic = (MusicPlayerLogic)installButton.Parent.LogicObject;
					musicPlayerLogic.BuildMusicTable();
					Ui.CloseWindow();
				}
				catch (Exception e)
				{
					Log.Write("debug", "Mounting the new MIX file and rebuild of scores list failed:\n{0}", e);
				}
			};

			var cancelButton = installMusicContainer.GetOrNull<ButtonWidget>("CANCEL_BUTTON");
			if (cancelButton != null)
				cancelButton.OnClick = () => Ui.CloseWindow();

			var copyFromDiscButton = installMusicContainer.GetOrNull<ButtonWidget>("COPY_FROM_CD_BUTTON");
			if (copyFromDiscButton != null)
			{
				copyFromDiscButton.OnClick = () => Ui.OpenWindow("INSTALL_FROMCD_PANEL", new WidgetArgs() {
					{ "continueLoading", after },
				});
			}

			var downloadButton = installMusicContainer.GetOrNull<ButtonWidget>("DOWNLOAD_BUTTON");
			if (downloadButton != null)
			{
				downloadButton.IsVisible = () => !string.IsNullOrEmpty(installData["MusicPackageMirrorList"]);
				var musicInstallData = new Dictionary<string, string> { };
				musicInstallData["PackageMirrorList"] =  installData["MusicPackageMirrorList"];

				downloadButton.OnClick = () => Ui.OpenWindow("INSTALL_DOWNLOAD_PANEL", new WidgetArgs() {
					{ "afterInstall", after },
					{ "installData", new ReadOnlyDictionary<string, string>(musicInstallData) },
				});
			}
		}
	}
}
