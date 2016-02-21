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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class InstallMusicLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public InstallMusicLogic(Widget widget, string modId)
		{
			var installMusicContainer = widget.Get("INSTALL_MUSIC_PANEL");

			Action loadDefaultMod = () => Game.RunAfterTick(() => Game.InitializeMod(modId, null));

			var cancelButton = installMusicContainer.GetOrNull<ButtonWidget>("BACK_BUTTON");
			if (cancelButton != null)
				cancelButton.OnClick = loadDefaultMod;

			var copyFromDiscButton = installMusicContainer.GetOrNull<ButtonWidget>("INSTALL_MUSIC_BUTTON");
			if (copyFromDiscButton != null)
			{
				copyFromDiscButton.OnClick = () =>
				{
					Ui.OpenWindow("INSTALL_FROMCD_PANEL", new WidgetArgs
					{
						{ "afterInstall", loadDefaultMod },
						{ "modId", modId }
					});
				};
			}

			var downloadButton = installMusicContainer.GetOrNull<ButtonWidget>("DOWNLOAD_MUSIC_BUTTON");
			if (downloadButton != null)
			{
				var installData = ModMetadata.AllMods[modId].Content;
				downloadButton.IsDisabled = () => string.IsNullOrEmpty(installData.MusicPackageMirrorList);
				downloadButton.OnClick = () =>
				{
					Ui.OpenWindow("INSTALL_DOWNLOAD_PANEL", new WidgetArgs
					{
						{ "afterInstall", loadDefaultMod },
						{ "mirrorListUrl", installData.MusicPackageMirrorList },
						{ "modId", modId }
					});
				};
			}
		}
	}
}
